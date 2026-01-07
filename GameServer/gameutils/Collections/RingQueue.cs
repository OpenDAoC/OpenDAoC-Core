using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DOL.GS
{
    public class RingQueue<T> : IEnumerable<T>
    {
        private T[] _buffer;
        private int _head, _tail;

        public int Count { get; private set; }

        public RingQueue(int capacity = 8)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero");

            _buffer = new T[capacity];
        }

        public void Enqueue(T item)
        {
            if (Count == _buffer.Length)
                Grow();

            _buffer[_tail] = item;
            _tail = (_tail + 1) % _buffer.Length;
            Count++;
        }

        public T Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("Queue is empty");

            return DequeueInternal();
        }

        public bool TryDequeue(out T result)
        {
            if (Count == 0)
            {
                result = default;
                return false;
            }

            result = DequeueInternal();
            return true;
        }

        private T DequeueInternal()
        {
            T item = _buffer[_head];

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _buffer[_head] = default;

            _head = (_head + 1) % _buffer.Length;
            Count--;
            return item;
        }

        public T Peek(int offset)
        {
            if (offset >= Count || offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset is out of range");

            return PeekInternal(offset);
        }

        public bool TryPeek(int offset, out T result)
        {
            if (offset >= Count || offset < 0)
            {
                result = default;
                return false;
            }

            result = PeekInternal(offset);
            return true;
        }

        private T PeekInternal(int offset)
        {
            return _buffer[(_head + offset) % _buffer.Length];
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Array.Clear(_buffer, 0, _buffer.Length);

            _head = 0;
            _tail = 0;
            Count = 0;
        }

        private void Grow()
        {
            int newCapacity = _buffer.Length * 2;
            T[] newBuf = new T[newCapacity];

            if (_head < _tail)
                Array.Copy(_buffer, _head, newBuf, 0, Count);
            else
            {
                int headLen = _buffer.Length - _head;
                Array.Copy(_buffer, _head, newBuf, 0, headLen);
                Array.Copy(_buffer, 0, newBuf, headLen, _tail);
            }

            _buffer = newBuf;
            _head = 0;
            _tail = Count;
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly RingQueue<T> _queue;
            private int _index;
            private T _current;

            public readonly T Current => _current;

            readonly object IEnumerator.Current => _current;

            public Enumerator(RingQueue<T> queue)
            {
                _queue = queue;
                _index = -1;
                _current = default!;
            }

            public bool MoveNext()
            {
                int next = _index + 1;

                if (next >= _queue.Count)
                    return false;

                _index = next;
                _current = _queue.Peek(next);
                return true;
            }

            public void Reset()
            {
                _index = -1;
                _current = default!;
            }

            public readonly void Dispose() { }
        }
    }
}
