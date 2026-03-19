using System;
namespace SmartPointers.Interfaces
{
    public interface ISharedPtr<T> : ISmartPtr<T> where T : class, IDisposable
    {
        /// <summary>Creates a new shared pointer to the same resource, incrementing the reference count.</summary>
        ISharedPtr<T> Share();

        /// <summary>
        /// Creates a non-owning weak pointer that can later attempt to upgrade to a shared pointer.
        /// </summary>
        IWeakPtr<T> Weak();

        /// <summary>Gets the current number of active shared pointers.</summary>
        int UseCount { get; }
    }
}
