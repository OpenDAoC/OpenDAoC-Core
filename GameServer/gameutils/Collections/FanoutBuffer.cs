using System;
using System.Threading;

namespace DOL.GS
{
    // A high-throughput buffer designed for multi-producer, single-consumer scenarios.
    // `Add` is lock-free unless the buffer needs to be resized.
    // `DrainTo` takes a delegate and automatically resets the internal index for the next consumers.
    // The buffer is never cleared; elements are overwritten but older references remain until replaced.
    // Avoid storing large objects or long-lived references that could hold memory unnecessarily.
    public sealed class FanoutBuffer<T>
    {
        private T[] _buffer;
        private int _writeIndex;
        private readonly Lock _resizeLock = new();

        public int Count => Volatile.Read(ref _writeIndex);

        public FanoutBuffer(int initialCapacity = 1024)
        {
            _buffer = new T[initialCapacity];
            _writeIndex = 0;
        }

        public void Add(T item)
        {
            int index = Interlocked.Increment(ref _writeIndex) - 1;

            if (index >= _buffer.Length)
            {
                lock (_resizeLock)
                {
                    if (index >= _buffer.Length)
                    {
                        int newSize = _buffer.Length * 2;

                        while (newSize <= index)
                            newSize *= 2;

                        T[] newArray = new T[newSize];
                        Array.Copy(_buffer, newArray, _buffer.Length);
                        _buffer = newArray;
                    }
                }
            }

            _buffer[index] = item;
        }

        public void DrainTo(Action<T> action)
        {
            int count = Volatile.Read(ref _writeIndex);

            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
                action(_buffer[i]);

            Volatile.Write(ref _writeIndex, 0);
        }

        public void DrainTo<TState>(Action<T, TState> action, TState state)
        {
            int count = Volatile.Read(ref _writeIndex);

            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
                action(_buffer[i], state);

            Volatile.Write(ref _writeIndex, 0);
        }
    }
}
