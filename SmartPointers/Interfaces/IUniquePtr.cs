using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPointers.Interfaces
{
    public interface IUniquePtr<out T> : ISmartPtr<T> where T : IDisposable
    {
        /// <summary>
        /// Transfers ownership to a new UniquePtr. 
        /// The current pointer becomes empty and can no longer be used.
        /// (Equivalent to std::move in C++).
        /// </summary>
        IUniquePtr<T> Transfer();

        /// <summary>
        /// Relinquishes ownership of the resource and returns it. 
        /// The caller is now responsible for disposing the resource.
        /// </summary>
        T Release();
    }
}
