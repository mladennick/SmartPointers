using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPointers.Interfaces
{
    // 'out T' makes it covariant, so an ISmartPtr<Derived> can be passed as an ISmartPtr<Base>
    public interface ISmartPtr<out T> : IDisposable where T : IDisposable
    {
        /// <summary>Access the underlying unmanaged resource.</summary>
        /// <exception cref="ObjectDisposedException">Thrown if the pointer has given up ownership or been disposed.</exception>
        T Target { get; }

        /// <summary>Checks if the pointer is still managing a valid resource.</summary>
        bool IsEmpty { get; }
    }
}
