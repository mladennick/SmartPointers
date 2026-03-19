using SmartPointers.Demo;
using SmartPointers.Implementations;
using SmartPointers.Interfaces;
using System.Diagnostics;

Console.WriteLine("=== SmartPointers Demo ===");
Console.WriteLine();

await RunSharedPtrDemo();
Console.WriteLine();
RunWeakPtrDemo();
Console.WriteLine();
RunUniquePtrDemo();

Console.WriteLine();
Console.WriteLine("Demo completed.");

static async Task RunSharedPtrDemo()
{
    Console.WriteLine("1) SharedPtr<T> - thread-safe shared ownership");
    Console.WriteLine("----------------------------------------------");

    using var root = new SharedPtr<FakeImageBuffer>(new FakeImageBuffer(sizeInMb: 20));
    Console.WriteLine($"[Main] Root created. UseCount = {root.UseCount}");

    var workers = Enumerable.Range(1, 4).Select(async workerId =>
    {
        using ISharedPtr<FakeImageBuffer> frame = root.Share();
        Console.WriteLine($"[W{workerId}] Share acquired. UseCount = {frame.UseCount}");
        await ProcessFrame(frame, workerId);
        Console.WriteLine($"[W{workerId}] Processing done.");
    });

    await Task.WhenAll(workers);
    Console.WriteLine($"[Main] Workers done. UseCount = {root.UseCount}");
    Console.WriteLine("[Main] Leaving using scope now. Last owner will dispose resource.");
}

static void RunWeakPtrDemo()
{
    Console.WriteLine("2) WeakPtr<T> - non-owning observer with upgrade");
    Console.WriteLine("------------------------------------------------");

    using var owner = new SharedPtr<FakeImageBuffer>(new FakeImageBuffer(sizeInMb: 10));
    using IWeakPtr<FakeImageBuffer> weak = owner.Weak();
    Console.WriteLine($"[Main] Weak pointer created. IsExpired = {weak.IsExpired}");

    if (weak.TryUpgrade(out ISharedPtr<FakeImageBuffer>? upgraded))
    {
        using (upgraded)
        {
            Console.WriteLine($"[Main] Upgrade succeeded. UseCount = {upgraded.UseCount}");
            int checksum = upgraded.Target.Data[0] + upgraded.Target.Data[^1];
            Console.WriteLine($"[Main] Upgraded pointer checksum = {checksum}");
        }
    }

    owner.Dispose();
    Console.WriteLine($"[Main] Owner disposed. Weak IsExpired = {weak.IsExpired}");
    bool upgradedAfterDispose = weak.TryUpgrade(out _);
    Console.WriteLine($"[Main] Upgrade after final dispose -> {upgradedAfterDispose}");
}

static void RunUniquePtrDemo()
{
    Console.WriteLine("3) UniquePtr<T> - strict single ownership");
    Console.WriteLine("------------------------------------------");

    var owner = new UniquePtr<FakeImageBuffer>(new FakeImageBuffer(sizeInMb: 5));
    Console.WriteLine("[Main] UniquePtr created.");

    IUniquePtr<FakeImageBuffer> moved = owner.Transfer();
    Console.WriteLine($"[Main] Ownership transferred. source.IsEmpty = {owner.IsEmpty}, moved.IsEmpty = {moved.IsEmpty}");

    try
    {
        _ = owner.Target;
    }
    catch (ObjectDisposedException ex)
    {
        Console.WriteLine($"[Main] Expected exception after move: {ex.GetType().Name}");
    }

    FakeImageBuffer raw = moved.Release();
    Console.WriteLine($"[Main] Resource released manually. moved.IsEmpty = {moved.IsEmpty}");
    Console.WriteLine("[Main] Caller now owns raw resource and must dispose it.");
    raw.Dispose();
}

static async Task ProcessFrame(ISharedPtr<FakeImageBuffer> frame, int workerId)
{
    Stopwatch sw = Stopwatch.StartNew();

    // Simulate CPU-bound work touching the resource.
    await Task.Delay(80 + workerId * 20);
    int checksum = frame.Target.Data[0] + frame.Target.Data[^1] + workerId;

    sw.Stop();
    Console.WriteLine($"[W{workerId}] checksum={checksum}, elapsed={sw.ElapsedMilliseconds}ms");
}


