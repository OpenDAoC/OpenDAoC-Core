using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class ConcurrentSortedList<T, U> : IEnumerable<KeyValuePair<T, U>> where T : class
    {
        private SortedList<T, U> _list = new();
        private ReaderWriterLockSlim _lock = new();
        public int Count => _list.Count;
        public bool Any => _list.Count > 0;

        public void Add(T key, U value)
        {
            if (_lock.IsWriteLockHeld)
            {
                _list.Add(key, value);
                return;
            }

            _lock.EnterWriteLock();

            try
            {
                _list.Add(key, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Remove(T key)
        {
            if (_lock.IsWriteLockHeld)
            {
                _list.Remove(key);
                return;
            }

            _lock.EnterWriteLock();

            try
            {
                _list.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerator<KeyValuePair<T, U>> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public sealed class Enumerator : IEnumerator<KeyValuePair<T, U>>
        {
            private SortedList<T, U> _list;
            private SimpleDisposableLock _lock;
            private int _index = -1;

            public KeyValuePair<T, U> Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(SortedList<T, U> list, ReaderWriterLockSlim @lock)
            {
                _list = list;
                _lock = new(@lock);
                _lock.EnterReadLock();
            }

            public bool MoveNext()
            {
                return false;
            }

            public void Reset()
            {
                Current = default;
                _index = -1;
                _lock.Dispose();
            }

            public void Dispose()
            {
                _lock.Dispose();
            }
        }
    }
}
