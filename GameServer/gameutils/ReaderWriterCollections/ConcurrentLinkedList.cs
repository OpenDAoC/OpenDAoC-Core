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

        public void AddLast(LinkedListNode<T> node)
        {
            _list.AddLast(node);
        }

        public void Remove(LinkedListNode<T> node)
        {
            _list.Remove(node);
        }

        public Reader GetReader()
        {
            return new Reader(this);
        }

        public Writer GetWriter()
        {
            return new Writer(this);
        }

        public Writer TryGetWriter(out bool success)
        {
            return new Writer(this, out success);
        }

        // A disposable iterator-like class taking care of the locking. Only iterations from first to last nodes are allowed, and the lock can't be upgraded.
        public sealed class Reader : IDisposable
        {
            LinkedListNode<T> _current;
            ConcurrentLinkedList<T> _list;
            private bool _hasLock;

            public Reader(ConcurrentLinkedList<T> list)
            {
                _list = list;
                _list._lock.EnterReadLock();
                _hasLock = true;
                _current = _list._list.First;
            }

            public LinkedListNode<T> Current()
            {
                return _current;
            }

            public LinkedListNode<T> Next()
            {
                _current = _current.Next;
                return _current ?? null;
            }

            public void MoveTo(LinkedListNode<T> node)
            {
                _current = node;
            }

            public void Dispose()
            {
                if (_hasLock)
                    _list._lock.ExitReadLock();
            }
        }

        // A disposable class taking care of acquiring and disposing a write lock.
        public sealed class Writer : IDisposable
        {
            private const int WRITE_LOCK_TIMEOUT = 3;

            private ConcurrentLinkedList<T> _list;
            private bool _hasLock;

            public Writer(ConcurrentLinkedList<T> list)
            {
                _list = list;
                _list._lock.EnterWriteLock();
                _hasLock = true;
            }

            public Writer(ConcurrentLinkedList<T> list, out bool success)
            {
                _list = list;
                _hasLock = _list._lock.TryEnterWriteLock(WRITE_LOCK_TIMEOUT);
                success = _hasLock;
            }

            public void Dispose()
            {
                if (_hasLock)
                    _list._lock.ExitWriteLock();
            }
        }
    }
}
