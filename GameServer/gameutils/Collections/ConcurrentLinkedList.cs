using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public static class ConcurrentLinkedListFactory
    {
        public static IConcurrentLinkedList<T> Create<T>() where T : class
        {
            return new ConcurrentLinkedList<T>();
        }

        public static IConcurrentLinkedList<T> Empty<T>() where T : class
        {
            return EmptyConcurrentLinkedList<T>.Instance;
        }

        private class ConcurrentLinkedList<T> : IConcurrentLinkedList<T> where T : class
        {
            private LinkedList<T> _list = new();
            private ReaderWriterLockSlim _lock = new();

            public bool IsStaticEmpty => false;
            public SimpleDisposableLock Lock { get; }
            public int Count => _list.Count;

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

        private class EmptyConcurrentLinkedList<T> : IConcurrentLinkedList<T> where T : class
        {
            private Enumerator _enumerator = new();

            public bool IsStaticEmpty => true;
            public SimpleDisposableLock Lock => null;
            public int Count => 0;

            public static EmptyConcurrentLinkedList<T> Instance => Holder.Instance;

            public void AddLast(LinkedListNode<T> node)
            {
                throw new InvalidOperationException("Cannot add to a static empty list.");
            }

            public void Remove(LinkedListNode<T> node)
            {
                throw new InvalidOperationException("Cannot remove from a static empty list.");
            }

            private EmptyConcurrentLinkedList() { }

            public Enumerator GetEnumerator()
            {
                return _enumerator;
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
                public LinkedListNode<T> Current { get; private set; }
                object IEnumerator.Current => Current;

                public Enumerator() { }

                public bool MoveNext()
                {
                    return false;
                }

                public void Reset() { }
                public void Dispose() { }
            }

            private static class Holder
            {
                public static readonly EmptyConcurrentLinkedList<T> Instance = new();
            }
        }
    }

    public interface IConcurrentLinkedList<T> : IEnumerable<LinkedListNode<T>> where T : class
    {
        bool IsStaticEmpty { get; }
        SimpleDisposableLock Lock { get; }
        int Count { get; }
        void AddLast(LinkedListNode<T> node);
        void Remove(LinkedListNode<T> node);
    }
}
