using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class ConcurrentLinkedList<T> where T : class
    {
        private LinkedList<T> _list = new();
        private ReaderWriterLockSlim _lock = new();
        public int Count => _list.Count;
        public bool Any => _list.Count > 0;

        public void AddLast(LinkedListNode<T> node)
        {
            _list.AddLast(node);
        }

        public void Remove(LinkedListNode<T> node)
        {
            _list.Remove(node);
        }

        public IteratorLock GetIteratorLock()
        {
            return new IteratorLock(this);
        }

        // A disposable iterator-like class taking care of the locking. Only iterations from first to last nodes are allowed, and the lock can't be upgraded.
        public sealed class IteratorLock : IDisposable
        {
            private LinkedListNode<T> _current;
            private ConcurrentLinkedList<T> _list;
            private LockState _lockState;

            public IteratorLock(ConcurrentLinkedList<T> list)
            {
                _list = list;
                _current = _list._list.First;
            }

            public LinkedListNode<T> Current()
            {
                return _current;
            }

            public LinkedListNode<T> Next()
            {
                _current = _current.Next;
                return _current;
            }

            public void MoveTo(LinkedListNode<T> node)
            {
                _current = node;
            }

            public void LockRead()
            {
                _list._lock.EnterReadLock();
                _lockState = LockState.READ;
            }

            public void LockWrite()
            {
                _list._lock.EnterWriteLock();
                _lockState = LockState.WRITE;
            }

            public bool TryLockWrite()
            {
                bool hasLock = _list._lock.TryEnterWriteLock(0);

                if (hasLock)
                    _lockState = LockState.WRITE;

                return hasLock;
            }

            public void Dispose()
            {
                if (IsSet(LockState.READ))
                    _list._lock.ExitReadLock();
                else if (IsSet(LockState.WRITE))
                    _list._lock.ExitWriteLock();

                _lockState = LockState.NONE;
            }

            private bool IsSet(LockState flag)
            {
                return (_lockState & flag) == flag;
            }

            [Flags]
            private enum LockState
            {
                NONE = 0,
                READ = 1,
                WRITE = 2,
            }
        }
    }
}
