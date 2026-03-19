using SmartPointers.Interfaces;
using System;
using System.Threading;

namespace SmartPointers.Implementations
{
    /// <summary>
    /// C++-style unique ownership pointer for <see cref="IDisposable"/> resources.
    /// Ownership can be moved (<see cref="Transfer"/>) or relinquished (<see cref="Release"/>),
    /// but it cannot be shared.
    /// </summary>
    public sealed class UniquePtr<T> : IUniquePtr<T> where T : class, IDisposable
    {
        private T? _resource;
        private int _isDisposed = 0;

        /// <summary>
        /// Creates a new unique pointer that takes exclusive ownership of <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">The disposable resource to manage.</param>
        public UniquePtr(T resource)
        {
            ArgumentNullException.ThrowIfNull(resource);
            _resource = resource;
        }

        /// <inheritdoc />
        public T Target
        {
            get
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) == 1, this);
                if (_resource == null) throw new InvalidOperationException("UniquePtr is empty (resource was moved or released).");
                return _resource;
            }
        }

        /// <inheritdoc />
        public bool IsEmpty => _resource == null || Volatile.Read(ref _isDisposed) == 1;

        /// <inheritdoc />
        public IUniquePtr<T> Transfer()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) == 1, this);
            if (_resource == null) throw new InvalidOperationException("Cannot transfer an empty UniquePtr.");

            // Take the resource and clear our own reference to it
            T resourceToMove = _resource;
            _resource = null;

            // We consider this pointer "disposed" now, as it no longer owns anything
            Interlocked.Exchange(ref _isDisposed, 1);
            GC.SuppressFinalize(this);

            return new UniquePtr<T>(resourceToMove);
        }

        /// <inheritdoc />
        public T Release()
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) == 1, this);
            if (_resource == null) throw new InvalidOperationException("Cannot release an empty UniquePtr.");

            T resourceToRelease = _resource;
            _resource = null;

            Interlocked.Exchange(ref _isDisposed, 1);
            GC.SuppressFinalize(this);

            return resourceToRelease;
        }

        // The Full Dispose Pattern
        ~UniquePtr()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the currently owned resource, if any, and marks this pointer as disposed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // If this hasn't been disposed, and it still owns a resource, destroy it.
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                _resource?.Dispose();
                _resource = null;
            }
        }
    }
}
