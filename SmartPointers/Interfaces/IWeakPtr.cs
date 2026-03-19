using System;
using System.Diagnostics.CodeAnalysis;

namespace SmartPointers.Interfaces
{
    /// <summary>
    /// Non-owning smart pointer that can attempt to upgrade to <see cref="ISharedPtr{T}"/>
    /// while the underlying resource is still alive.
    /// </summary>
    /// <typeparam name="T">The disposable resource type.</typeparam>
    public interface IWeakPtr<T> : IDisposable where T : class, IDisposable
    {
        /// <summary>
        /// Attempts to upgrade the weak pointer to a strong shared pointer.
        /// </summary>
        /// <param name="sharedPtr">The upgraded pointer, or <see langword="null"/> if the resource is dead.</param>
        /// <returns><see langword="true"/> if the resource is still alive and was upgraded; otherwise <see langword="false"/>.</returns>
        bool TryUpgrade([NotNullWhen(true)] out ISharedPtr<T>? sharedPtr);

        /// <summary>
        /// Gets whether the underlying shared resource has already expired.
        /// </summary>
        bool IsExpired { get; }
    }
}
