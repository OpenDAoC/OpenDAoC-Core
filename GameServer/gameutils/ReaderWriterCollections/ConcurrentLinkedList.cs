using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class ConcurrentLinkedList<T> : IEnumerable<LinkedListNode<T>> where T : class
    {
        private LinkedList<T> _list = new();
        private ReaderWriterLockSlim _lock = new();

        public SimpleDisposableLock Lock { get; }
        public int Count => _list.Count;
        public bool Any => _list.Count > 0;

        public ConcurrentLinkedList()
        {
            Lock = new(_lock);
        }

        public void AddLast(LinkedListNode<T> node)
        {
            if (_lock.IsWriteLockHeld)
            {
                _list.AddLast(node);
                return;
            }

            _lock.EnterWriteLock();

            try
            {
                _list.AddLast(node);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Remove(LinkedListNode<T> node)
        {
            if (_lock.IsWriteLockHeld)
            {
                _list.Remove(node);
                return;
            }

            _lock.EnterWriteLock();

            try
            {
                _list.Remove(node);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_list, Lock);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<LinkedListNode<T>> IEnumerable<LinkedListNode<T>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public sealed class Enumerator : IEnumerator<LinkedListNode<T>>
        {
            private LinkedList<T> _list;
            private SimpleDisposableLock _lock;

            public LinkedListNode<T> Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(LinkedList<T> list, SimpleDisposableLock @lock)
            {
                _list = list;
                _lock = @lock;
                _lock.EnterReadLock();
            }

            public bool MoveNext()
            {
                Current = Current == null ? _list.First : Current.Next;
                return Current != null;
            }

            public void Reset()
            {
                Current = null;
                _lock.Dispose();
            }

            public void Dispose()
            {
                _lock.Dispose();
            }
        }
    }
}
