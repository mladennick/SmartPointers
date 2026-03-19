using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPointers.Demo
{
    public sealed class FakeImageBuffer : IDisposable
    {
        private static int _nextId;
        private int _disposeFlag;

        public int Id { get; }
        public byte[] Data { get; }

        public FakeImageBuffer(int sizeInMb)
        {
            Id = Interlocked.Increment(ref _nextId);
            Data = new byte[sizeInMb * 1024 * 1024];
            Data[0] = 123;
            Data[^1] = 45;
            Console.WriteLine($"[Buffer {Id}] Created ({sizeInMb} MB)");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeFlag, 1) != 0)
            {
                return;
            }

            Console.WriteLine($"[Buffer {Id}] Disposed exactly once");
        }
    }
}
