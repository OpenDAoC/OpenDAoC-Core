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
            _lock.EnterWriteLock();

            try
            {
                _list.AddLast(node);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Remove(LinkedListNode<T> node)
        {
            _lock.EnterWriteLock();

            try
            {
                _list.Remove(node);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Reader GetReader()
        {
            return new Reader(this);
        }

        // A disposable iterator-like class taking care of the locking. Only iterations from first to last nodes are allowed, and the lock can't be upgraded.
        public sealed class Reader : IDisposable
        {
            LinkedListNode<T> _current;
            ConcurrentLinkedList<T> _list;

            public Reader(ConcurrentLinkedList<T> list)
            {
                _list = list;
                _list._lock.EnterReadLock();
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
                _list._lock.ExitReadLock();
            }
        }
    }
}
