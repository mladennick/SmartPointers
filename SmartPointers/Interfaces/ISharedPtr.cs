using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPointers.Interfaces
{
    public interface ISharedPtr<out T> : ISmartPtr<T> where T : IDisposable
    {
        /// <summary>Creates a new shared pointer to the same resource, incrementing the reference count.</summary>
        ISharedPtr<T> Share();

        /// <summary>Gets the current number of active shared pointers.</summary>
        int UseCount { get; }
    }
}
