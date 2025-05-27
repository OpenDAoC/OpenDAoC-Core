using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DOL.GS
{
    // A lock-free, multi-producer single-consumer collection optimized for high-throughput concurrent additions.
    // Allows concurrent, lock-free add operations from multiple producer threads.
    // Designed for a single consumer to drain items; draining and adding must not occur simultaneously.
    // If the internal buffer overflows, excess items are temporarily stored in a concurrent queue and the buffer is automatically resized (never shrinks).

    public sealed class DrainArray<T>
    {
        private T[] _buffer;
        private int _writeIndex;
        private ConcurrentQueue<T> _overflowQueue = new();
        private bool _overflowed;
        private bool _draining;

        public bool Any => Volatile.Read(ref _writeIndex) > 0; // Not accurate.

        public DrainArray(int initialCapacity = 1024)
        {
            _buffer = new T[initialCapacity];
            _writeIndex = 0;
        }

        public void Add(T item)
        {
            if (Volatile.Read(ref _draining))
                throw new InvalidOperationException($"Cannot {nameof(Add)} while {nameof(DrainTo)} is in progress.");

            int index = Interlocked.Increment(ref _writeIndex) - 1;

            if (index < _buffer.Length)
                _buffer[index] = item;
            else
            {
                _overflowQueue.Enqueue(item);
                _overflowed = true;
            }
        }

        public void DrainTo(Action<T> action)
        {
            DrainTo(static (item, act) => act(item), action);
        }

        public void DrainTo<TState>(Action<T, TState> action, TState state)
        {
            if (Interlocked.Exchange(ref _draining, true) != false)
                throw new InvalidOperationException($"Concurrent {nameof(DrainTo)} detected.");

            try
            {
                int count = Interlocked.Exchange(ref _writeIndex, 0);

                if (count == 0)
                    return;

                if (_overflowed)
                {
                    for (int i = 0; i < _buffer.Length; i++)
                        action(_buffer[i], state);

                    int overflowCount = 0;

                    while (_overflowQueue.TryDequeue(out T result))
                    {
                        overflowCount++;
                        action(result, state);
                    }

                    ResizeBuffer(overflowCount);
                    _overflowed = false;
                    return;
                }

                for (int i = 0; i < count; i++)
                    action(_buffer[i], state);

                Array.Clear(_buffer);
            }
            finally
            {
                Volatile.Write(ref _draining, false);
            }
        }

        private void ResizeBuffer(int minToAdd)
        {
            int newSize = _buffer.Length * 2;

            while (newSize <= minToAdd)
                newSize *= 2;

            _buffer = new T[newSize];
        }
    }
}
