using System;

namespace DOL.GS
{
    public abstract class TickPool<T> : TickPoolBase
    {
        protected T[] _pool = new T[INITIAL_CAPACITY];

        public T GetForTick()
        {
            T item;

            if (_used < _logicalSize)
                item = _pool[_used++];
            else
            {
                item = CreateNew();

                if (_used >= _pool.Length)
                    Array.Resize(ref _pool, (int) (_pool.Length * 1.25));

                _pool[_used++] = item;
                _logicalSize = Math.Max(_logicalSize, _used);
            }

            // If the item retrieved from the pool is "dirty", replace it with a new one.
            if (IsDirty(item))
            {
                LogDirtyItemWarning(item);
                item = CreateNew();
                _pool[_used - 1] = item;
            }

            PrepareForUse(item);
            return item;
        }

        protected abstract T CreateNew();
        protected abstract bool IsDirty(T item);
        protected abstract void LogDirtyItemWarning(T item);
        protected abstract void PrepareForUse(T item);

        protected override void OnTrim(int currentSize, int newSize)
        {
            Array.Clear(_pool, newSize, currentSize - newSize);
        }
    }
}
