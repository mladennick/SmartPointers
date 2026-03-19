using SmartPointers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SmartPointers.Implementations
{
    /// <summary>
    /// Non-owning pointer associated with a <see cref="SharedPtr{T}"/> control block.
    /// It can atomically upgrade to a strong owner while references are still alive.
    /// </summary>
    public sealed class WeakPtr<T> : IWeakPtr<T> where T : class, IDisposable
    {
        private ControlBlock<T>? _controlBlock;
        private int _isDisposed;

        /// <summary>
        /// Initializes a weak pointer bound to the same control block used by shared owners.
        /// </summary>
        /// <param name="block">The shared control block backing the resource lifetime.</param>
        internal WeakPtr(ControlBlock<T> block)
        {
            ArgumentNullException.ThrowIfNull(block);
            _controlBlock = block;
        }

        /// <summary>
        /// Gets whether this weak pointer can no longer be upgraded.
        /// </summary>
        /// <remarks>
        /// A weak pointer is considered expired when it has been disposed itself,
        /// or when the strong reference count reaches zero.
        /// </remarks>
        public bool IsExpired => Volatile.Read(ref _isDisposed) == 1 || Volatile.Read(ref _controlBlock)?.RefCount == 0;

        /// <summary>
        /// Attempts to atomically promote this weak pointer to a new strong owner.
        /// </summary>
        /// <param name="sharedPtr">
        /// The upgraded shared pointer when promotion succeeds; otherwise <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the underlying resource is still alive and promotion succeeds;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this weak pointer instance was already disposed.
        /// </exception>
        public bool TryUpgrade([NotNullWhen(true)] out ISharedPtr<T>? sharedPtr)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed) == 1, this);

            ControlBlock<T>? block = Volatile.Read(ref _controlBlock);
            if (block != null && block.TryAddRef())
            {
                sharedPtr = SharedPtr<T>.CreateFromUpgradedWeakPtr(block);
                return true;
            }

            sharedPtr = null;
            return false;
        }

        /// <summary>
        /// Disposes this weak pointer instance by dropping its reference to the control block.
        /// </summary>
        /// <remarks>
        /// Disposing a weak pointer never disposes the underlying resource.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
            {
                _controlBlock = null;
            }
        }
    }
}
