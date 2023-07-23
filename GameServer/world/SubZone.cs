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
        private int _isSubZoneChangeBeingHandled; // Used to prevent multiple reader threads from adding it to the 'EntityManager' more than once.

        public SubZoneObject(LightConcurrentLinkedList<GameObject>.Node node, SubZone currentSubZone)
        {
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

    // Temporary objects to be added to 'EntityManager' and consummed by 'ZoneService', representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone : IManagedEntity
    {
        public LightConcurrentLinkedList<GameObject>.Node Node { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new();
        public bool AllowReuseByEntityManager => true;

        private ObjectChangingSubZone(LightConcurrentLinkedList<GameObject>.Node node, Zone destinationZone, SubZone destinationSubZone)
        {
            Initialize(node, destinationZone, destinationSubZone);
        }

        public static void Create(LightConcurrentLinkedList<GameObject>.Node node, Zone destinationZone, SubZone destinationSubZone)
        {
            if (EntityManager.TryReuse(EntityManager.EntityType.ObjectChangingSubZone, out ObjectChangingSubZone objectChangingSubZone))
                objectChangingSubZone.Initialize(node, destinationZone, destinationSubZone);
            else
            {
                objectChangingSubZone = new(node, destinationZone, destinationSubZone);
                EntityManager.Add(EntityManager.EntityType.ObjectChangingSubZone, objectChangingSubZone);
            }
        }

        private void Initialize(LightConcurrentLinkedList<GameObject>.Node node, Zone destinationZone, SubZone destinationSubZone)
        {
            Node = node;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
        }
    }
}
