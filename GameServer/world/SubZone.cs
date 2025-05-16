using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // A 'SubZone' inside a 'Zone', holding linked lists of 'GameObject'.
    public class SubZone
    {
        private static int _nextId = 0;
        private readonly IConcurrentLinkedList<GameObject>[] _objects = new IConcurrentLinkedList<GameObject>[Enum.GetValues<eGameObjectType>().Length];
        private readonly int _id; // Internal ID for locking order, non-deterministic.

        public Zone ParentZone { get; }

        public SubZone(Zone parentZone)
        {
            _id = Interlocked.Increment(ref _nextId);
            ParentZone = parentZone;

            for (int i = 0; i < _objects.Length; i++)
                _objects[i] = ConcurrentLinkedListFactory.Create<GameObject>();
        }

        public void AddObject(LinkedListNode<GameObject> node)
        {
            if (node.Value.SubZoneObject.CurrentSubZone == this)
                throw new ArgumentException("Object already in this subzone", nameof(node));

            using SimpleDisposableLock @lock = _objects[(int) node.Value.GameObjectType].Lock;
            @lock.EnterWriteLock();
            AddObjectUnsafe(node);
        }

        public void RemoveObject(LinkedListNode<GameObject> node)
        {
            if (node.Value.SubZoneObject.CurrentSubZone != this)
                throw new ArgumentException("Object not in this subzone", nameof(node));

            using SimpleDisposableLock @lock = _objects[(int) node.Value.GameObjectType].Lock;
            @lock.EnterWriteLock();
            RemoveObjectUnsafe(node);
        }

        public void AddObjectToThisAndRemoveFromOther(LinkedListNode<GameObject> node, SubZone otherSubZone)
        {
            if (node.Value.SubZoneObject.CurrentSubZone == this)
                throw new ArgumentException("Object already in this subzone", nameof(node));

            if (node.Value.SubZoneObject.CurrentSubZone != otherSubZone)
                throw new ArgumentException("Object not in the other subzone", nameof(otherSubZone));

            if (this == otherSubZone)
                throw new ArgumentException("Cannot move object to the same subzone", nameof(otherSubZone));

            eGameObjectType objectType = node.Value.GameObjectType;
            SubZone first;
            SubZone second;

            // Deadlock prevention: always acquire locks in the same order based on ID.
            if (_id < otherSubZone._id)
            {
                first = this;
                second = otherSubZone;
            }
            else
            {
                first = otherSubZone;
                second = this;
            }

            // Acquire locks on both lists. We want the removal and addition to happen at the same time from a reader's point of view.
            using SimpleDisposableLock firstLock = first._objects[(int) objectType].Lock;
            @firstLock.EnterWriteLock();
            using SimpleDisposableLock secondLock = second._objects[(int) objectType].Lock;
            @secondLock.EnterWriteLock();

            // Remove from other, add to this.
            otherSubZone.RemoveObjectUnsafe(node);
            AddObjectUnsafe(node);
        }

        public IConcurrentLinkedList<GameObject> this[eGameObjectType objectType] => _objects[(int) objectType];

        private void AddObjectUnsafe(LinkedListNode<GameObject> node)
        {
            _objects[(int) node.Value.GameObjectType].AddLast(node);
            node.Value.SubZoneObject.CurrentSubZone = this;
        }

        private void RemoveObjectUnsafe(LinkedListNode<GameObject> node)
        {
            _objects[(int) node.Value.GameObjectType].Remove(node);
            node.Value.SubZoneObject.CurrentSubZone = null;
        }
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
