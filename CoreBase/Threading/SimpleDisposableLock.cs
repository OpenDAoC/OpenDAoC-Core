using System;
using System.Threading;

namespace DOL
{
    // A wrapper for a `ReaderWriterLockSlim`.
    // Call `GetRead` or `GetWrite` with the using keyword.
    // Recursion, upgrades, tries are not supported.
    public class SimpleDisposableLock
    {
        private ReaderWriterLockSlim _lock = new();

        public Read GetRead()
        {
            return new Read(_lock);
        }

        public Write GetWrite()
        {
            return new Write(_lock);
        }

        public sealed class Read : IDisposable
        {
            private ReaderWriterLockSlim _lock;

            public Read(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterReadLock();
            }

            public void Dispose()
            {
                _lock.ExitReadLock();
            }
        }

        public sealed class Write : IDisposable
        {
            private ReaderWriterLockSlim _lock;

            public Write(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterWriteLock();
            }

            public void Dispose()
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
