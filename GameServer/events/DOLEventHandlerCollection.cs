using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.Events
{
    public delegate void DOLEventHandler(DOLEvent e, object sender, EventArgs args);

    public sealed class DOLEventHandlerCollection
    {
        private readonly Dictionary<DOLEvent, DOLEventHandlerChain> _chains = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

        public int Count => _chains.Count; // Returns the number of events with registered handlers, not the total number of handlers.

        public void AddHandler(DOLEvent e, DOLEventHandler handler)
        {
            _lock.EnterWriteLock();

            try
            {
                _chains.TryGetValue(e, out DOLEventHandlerChain chain);
                _chains[e] = DOLEventHandlerChain.Subscribe(chain, handler);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void AddHandlerUnique(DOLEvent e, DOLEventHandler handler)
        {
            _lock.EnterWriteLock();

            try
            {
                _chains.TryGetValue(e, out DOLEventHandlerChain chain);
                _chains[e] = DOLEventHandlerChain.SubscribeOnce(chain, handler);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveHandler(DOLEvent e, DOLEventHandler handler)
        {
            _lock.EnterWriteLock();

            try
            {
                if (_chains.TryGetValue(e, out DOLEventHandlerChain chain))
                {
                    chain = DOLEventHandlerChain.Unsubscribe(chain, handler);

                    if (chain == null)
                        _chains.Remove(e);
                    else
                        _chains[e] = chain;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveHandlers(DOLEvent e)
        {
            _lock.EnterWriteLock();

            try
            {
                _chains.Remove(e);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveAllHandlers()
        {
            _lock.EnterWriteLock();

            try
            {
                _chains.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Notify(DOLEvent e)
        {
            Notify(e, null, null);
        }

        public void Notify(DOLEvent e, object sender)
        {
            Notify(e, sender, null);
        }

        public void Notify(DOLEvent e, EventArgs args)
        {
            Notify(e, null, args);
        }

        public void Notify(DOLEvent e, object sender, EventArgs args)
        {
            DOLEventHandlerChain chain = null;
            _lock.EnterReadLock();

            try
            {
                if (!_chains.TryGetValue(e, out chain))
                    return;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            chain.Invoke(e, sender, args);
        }
    }
}
