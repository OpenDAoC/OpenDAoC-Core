using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class ConcurrentLinkedList<T> : IEnumerable<T> where T : class
    {
        private LinkedList<T> _list = new();
        private ReaderWriterLockSlim _lock = new();
        public int Count => _list.Count;
        public bool Any => _list.Count > 0;

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

        public SimpleDisposableLock GetLock()
        {
            return new SimpleDisposableLock(_lock);
        }

        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
