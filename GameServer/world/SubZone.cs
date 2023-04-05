using System;
using System.Threading;
using static DOL.GS.Zone;

namespace DOL.GS
{
    // A 'SubZone' inside a 'Zone', holding linked lists of 'SubZoneObject'.
    // To preserve thread safety, the list returned by 'GetObjects' must be iterated with 'SubZoneObjectReader' and modified with 'SubZoneObjectWriter'.
    // Modification during iteration on the same thread isn't supported.
    public class SubZone
    {
        public Zone ParentZone { get; private set; }
        private LightConcurrentLinkedList<SubZoneObject>[] _objects = new LightConcurrentLinkedList<SubZoneObject>[Enum.GetValues(typeof(eGameObjectType)).Length];

        public SubZone(Zone parentZone)
        {
            ParentZone = parentZone;

            for (int i = 0; i < _objects.Length; i++)
                _objects[i] = new LightConcurrentLinkedList<SubZoneObject>();
        }

        public bool AddObjectNode(LightConcurrentLinkedList<SubZoneObject>.Node node, eGameObjectType objectType)
        {
            return _objects[(byte)objectType].AddLast(node);
        }

        public bool RemoveObjectNode(LightConcurrentLinkedList<SubZoneObject>.Node node, eGameObjectType objectType)
        {
            return _objects[(byte)objectType].Remove(node);
        }

        public LightConcurrentLinkedList<SubZoneObject> GetObjects(eGameObjectType objectType)
        {
            return _objects[(byte)objectType];
        }
    }

    // A wrapper for a 'GameObject'.
    public class SubZoneObject
    {
        public GameObject Object { get; private set; }
        public SubZone CurrentSubZone { get; set; }
        private int _isChangingSubZone; // Used to prevent multiple reader threads from adding it to the 'EntityManager' more than once.

        public SubZoneObject(GameObject gameObject, SubZone currentSubZone)
        {
            Object = gameObject;
            CurrentSubZone = currentSubZone;
        }

        public bool IsChangingSubZone
        {
            get => Interlocked.Exchange(ref _isChangingSubZone, 1) == 1;
            set => _isChangingSubZone = value ? 1 : 0;
        }
    }

    // Temporary objects to be added to the 'EntityManager' and consummed by the 'ZoneService', representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone
    {
        public LightConcurrentLinkedList<SubZoneObject>.Node SubZoneObject { get; private set; }
        public eGameObjectType ObjectType { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }

        public ObjectChangingSubZone(LightConcurrentLinkedList<SubZoneObject>.Node subZoneObject, eGameObjectType objectType, Zone destinationZone, SubZone destinationSubZone)
        {
            SubZoneObject = subZoneObject;
            ObjectType = objectType;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
        }
    }
}
