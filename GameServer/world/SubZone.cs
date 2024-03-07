using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    // A 'SubZone' inside a 'Zone', holding linked lists of 'GameObject'.
    public class SubZone
    {
        private ConcurrentLinkedList<GameObject>[] _objects = new ConcurrentLinkedList<GameObject>[Enum.GetValues(typeof(eGameObjectType)).Length];
        public Zone ParentZone { get; private set; }

        public SubZone(Zone parentZone)
        {
            ParentZone = parentZone;

            for (int i = 0; i < _objects.Length; i++)
                _objects[i] = new();
        }

        public void AddObjectNode(eGameObjectType objectType, LinkedListNode<GameObject> node)
        {
            _objects[(byte) objectType].AddLast(node);
        }

        public void RemoveObjectNode(eGameObjectType objectType, LinkedListNode<GameObject> node)
        {
            _objects[(byte) objectType].Remove(node);
        }

        public SimpleDisposableLock GetLock(eGameObjectType objectType)
        {
            return _objects[(byte) objectType].GetLock();
        }

        public bool Any(eGameObjectType objectType)
        {
            return _objects[(int) objectType].Any;
        }
        
        public ConcurrentLinkedList<GameObject> this[eGameObjectType objectType] => _objects[(byte) objectType];
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
            Node.Value.CurrentZone?.CheckForRelocation(Node);
        }
    }
}
