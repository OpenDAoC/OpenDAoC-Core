using System;
using System.Threading;

namespace DOL
{
    // A wrapper for a `ReaderWriterLockSlim` implementing `IDisposable`.
    // Recursion and upgrades are not supported.
    public class SimpleDisposableLock : IDisposable
    {
        private ReaderWriterLockSlim _lock;
        private LockState _lockState;

        public SimpleDisposableLock()
        {
            _lock = new();
        }

        public SimpleDisposableLock(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
        }

        public void LockRead()
        {
            _lock.EnterReadLock();
            _lockState = LockState.READ;
        }

        public void LockWrite()
        {
            _lock.EnterWriteLock();
            _lockState = LockState.WRITE;
        }

        public bool TryLockWrite()
        {
            bool hasLock = _lock.TryEnterWriteLock(0);

            if (hasLock)
                _lockState = LockState.WRITE;

            return hasLock;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (IsSet(LockState.READ))
                _lock.ExitReadLock();
            else if (IsSet(LockState.WRITE))
                _lock.ExitWriteLock();

            _lockState = LockState.NONE;
        }

        protected bool IsSet(LockState flag)
        {
            return (_lockState & flag) == flag;
        }

        [Flags]
        protected enum LockState
        {
            NONE = 0,
            READ = 1,
            WRITE = 2,
        }
    }
}
