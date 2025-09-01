using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // A linked list that provides write-locking for safe concurrent modifications.
    // Ensures that add, remove, and move operations are atomic and protected by a lock,
    // while supporting safe enumeration with version checks to detect modifications during iteration.
    public class WriteLockedLinkedList<T> : IEnumerable<LinkedListNode<T>> where T : class
    {
        private LinkedList<T> _list = new();
        private Lock _writeLock = new();
        private int _version;

        public int Count => _list.Count;

        public void AddLast(LinkedListNode<T> node, Action<LinkedListNode<T>> callback)
        {
            lock (_writeLock)
            {
                AddLastUnsafe(node);
                callback(node);
            }
        }

        public void Remove(LinkedListNode<T> node, Action<LinkedListNode<T>> callback)
        {
            lock (_writeLock)
            {
                RemoveUnsafe(node);
                callback(node);
            }
        }

        public static void Move<TState>(LinkedListNode<T> node, WriteLockedLinkedList<T> from, WriteLockedLinkedList<T> to, int fromId, int toId, TState state, Action<LinkedListNode<T>, TState> callback)
        {
            WriteLockedLinkedList<T> first;
            WriteLockedLinkedList<T> second;

            // Deadlock prevention: always acquire locks in the same order based on id
            if (fromId < toId)
            {
                first = from;
                second = to;
            }
            else
            {
                first = to;
                second = from;
            }

            lock (first._writeLock)
            {
                lock (second._writeLock)
                {
                    from.RemoveUnsafe(node);
                    to.AddLastUnsafe(node);
                    callback(node, state);
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<LinkedListNode<T>> IEnumerable<LinkedListNode<T>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void AddLastUnsafe(LinkedListNode<T> node)
        {
            _list.AddLast(node);
            _version++;
        }

        private void RemoveUnsafe(LinkedListNode<T> node)
        {
            _list.Remove(node);
            _version++;
        }

        public struct Enumerator : IEnumerator<LinkedListNode<T>>
        {
            private readonly WriteLockedLinkedList<T> _parent;
            private readonly int _version;

            public LinkedListNode<T> Current { get; private set; }
            readonly object IEnumerator.Current => Current;

            public Enumerator(WriteLockedLinkedList<T> parent)
            {
                _parent = parent;
                _version = parent._version;
                Current = null;
            }

            public bool MoveNext()
            {
                if (_version != _parent._version)
                    throw new InvalidOperationException("Collection was modified during iteration.");

                Current = Current == null ? _parent._list.First : Current.Next;
                return Current != null;
            }

            public void Reset()
            {
                if (_version != _parent._version)
                    throw new InvalidOperationException("Collection was modified during iteration.");

                Current = null;
            }

            public void Dispose()
            {
                Reset();
            }
        }
    }
}
