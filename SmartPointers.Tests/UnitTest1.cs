using SmartPointers.Implementations;
using SmartPointers.Interfaces;
using System.Collections.Concurrent;

namespace SmartPointers.Tests;

public class SharedPtrTests
{
    [Fact]
    public void Share_IncrementsUseCount_AndDisposingAllDisposesResourceOnce()
    {
        var resource = new CountingDisposable();
        var ptr = new SharedPtr<CountingDisposable>(resource);

        ISharedPtr<CountingDisposable> copy = ptr.Share();

        Assert.Equal(2, ptr.UseCount);
        Assert.Equal(2, copy.UseCount);
        Assert.False(ptr.IsEmpty);
        Assert.False(copy.IsEmpty);

        copy.Dispose();
        Assert.Equal(1, ptr.UseCount);
        Assert.Equal(0, resource.DisposeCount);

        ptr.Dispose();
        Assert.Equal(1, resource.DisposeCount);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesResourceOnlyOnce()
    {
        var resource = new CountingDisposable();
        var ptr = new SharedPtr<CountingDisposable>(resource);

        ptr.Dispose();
        ptr.Dispose();

        Assert.Equal(1, resource.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => _ = ptr.Target);
    }

    [Fact]
    public void ConcurrentShareAndDispose_NeverDoubleDisposes_AndNeverResurrects()
    {
        var resource = new CountingDisposable();
        var ptr = new SharedPtr<CountingDisposable>(resource);
        var children = new ConcurrentBag<ISharedPtr<CountingDisposable>>();

        Parallel.For(0, 5000, _ =>
        {
            try
            {
                ISharedPtr<CountingDisposable> child = ptr.Share();
                children.Add(child);
            }
            catch (ObjectDisposedException)
            {
                // Expected if root gets disposed during contention.
            }
            catch (InvalidOperationException)
            {
                // Expected if share races with final release.
            }
        });

        ptr.Dispose();

        while (children.TryTake(out ISharedPtr<CountingDisposable>? child))
        {
            child.Dispose();
        }

        Assert.Equal(1, resource.DisposeCount);
    }
}

public class UniquePtrTests
{
    [Fact]
    public void Transfer_MovesOwnership_AndSourceBecomesDisposed()
    {
        var resource = new CountingDisposable();
        var source = new UniquePtr<CountingDisposable>(resource);

        IUniquePtr<CountingDisposable> destination = source.Transfer();

        Assert.True(source.IsEmpty);
        Assert.False(destination.IsEmpty);
        Assert.Throws<ObjectDisposedException>(() => _ = source.Target);

        destination.Dispose();
        Assert.Equal(1, resource.DisposeCount);
    }

    [Fact]
    public void Release_ReturnsResource_AndDoesNotDisposeIt()
    {
        var resource = new CountingDisposable();
        var ptr = new UniquePtr<CountingDisposable>(resource);

        CountingDisposable released = ptr.Release();

        Assert.Same(resource, released);
        Assert.True(ptr.IsEmpty);
        Assert.Equal(0, resource.DisposeCount);

        released.Dispose();
        Assert.Equal(1, resource.DisposeCount);
    }
}

internal sealed class CountingDisposable : IDisposable
{
    private int _disposeCount;
    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public void Dispose()
    {
        Interlocked.Increment(ref _disposeCount);
    }
}
