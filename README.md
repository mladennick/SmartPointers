# C# Smart Pointers

A .NET library bringing C++ style `SharedPtr<T>` and `UniquePtr<T>` semantics to C# for robust, thread-safe management of unmanaged resources.

## The Problem: The GC Illusion

When wrapping low-level C++ memory (like image buffers, Halcon `HObject`s, or OpenCV `Mat`s) in C#, the .NET Garbage Collector only sees a tiny wrapper object. It is completely unaware of the massive unmanaged memory blocks tied to that wrapper. 

Because the GC feels no memory pressure from these tiny wrappers, it delays collection. In a high-throughput or multi-threaded environment (like an image processing pipeline), this causes massive unmanaged memory leaks. 

## The Solution

This library provides explicit ownership and thread-safe reference counting for `IDisposable` objects, ensuring unmanaged memory is freed the exact moment it is no longer needed.

### Features
* **`SharedPtr<T>`**: Thread-safe, reference-counted pointer. The underlying `IDisposable` is only disposed when the last reference is released. Perfect for passing images across multiple worker threads.
* **`UniquePtr<T>`**: Enforces strict single-ownership of a resource. Allows safely transferring ownership between scopes without accidental sharing.

## Project Structure
* `SmartPointers`: The core class library.
* `SmartPointers.Tests`: xUnit tests verifying thread safety and preventing race conditions.
* `SmartPointers.Demo`: A console application demonstrating how to manage large dummy image allocations across threads.

## Usage

*(Code examples coming soon!)*

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.