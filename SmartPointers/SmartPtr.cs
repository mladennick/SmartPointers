using SmartPointers.Implementations;
using SmartPointers.Interfaces;
using System;

namespace SmartPointers
{
    /// <summary>
    /// Factory helpers inspired by C++ <c>make_shared</c> and <c>make_unique</c>.
    /// </summary>
    public static class SmartPtr
    {
        /// <summary>
        /// Creates a new <see cref="ISharedPtr{T}"/> from an existing resource instance.
        /// </summary>
        public static ISharedPtr<T> MakeShared<T>(T resource) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(resource);
            return new SharedPtr<T>(resource);
        }

        /// <summary>
        /// Creates a new <see cref="ISharedPtr{T}"/> from an existing resource instance
        /// with a custom cleanup action.
        /// </summary>
        public static ISharedPtr<T> MakeShared<T>(T resource, Action<T> deleter) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(deleter);
            return new SharedPtr<T>(resource, deleter);
        }

        /// <summary>
        /// Creates a new <see cref="ISharedPtr{T}"/> by invoking <paramref name="factory"/>.
        /// </summary>
        public static ISharedPtr<T> MakeShared<T>(Func<T> factory) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(factory);
            T resource = factory();
            ArgumentNullException.ThrowIfNull(resource);
            return new SharedPtr<T>(resource);
        }

        /// <summary>
        /// Creates a new <see cref="ISharedPtr{T}"/> by invoking <paramref name="factory"/>
        /// and registering a custom cleanup action.
        /// </summary>
        public static ISharedPtr<T> MakeShared<T>(Func<T> factory, Action<T> deleter) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(deleter);
            T resource = factory();
            ArgumentNullException.ThrowIfNull(resource);
            return new SharedPtr<T>(resource, deleter);
        }

        /// <summary>
        /// Creates a new <see cref="IUniquePtr{T}"/> from an existing resource instance.
        /// </summary>
        public static IUniquePtr<T> MakeUnique<T>(T resource) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(resource);
            return new UniquePtr<T>(resource);
        }

        /// <summary>
        /// Creates a new <see cref="IUniquePtr{T}"/> from an existing resource instance
        /// with a custom cleanup action.
        /// </summary>
        public static IUniquePtr<T> MakeUnique<T>(T resource, Action<T> deleter) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(deleter);
            return new UniquePtr<T>(resource, deleter);
        }

        /// <summary>
        /// Creates a new <see cref="IUniquePtr{T}"/> by invoking <paramref name="factory"/>.
        /// </summary>
        public static IUniquePtr<T> MakeUnique<T>(Func<T> factory) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(factory);
            T resource = factory();
            ArgumentNullException.ThrowIfNull(resource);
            return new UniquePtr<T>(resource);
        }

        /// <summary>
        /// Creates a new <see cref="IUniquePtr{T}"/> by invoking <paramref name="factory"/>
        /// and registering a custom cleanup action.
        /// </summary>
        public static IUniquePtr<T> MakeUnique<T>(Func<T> factory, Action<T> deleter) where T : class, IDisposable
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(deleter);
            T resource = factory();
            ArgumentNullException.ThrowIfNull(resource);
            return new UniquePtr<T>(resource, deleter);
        }
    }
}
