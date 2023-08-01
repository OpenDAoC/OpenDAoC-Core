using System;
using System.Threading;

namespace DOL.GS
{
    // A 'SubZone' inside a 'Zone', holding linked lists of 'GameObject'.
    // To preserve thread safety, the list returned by 'GetObjects' must be iterated with 'SubZoneObjectReader' and modified with 'SubZoneObjectWriter'.
    // Modification during iteration on the same thread isn't supported.
    public class SubZone
    {
        public Zone ParentZone { get; private set; }
        private LightConcurrentLinkedList<GameObject>[] _objects = new LightConcurrentLinkedList<GameObject>[Enum.GetValues(typeof(eGameObjectType)).Length];

        public SubZone(Zone parentZone)
        {
            ParentZone = parentZone;

            for (int i = 0; i < _objects.Length; i++)
                _objects[i] = new();
        }

        public bool AddObjectNode(LightConcurrentLinkedList<GameObject>.Node node)
        {
            return _objects[(byte) node.Item.GameObjectType].AddLast(node);
        }

        public bool RemoveObjectNode(LightConcurrentLinkedList<GameObject>.Node node)
        {
            return _objects[(byte) node.Item.GameObjectType].Remove(node);
        }

        public LightConcurrentLinkedList<GameObject> GetObjects(eGameObjectType objectType)
        {
            return _objects[(byte) objectType];
        }

        public void CheckForRelocation(LightConcurrentLinkedList<GameObject>.Node node)
        {
            ParentZone.CheckForRelocation(node);
        }
    }

    // A wrapper for a 'LightConcurrentLinkedList<GameObject>.Node'.
    public class SubZoneObject
    {
        public LightConcurrentLinkedList<GameObject>.Node Node { get; private set; }
        public SubZone CurrentSubZone { get; set; }
        private int _isChangingSubZone;
        public bool StartSubZoneChange => Interlocked.Exchange(ref _isChangingSubZone, 1) == 0; // Returns true the first time it's called.

        public SubZoneObject(LightConcurrentLinkedList<GameObject>.Node node, SubZone currentSubZone)
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
