using System;

namespace SmartPointers.Interfaces
{
    /// <summary>
    /// Strategy abstraction for custom cleanup policies.
    /// </summary>
    /// <remarks>
    /// TODO: Add optional constructor/factory overloads that accept <see cref="IDeleter{T}"/>
    /// directly when strategy-based deleters become a primary extension point.
    /// </remarks>
    public interface IDeleter<in T> where T : class, IDisposable
    {
        /// <summary>
        /// Releases the resource.
        /// </summary>
        void Delete(T resource);
    }
}
