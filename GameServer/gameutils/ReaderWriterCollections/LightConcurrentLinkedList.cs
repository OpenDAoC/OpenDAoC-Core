using System;
using System.Threading;

namespace DOL.GS
{
    // A lightweight and partially implemented concurrent linked list. Each of the nodes possess a custom readers-writer lock allowing them to be removed even while another thread is working on the list.
    // This implies that a node could be read, removed by a second thread, then re-added, and finally re-read by the first thread during the same loop; so readers should check for duplicates.
    // Iterating is done using the disposable object returned by 'GetReader()'. Calling 'Remove()' from the thread currently iterating the list isn't supported, and will most likely lead to deadlocks.
    // Alternatively, the custom locks could be dropped and a 'ReaderWriterLockSlim' be used on the whole list instead, but the custom locks appear to offer better performances.
    public class LightConcurrentLinkedList<T> where T : class
    {
        private int _count;
        private OriginNode _origin = new(null);

        public Node First => _origin.Next;
        public Node Last => _origin.Previous;
        public int Count => _count;

        public bool AddLast(Node node)
        {
            // To add a node at the last position, we need to lock the origin and the last node.
            // The lock and unlock order are origin -> last.
            INode iNode = node;
            INode iOrigin = _origin;
            LightReaderWriterLock originLock = iOrigin.Lock;

            if (!originLock.EnterWrite())
                return false;

            INode iOriginPrevious = iOrigin.InnerPrevious;
            LightReaderWriterLock originPreviousLock = null;

            if (_count > 0)
            {
                originPreviousLock = iOriginPrevious.Lock;

                if (!originPreviousLock.EnterWrite())
                {
                    originLock.ExitWrite();
                    return false;
                }

                iNode.InnerPrevious = Last;
                iNode.InnerNext = _origin;
                iOrigin.InnerPrevious.InnerNext = node;
                iOrigin.InnerPrevious = node;
            }
            else
            {
                iOrigin.InnerPrevious = node;
                iOrigin.InnerNext = node;
                iNode.InnerPrevious = _origin;
                iNode.InnerNext = _origin;
            }

            Interlocked.Increment(ref _count);
            originLock.ExitWrite();

            if (originPreviousLock != null)
                originPreviousLock.ExitWrite();

            return true;
        }

        public bool Remove(Node node)
        {
            // To remove a node we need to lock it and its surrounding nodes.
            // The lock and unlock order is current -> next -> previous.
            INode iNode = node;
            LightReaderWriterLock nodeLock = iNode.Lock;

            if (!nodeLock.EnterWrite())
                return false;

            INode iNodeNext = iNode.InnerNext;
            LightReaderWriterLock nodeNextLock = iNodeNext.Lock;

            // To prevent deadlocks, we free our current lock if a contention happens on the next node, which may allow another thread to finish its task.
            // There has to be a better way to do this.
            while (!nodeNextLock.TryEnterWrite())
            {
                nodeLock.ExitWrite();

                do
                {
                    Thread.Sleep(0);
                } while (!nodeLock.TryEnterWrite());

                // Our current next node may no longer be valid.
                iNodeNext = iNode.InnerNext;
                nodeNextLock = iNodeNext.Lock;
            }

            INode iNodePrevious = iNode.InnerPrevious;
            LightReaderWriterLock nodePreviousLock = null;

            // This basically means there are at least two elements in the list (ignoring '_origin').
            if (iNodeNext != iNodePrevious)
            {
                nodePreviousLock = iNodePrevious.Lock;

                if (!nodePreviousLock.EnterWrite())
                {
                    nodeLock.ExitWrite();
                    nodeNextLock.ExitWrite();
                    return false;
                }

                iNodePrevious.InnerNext = iNodeNext;
                iNodeNext.InnerPrevious = iNodePrevious;
                node.Clear();
            }
            else
            {
                _origin.Initialize();
                node.Clear();
            }

            Interlocked.Decrement(ref _count);
            nodeLock.ExitWrite();
            nodeNextLock.ExitWrite();

            if (nodePreviousLock != null)
               nodePreviousLock.ExitWrite();

            return true;
        }

        public Reader GetReader()
        {
            return new Reader(this);
        }

        // A disposable iterator-like class taking care of the locking. Only iterations from first to last nodes are allowed, and the lock can't be upgraded.
        public sealed class Reader : IDisposable
        {
            Node _current;
            LightReaderWriterLock _lock;

            public Reader(LightConcurrentLinkedList<T> list)
            {
                _current = list.First;
            }

            public Node Current()
            {
                if (_current == null)
                    return null;

                Lock(_current);
                return _current;
            }

            public Node Next()
            {
                _current = _current.Next;

                if (_current == null)
                    return null;

                _lock.ExitRead();
                Lock(_current);
                return _current;
            }

            public void Dispose()
            {
                _lock.ExitRead();
            }

            private void Lock(INode node)
            {
                if (node == null)
                    return;

                _lock = node.Lock;
                _lock.EnterRead();
            }
        }

        public class Node : INode
        {
            protected Node _next;
            protected Node _previous;
            protected LightReaderWriterLock _lock = new();

            public T Item { get; set; }
            public Node Next => _next?.IsOrigin == false ? _next : null;
            public Node Previous => _previous?.IsOrigin == false ? _previous : null;
            protected virtual bool IsOrigin => false; // Used to tell if we're at the start or end of the list.

            public Node(T item)
            {
                Item = item;
            }

            internal void Clear()
            {
                _next = null;
                _previous = null;
            }

            LightReaderWriterLock INode.Lock => _lock;

            INode INode.InnerNext
            {
                get => _next;
                set => _next = (Node)value;
            }

            INode INode.InnerPrevious
            {
                get => _previous;
                set => _previous = (Node)value;
            }
        }

        private class OriginNode : Node
        {
            protected override bool IsOrigin => true;

            public OriginNode(T item) : base(item)
            {
                Initialize();
            }

            public void Initialize()
            {
                INode iOrigin = this;
                iOrigin.InnerPrevious = this;
                iOrigin.InnerNext = this;
            }
        }

        // Explicit interface implementation allows the outer classes to have exclusive access to the lock.
        private interface INode
        {
            public LightReaderWriterLock Lock { get; }
            public INode InnerNext { get; set; }
            public INode InnerPrevious { get; set; }
        }
    }

    // A custom readers-writer lock. Appears to offers better performances than 'ReaderWriterLockSlim' when locks are short but frequent.
    public class LightReaderWriterLock
    {
        private const int WRITER_LOCK_TIMEOUT = 5;
        private int _activeReaderCount = 0;
        private int _activeOrPendingWriterCount = 0;
        private object _writerLock = new();

        public void EnterRead()
        {
            while (Interlocked.CompareExchange(ref _activeOrPendingWriterCount, 0, 0) > 0)
                Thread.Sleep(0);

            Interlocked.Increment(ref _activeReaderCount);
        }

        public void ExitRead()
        {
            Interlocked.Decrement(ref _activeReaderCount);
        }

        public bool TryEnterWrite()
        {
            Interlocked.Increment(ref _activeOrPendingWriterCount);

            while (Interlocked.CompareExchange(ref _activeReaderCount, 0, 0) > 0)
                Thread.Sleep(0);

            if (Monitor.TryEnter(_writerLock))
                return true;

            Interlocked.Decrement(ref _activeOrPendingWriterCount);
            return false;
        }

        public bool EnterWrite()
        {
            Interlocked.Increment(ref _activeOrPendingWriterCount);

            while (Interlocked.CompareExchange(ref _activeReaderCount, 0, 0) > 0)
                Thread.Sleep(0);

            if (!Monitor.TryEnter(_writerLock, WRITER_LOCK_TIMEOUT))
            {
                Interlocked.Decrement(ref _activeOrPendingWriterCount);
                return false;
            }

            return true;
        }

        public void ExitWrite()
        {
            Monitor.Exit(_writerLock);
            Interlocked.Decrement(ref _activeOrPendingWriterCount);
        }
    }
}
