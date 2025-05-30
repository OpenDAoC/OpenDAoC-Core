using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // A 'SubZone' inside a 'Zone', holding linked lists of 'GameObject'.
    public class SubZone
    {
        private static int _nextId = 0;
        private readonly WriteLockedLinkedList<GameObject>[] _objects = new WriteLockedLinkedList<GameObject>[Enum.GetValues<eGameObjectType>().Length];
        private readonly int _id; // Internal ID for locking order, non-deterministic.

        public Zone ParentZone { get; }

        public SubZone(Zone parentZone)
        {
            _id = Interlocked.Increment(ref _nextId);
            ParentZone = parentZone;

            for (int i = 0; i < _objects.Length; i++)
                _objects[i] = new();
        }

        public void AddObject(LinkedListNode<GameObject> node)
        {
            if (node.Value.SubZoneObject.CurrentSubZone == this)
                throw new ArgumentException("Object already in this subzone", nameof(node));

            _objects[(int) node.Value.GameObjectType].AddLast(node, OnAddObject);

            void OnAddObject(LinkedListNode<GameObject> node)
            {
                node.Value.SubZoneObject.CurrentSubZone = this;
            }
        }

        public void RemoveObject(LinkedListNode<GameObject> node)
        {
            if (node.Value.SubZoneObject.CurrentSubZone != this)
                throw new ArgumentException("Object not in this subzone", nameof(node));

            _objects[(int) node.Value.GameObjectType].Remove(node, OnRemoveObject);

            void OnRemoveObject(LinkedListNode<GameObject> node)
            {
                node.Value.SubZoneObject.CurrentSubZone = null;
            }
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
            WriteLockedLinkedList<GameObject>.Move(node, otherSubZone._objects[(int) objectType], _objects[(int) objectType], otherSubZone._id, _id, OnMoveObject);

            void OnMoveObject(LinkedListNode<GameObject> node)
            {
                node.Value.SubZoneObject.CurrentSubZone = this;
            }
        }

        public WriteLockedLinkedList<GameObject> this[eGameObjectType objectType] => _objects[(int) objectType];
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
