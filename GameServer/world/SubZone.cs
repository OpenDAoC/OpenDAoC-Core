using System;
using System.Threading;
using static DOL.GS.Zone;

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
                _objects[i] = new LightConcurrentLinkedList<GameObject>();
        }

        public bool AddObjectNode(LightConcurrentLinkedList<GameObject>.Node node, eGameObjectType objectType)
        {
            return _objects[(byte) objectType].AddLast(node);
        }

        public bool RemoveObjectNode(LightConcurrentLinkedList<GameObject>.Node node, eGameObjectType objectType)
        {
            return _objects[(byte) objectType].Remove(node);
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
        public eGameObjectType ObjectType { get; private set; }
        public SubZone CurrentSubZone { get; set; }
        private int _isSubZoneChangeBeingHandled; // Used to prevent multiple reader threads from adding it to the 'EntityManager' more than once.

        public SubZoneObject(LightConcurrentLinkedList<GameObject>.Node node, eGameObjectType objectType, SubZone currentSubZone)
        {
            ObjectType = objectType;
            Node = node;
            CurrentSubZone = currentSubZone;
        }

        public bool IsSubZoneChangeBeingHandled
        {
            get => Interlocked.Exchange(ref _isSubZoneChangeBeingHandled, 1) == 1; // Returns false the first time it's called.
            set => _isSubZoneChangeBeingHandled = value ? 1 : 0;
        }

        public void CheckForRelocation()
        {
            CurrentSubZone?.CheckForRelocation(Node);
        }
    }

    // Temporary objects to be added to the 'EntityManager' and consummed by the 'ZoneService', representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone : IManagedEntity
    {
        public LightConcurrentLinkedList<GameObject>.Node Node { get; private set; }
        public eGameObjectType ObjectType { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new();

        public ObjectChangingSubZone(LightConcurrentLinkedList<GameObject>.Node node, eGameObjectType objectType, Zone destinationZone, SubZone destinationSubZone)
        {
            Node = node;
            ObjectType = objectType;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
        }
    }
}
