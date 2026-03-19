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

public class WeakPtrTests
{
    [Fact]
    public void TryUpgrade_Succeeds_WhileStrongReferenceIsAlive()
    {
        var resource = new CountingDisposable();
        using var shared = new SharedPtr<CountingDisposable>(resource);
        using IWeakPtr<CountingDisposable> weak = shared.Weak();

        bool upgraded = weak.TryUpgrade(out ISharedPtr<CountingDisposable>? upgradedShared);

        Assert.True(upgraded);
        Assert.NotNull(upgradedShared);
        Assert.False(weak.IsExpired);
        upgradedShared.Dispose();
    }

    [Fact]
    public void TryUpgrade_Fails_AfterLastStrongReferenceIsDisposed()
    {
        var resource = new CountingDisposable();
        IWeakPtr<CountingDisposable> weak;

        using (var shared = new SharedPtr<CountingDisposable>(resource))
        {
            weak = shared.Weak();
        }

        bool upgraded = weak.TryUpgrade(out ISharedPtr<CountingDisposable>? upgradedShared);

        Assert.False(upgraded);
        Assert.Null(upgradedShared);
        Assert.True(weak.IsExpired);
        weak.Dispose();
    }

    [Fact]
    public void ConcurrentTryUpgrade_AndDispose_NeverDoubleDisposes()
    {
        var resource = new CountingDisposable();
        using var shared = new SharedPtr<CountingDisposable>(resource);
        using IWeakPtr<CountingDisposable> weak = shared.Weak();
        var upgradedPtrs = new ConcurrentBag<ISharedPtr<CountingDisposable>>();

        Parallel.For(0, 5000, i =>
        {
            if (i == 1000)
            {
                shared.Dispose();
            }

            if (weak.TryUpgrade(out ISharedPtr<CountingDisposable>? upgraded))
            {
                upgradedPtrs.Add(upgraded);
            }
        });

        while (upgradedPtrs.TryTake(out ISharedPtr<CountingDisposable>? upgraded))
        {
            upgraded.Dispose();
        }

        Assert.Equal(1, resource.DisposeCount);
    }
}

public class SmartPtrFactoryTests
{
    [Fact]
    public void MakeShared_FromInstance_CreatesWorkingSharedPointer()
    {
        var resource = new CountingDisposable();
        using ISharedPtr<CountingDisposable> ptr = SmartPtr.MakeShared(resource);

        Assert.False(ptr.IsEmpty);
        Assert.Equal(1, ptr.UseCount);
    }

    [Fact]
    public void MakeShared_FromFactory_CreatesWorkingSharedPointer()
    {
        using ISharedPtr<CountingDisposable> ptr = SmartPtr.MakeShared(() => new CountingDisposable());

        Assert.False(ptr.IsEmpty);
        Assert.Equal(1, ptr.UseCount);
    }

    [Fact]
    public void MakeUnique_FromFactory_CreatesWorkingUniquePointer()
    {
        using IUniquePtr<CountingDisposable> ptr = SmartPtr.MakeUnique(() => new CountingDisposable());

        Assert.False(ptr.IsEmpty);
        Assert.NotNull(ptr.Target);
    }

    [Fact]
    public void MakeShared_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SmartPtr.MakeShared((Func<CountingDisposable>)null!));
    }

    [Fact]
    public void MakeUnique_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SmartPtr.MakeUnique((Func<CountingDisposable>)null!));
    }

    [Fact]
    public void MakeShared_FactoryReturningNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SmartPtr.MakeShared(() => (CountingDisposable)null!));
    }

    [Fact]
    public void MakeShared_CustomDeleter_IsCalledOnceAtFinalRelease()
    {
        var resource = new CountingDisposable();
        int deleterCalls = 0;
        using ISharedPtr<CountingDisposable> root = SmartPtr.MakeShared(resource, _ => Interlocked.Increment(ref deleterCalls));
        using ISharedPtr<CountingDisposable> copy = root.Share();

        copy.Dispose();
        Assert.Equal(0, deleterCalls);

        root.Dispose();
        Assert.Equal(1, deleterCalls);
        Assert.Equal(0, resource.DisposeCount);
    }

    [Fact]
    public void MakeUnique_CustomDeleter_IsCalledOnce()
    {
        var resource = new CountingDisposable();
        int deleterCalls = 0;
        using IUniquePtr<CountingDisposable> ptr = SmartPtr.MakeUnique(resource, _ => Interlocked.Increment(ref deleterCalls));

        ptr.Dispose();
        ptr.Dispose();

        Assert.Equal(1, deleterCalls);
        Assert.Equal(0, resource.DisposeCount);
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
