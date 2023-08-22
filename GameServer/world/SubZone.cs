using System;
using System.Collections.Generic;
using System.Threading;
using DOL.GS.Scripts;

namespace DOL.GS
{
    // A 'SubZone' inside a 'Zone', holding linked lists of 'GameObject'.
    // To preserve thread safety, the list returned by 'GetObjects' must be iterated with 'ConcurrentLinkedList.Reader'.
    // 'ConcurrentLinkedList.Writer' must be acquired before calling `AddObjectNode` or `RemoveObjectNode`.
    // Modification during iteration on the same thread isn't supported.
    public class SubZone
    {
        public Zone ParentZone { get; private set; }
        private ConcurrentLinkedList<GameObject>[] _objects = new ConcurrentLinkedList<GameObject>[Enum.GetValues(typeof(eGameObjectType)).Length];

        public SubZone(Zone parentZone)
        {
            ParentZone = parentZone;

            for (int i = 0; i < _objects.Length; i++)
                _objects[i] = new();
        }

        public void AddObjectNode(LinkedListNode<GameObject> node)
        {
            _objects[(byte) node.Value.GameObjectType].AddLast(node);
        }

        public void RemoveObjectNode(LinkedListNode<GameObject> node)
        {
            _objects[(byte) node.Value.GameObjectType].Remove(node);
        }

        public ConcurrentLinkedList<GameObject> GetObjects(eGameObjectType objectType)
        {
            return _objects[(byte) objectType];
        }

        public ConcurrentLinkedList<GameObject>.Reader GetObjectReader(LinkedListNode<GameObject> node)
        {
            return _objects[(byte) node.Value.GameObjectType].GetReader();
        }

        public ConcurrentLinkedList<GameObject>.Writer GetObjectWriter(LinkedListNode<GameObject> node)
        {
            return _objects[(byte) node.Value.GameObjectType].GetWriter();
        }

        public ConcurrentLinkedList<GameObject>.Writer TryGetObjectWriter(LinkedListNode<GameObject> node, out bool success)
        {
            return _objects[(byte) node.Value.GameObjectType].TryGetWriter(out success);
        }

        public void CheckForRelocation(LinkedListNode<GameObject> node)
        {
            ParentZone.CheckForRelocation(node);
        }
    }

    // A wrapper for a 'LinkedListNode<GameObject>'.
    public class SubZoneObject
    {
        public LinkedListNode<GameObject> Node { get; private set; }
        public SubZone CurrentSubZone { get; set; }
        private int _isChangingSubZone;
        public bool StartSubZoneChange => Interlocked.Exchange(ref _isChangingSubZone, 1) == 0; // Returns true the first time it's called.

        public SubZoneObject(LinkedListNode<GameObject> node, SubZone currentSubZone)
        {
            Node = node;
            CurrentSubZone = currentSubZone;
        }

        public void ResetSubZoneChange()
        {
            _isChangingSubZone = 0;
        }

        public void CheckForRelocation()
        {
            CurrentSubZone?.CheckForRelocation(Node);
        }
    }
}
