using global::SmartPointers.Interfaces;
using System;
using System.Threading;

namespace SmartPointers.Implementations
{
    /// <summary>
    /// Thread-safe shared state for all <see cref="SharedPtr{T}"/> instances
    /// that reference the same resource.
    /// </summary>
    internal sealed class ControlBlock<T> where T : class, IDisposable
    {
        /// <summary>
        /// The managed wrapper that owns unmanaged memory.
        /// Set to <see langword="null"/> when the final reference is released.
        /// </summary>
        public T? Resource;

        // The shared counter
        private int _refCount;

        /// <summary>
        /// Gets the current number of active references.
        /// </summary>
        public int RefCount => Volatile.Read(ref _refCount);

        /// <summary>
        /// Initializes a new control block with one owner.
        /// </summary>
        public ControlBlock(T resource)
        {
            Resource = resource;
            _refCount = 1;
        }

        /// <summary>
        /// Attempts to increment the reference count.
        /// Returns <see langword="false"/> if the count already reached zero.
        /// </summary>
        public bool TryAddRef()
        {
            while (true)
            {
                int current = Volatile.Read(ref _refCount);
                if (current == 0)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _refCount, current + 1, current) == current)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Releases one owner and disposes the resource when the last owner leaves.
        /// </summary>
        public void Release()
        {
            // Interlocked.Decrement returns the NEW value after subtracting.
            // If it hits exactly 0, this thread is the one responsible for the cleanup.
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                Resource?.Dispose();
                Resource = null; // Free the C# reference to help the GC
            }
        }
    }

    /// <summary>
    /// C++-style shared ownership pointer for <see cref="IDisposable"/> resources.
    /// The wrapped resource is disposed exactly once when the last pointer is released.
    /// </summary>
    public sealed class SharedPtr<T> : ISharedPtr<T> where T : class, IDisposable
    {
        private ControlBlock<T>? _controlBlock;
        private int _isDisposed = 0;

        /// <summary>
        /// Creates a new shared pointer that owns <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">The disposable resource to manage.</param>
        public SharedPtr(T resource)
        {
            ArgumentNullException.ThrowIfNull(resource);
            _controlBlock = new ControlBlock<T>(resource);
        }

        private SharedPtr(ControlBlock<T> block)
        {
            _controlBlock = block;
            if (!_controlBlock.TryAddRef())
            {
                _controlBlock = null;
                throw new InvalidOperationException("Cannot share a disposed SharedPtr.");
            }
        }

        // --- ISmartPtr Implementation ---

        /// <inheritdoc />
        public T Target
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) == 1, this);
                if (_controlBlock?.Resource == null) throw new InvalidOperationException("SharedPtr is empty.");
                return _controlBlock.Resource;
            }
        }

        // The new property required by the base interface
        /// <inheritdoc />
        public bool IsEmpty => Volatile.Read(ref _isDisposed) == 1 || _controlBlock?.Resource == null;


        // --- ISharedPtr Implementation ---

        /// <inheritdoc />
        public int UseCount => _controlBlock?.RefCount ?? 0;

        /// <inheritdoc />
        public ISharedPtr<T> Share()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) == 1, this);
            ControlBlock<T>? block = Volatile.Read(ref _controlBlock);
            if (block == null) throw new InvalidOperationException("Cannot share an empty SharedPtr.");

            return new SharedPtr<T>(block);
        }


        // --- Dispose Pattern ---

        ~SharedPtr()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases this pointer's ownership. The underlying resource is disposed
        /// only when this call removes the final active owner.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Thread-safe disposal. Interlocked.Exchange guarantees this block only runs once per pointer instance.
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                _controlBlock?.Release();
                _controlBlock = null;
            }
        }
    }
}
