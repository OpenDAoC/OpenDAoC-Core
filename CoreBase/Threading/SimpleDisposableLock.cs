using System;
using System.Threading;

namespace DOL
{
    // A wrapper for a `ReaderWriterLockSlim` implementing `IDisposable`.
    // `Dispose` only takes care of the unlocking; it doesn't invalidate the underlying lock.
    // Upgrades are not allowed.
    // This class' instances aren't meant to be shared by multiple threads.
    public class SimpleDisposableLock : IDisposable
    {
        private ReaderWriterLockSlim _lock;

        public SimpleDisposableLock(LockRecursionPolicy recursionPolicy)
        {
            _lock = new(recursionPolicy);
        }

        public SimpleDisposableLock(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
        }

        public void EnterReadLock()
        {
            _lock.EnterReadLock();
        }

        public void ExitReadLock()
        {
            _lock.ExitReadLock();
        }

        public void EnterWriteLock()
        {
            _lock.EnterWriteLock();
        }

        public bool TryEnterWriteLock()
        {
            return _lock.TryEnterWriteLock(0);
        }

        public void ExitWriteLock()
        {
            _lock.ExitWriteLock();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_lock.IsReadLockHeld)
                _lock.ExitReadLock();
            else if (_lock.IsWriteLockHeld)
                _lock.ExitWriteLock();
        }
    }
}
