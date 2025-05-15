using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // A 'SubZone' inside a 'Zone', holding linked lists of 'GameObject'.
    public class SubZone
    {
        private Dictionary<eGameObjectType, IConcurrentLinkedList<GameObject>> _objects = new();
        public Zone ParentZone { get; }
        public Lock _lock = new();

        public SubZone(Zone parentZone)
        {
            ParentZone = parentZone;

            foreach (eGameObjectType objectType in Enum.GetValues<eGameObjectType>())
                _objects.Add(objectType, ConcurrentLinkedListFactory.Empty<GameObject>());
        }

        public void AddObject(LinkedListNode<GameObject> node)
        {
            if (node.Value.SubZoneObject.CurrentSubZone == this)
                throw new ArgumentException("Object already in this subzone", nameof(node));

            using SimpleDisposableLock @lock = ValidateListAndGetLock(node.Value.GameObjectType);
            @lock.EnterWriteLock();
            AddObjectUnsafe(node);
        }

        public void AddObjectUnsafe(LinkedListNode<GameObject> node)
        {
            _objects[node.Value.GameObjectType].AddLast(node);
            node.Value.SubZoneObject.CurrentSubZone = this;
        }

        public void RemoveObject(LinkedListNode<GameObject> node)
        {
            if (node.Value.SubZoneObject.CurrentSubZone != this)
                throw new ArgumentException("Object not in this subzone", nameof(node));

            using SimpleDisposableLock @lock = ValidateListAndGetLock(node.Value.GameObjectType);
            @lock.EnterWriteLock();
            RemoveObjectUnsafe(node);
        }

        public void RemoveObjectUnsafe(LinkedListNode<GameObject> node)
        {
            _objects[node.Value.GameObjectType].Remove(node);
            node.Value.SubZoneObject.CurrentSubZone = null;

            // This saves about 315MB of RAM on an idle server.
            if (_objects[node.Value.GameObjectType].Count == 0)
                _objects[node.Value.GameObjectType] = ConcurrentLinkedListFactory.Empty<GameObject>();
        }

        public void AddObjectToThisAndRemoveFromOther(LinkedListNode<GameObject> node, SubZone otherSubZone)
        {
            if (node.Value.SubZoneObject.CurrentSubZone == this)
                throw new ArgumentException("Object already in this subzone", nameof(node));

            if (node.Value.SubZoneObject.CurrentSubZone != otherSubZone)
                throw new ArgumentException("Object not in the other subzone", nameof(otherSubZone));

            if (this == otherSubZone)
                throw new ArgumentException("Cannot move object to the same subzone", nameof(otherSubZone));

            // Acquire locks on both lists. We want the removal and addition to happen at the same time from a reader's point of view.
            SimpleDisposableLock thisLock = ValidateListAndGetLock(node.Value.GameObjectType);
            using SimpleDisposableLock otherLock = otherSubZone.ValidateListAndGetLock(node.Value.GameObjectType);
            thisLock.EnterWriteLock();

            // Spin until we can acquire a lock on the other list.
            while (!otherLock.TryEnterWriteLock())
            {
                // Relinquish then reacquire our current lock to prevent dead-locks.
                thisLock.Dispose();
                Thread.Sleep(0);
                thisLock = ValidateListAndGetLock(node.Value.GameObjectType);
                thisLock.EnterWriteLock();
            }

            otherSubZone.RemoveObjectUnsafe(node);
            AddObjectUnsafe(node);
            thisLock.Dispose();
        }

        public SimpleDisposableLock ValidateListAndGetLock(eGameObjectType objectType)
        {
            if (_objects[objectType].IsStaticEmpty)
            {
                lock (_lock)
                {
                    if (_objects[objectType].IsStaticEmpty)
                        _objects[objectType] = ConcurrentLinkedListFactory.Create<GameObject>();
                }
            }

            return _objects[objectType].Lock;
        }

        public IConcurrentLinkedList<GameObject> this[eGameObjectType objectType] => _objects[objectType];
    }

    // A wrapper for a 'LinkedListNode<GameObject>'.
    public class SubZoneObject
    {
        public LinkedListNode<GameObject> Node { get; }
        public SubZone CurrentSubZone { get; set; }
        private int _isChangingSubZone;
        public bool StartSubZoneChange => Interlocked.Exchange(ref _isChangingSubZone, 1) == 0; // Returns true the first time it's called.

        public SubZoneObject(GameObject obj)
        {
            Node = new(obj);
        }

        public void ResetSubZoneChange()
        {
            _isChangingSubZone = 0;
        }

        public void CheckForRelocation()
        {
            Node.Value.CurrentZone?.CheckForRelocation(Node);
        }
    }
}
