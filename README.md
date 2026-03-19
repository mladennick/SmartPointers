# C# Smart Pointers

A .NET library bringing C++ style `SharedPtr<T>` and `UniquePtr<T>` semantics to C# for robust, thread-safe management of unmanaged resources.

## The Problem: The GC Illusion

When wrapping low-level C++ memory (like image buffers, Halcon `HObject`s, or OpenCV `Mat`s) in C#, the .NET Garbage Collector only sees a tiny wrapper object. It is completely unaware of the massive unmanaged memory blocks tied to that wrapper. 

Because the GC feels no memory pressure from these tiny wrappers, it delays collection. In a high-throughput or multi-threaded environment (like an image processing pipeline), this causes massive unmanaged memory leaks. 

## The Solution

This library provides explicit ownership and thread-safe reference counting for `IDisposable` objects, ensuring unmanaged memory is freed the exact moment it is no longer needed.

### Features
* **`SharedPtr<T>`**: Thread-safe, reference-counted pointer. The underlying `IDisposable` is only disposed when the last reference is released. Perfect for passing images across multiple worker threads.
* **`WeakPtr<T>`**: Non-owning observer pointer. It can attempt to upgrade to a `SharedPtr<T>` only while the resource is still alive, helping avoid ownership cycles.
* **`UniquePtr<T>`**: Enforces strict single-ownership of a resource. Allows safely transferring ownership between scopes without accidental sharing.

## Project Structure
* `SmartPointers`: The core class library.
* `SmartPointers.Tests`: xUnit tests verifying thread safety and preventing race conditions.
* `SmartPointers.Demo`: A console application demonstrating how to manage large dummy image allocations across threads.

## Usage

### Factory helpers (`SmartPtr`)

```csharp
using SmartPointers;
using SmartPointers.Demo;

using var shared = SmartPtr.MakeShared(() => new FakeImageBuffer(sizeInMb: 20));
using var unique = SmartPtr.MakeUnique(() => new FakeImageBuffer(sizeInMb: 5));
```

What this gives you:
- C++-style creation flow similar to `make_shared` / `make_unique`.
- Centralized argument validation for constructors and factories.
- Same runtime semantics as creating `SharedPtr<T>` / `UniquePtr<T>` directly.

### Custom deleters (`Action<T>`)

```csharp
using SmartPointers;
using SmartPointers.Demo;

int customCleanupCalls = 0;

using var shared = SmartPtr.MakeShared(
    () => new FakeImageBuffer(sizeInMb: 20),
    buffer =>
    {
        // Replace with native cleanup for interop scenarios.
        customCleanupCalls++;
    });

using var unique = SmartPtr.MakeUnique(
    () => new FakeImageBuffer(sizeInMb: 5),
    buffer =>
    {
        // You can customize cleanup for unique ownership too.
        customCleanupCalls++;
    });
```

What this gives you:
- A custom release policy for unmanaged interop or specialized cleanup flows.
- Default behavior remains `Dispose()` when no custom deleter is provided.
- The custom deleter is executed exactly once at resource final release.

### `SharedPtr<T>`: share ownership across threads

```csharp
using SmartPointers.Implementations;
using SmartPointers.Interfaces;
using SmartPointers.Demo;

using var root = new SharedPtr<FakeImageBuffer>(new FakeImageBuffer(sizeInMb: 20));

var tasks = Enumerable.Range(1, 4).Select(async workerId =>
{
    using ISharedPtr<FakeImageBuffer> local = root.Share();
    await Task.Delay(100); // Simulate work
    int checksum = local.Target.Data[0] + local.Target.Data[^1] + workerId;
    Console.WriteLine($"Worker {workerId} -> {checksum}, UseCount={local.UseCount}");
});

await Task.WhenAll(tasks);
Console.WriteLine($"After workers: UseCount={root.UseCount}");
```

What this guarantees:
- `Share()` increments the reference count atomically.
- Every pointer instance can be disposed independently.
- The underlying resource is disposed exactly once, when the final owner is released.

### `WeakPtr<T>`: observe without owning

```csharp
using SmartPointers.Implementations;
using SmartPointers.Interfaces;
using SmartPointers.Demo;

using var owner = new SharedPtr<FakeImageBuffer>(new FakeImageBuffer(sizeInMb: 10));
using IWeakPtr<FakeImageBuffer> weak = owner.Weak();

if (weak.TryUpgrade(out ISharedPtr<FakeImageBuffer>? upgraded))
{
    using (upgraded)
    {
        Console.WriteLine($"Upgrade succeeded, UseCount={upgraded.UseCount}");
    }
}

owner.Dispose();
bool upgradedAfterDispose = weak.TryUpgrade(out _); // false
```

What this guarantees:
- `Weak()` does not increment the strong ownership count.
- `TryUpgrade(...)` atomically succeeds only while the resource is still alive.
- Once the last shared owner is released, upgrades fail safely.

### `UniquePtr<T>`: strict single ownership

```csharp
using SmartPointers.Implementations;
using SmartPointers.Interfaces;
using SmartPointers.Demo;

var owner = new UniquePtr<FakeImageBuffer>(new FakeImageBuffer(sizeInMb: 5));

// Move ownership (C++-like std::move semantics)
IUniquePtr<FakeImageBuffer> moved = owner.Transfer();

// Give ownership back to caller
FakeImageBuffer raw = moved.Release();
raw.Dispose(); // Caller is now responsible
```

What this guarantees:
- `Transfer()` invalidates the source pointer and moves ownership to a new `UniquePtr<T>`.
- `Release()` detaches the resource from the pointer without disposing it.
- `Dispose()` is idempotent and safe to call more than once.

### Thread-safety note

`SharedPtr<T>` makes ownership and lifetime management thread-safe. It does not automatically make `T` itself thread-safe. If multiple threads write to the same underlying resource, protect access with your own synchronization strategy (`lock`, `SemaphoreSlim`, etc.).

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.