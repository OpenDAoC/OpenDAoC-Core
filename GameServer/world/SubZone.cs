using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;

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

            static void OnRemoveObject(LinkedListNode<GameObject> node)
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
            WriteLockedLinkedList<GameObject>.Move(node, otherSubZone._objects[(int) objectType], _objects[(int) objectType], otherSubZone._id, _id, this, OnMoveObject);

            static void OnMoveObject(LinkedListNode<GameObject> node, SubZone subZone)
            {
                node.Value.SubZoneObject.CurrentSubZone = subZone;
            }
        }

        public WriteLockedLinkedList<GameObject> this[eGameObjectType objectType] => _objects[(int) objectType];
    }

    // This class serves two purposes:
    // * Wraps a `LinkedListNode<GameObject>`, representing a game object in a `SubZone`.
    // * Serves as an object to be added to `ServiceObjectStore` and consumed by `ZoneService`, to move the game object from one `SubZone` to another.
    public class SubZoneObject : IServiceObject
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private bool _isInitiatingTransition; // Guard. Only true for the duration of the `InitiateSubZoneTransition` method execution.
        private bool _isTransitionQueued;     // Persistent state flag indicating that the object will be processed by `ZoneService`.

        public LinkedListNode<GameObject> Node { get; }
        public SubZone CurrentSubZone { get; set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public ServiceObjectId ServiceObjectId { get; } = new(ServiceObjectType.SubZoneObject);

        public SubZoneObject(GameObject obj)
        {
            Node = new(obj);
        }

        public void InitiateSubZoneTransition(Zone destinationZone, SubZone destinationSubZone)
        {
            // Disallow concurrent calls.
            if (Interlocked.CompareExchange(ref _isInitiatingTransition, true, false) != false)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Concurrent call to {nameof(InitiateSubZoneTransition)} detected for {Node.Value}.");

                return;
            }

            try
            {
                DestinationZone = destinationZone;
                DestinationSubZone = destinationSubZone;

                // Early out if there's already a pending transition.
                if (_isTransitionQueued)
                    return;

                if (!ServiceObjectStore.Add(this))
                {
                    // If adding failed, we must revert the state to be non-pending.
                    if (log.IsErrorEnabled)
                        log.Error($"SubZone transition for {Node.Value} couldn't be added to {nameof(ServiceObjectStore)}.");

                    DestinationZone = null;
                    DestinationSubZone = null;
                    return;
                }

                _isTransitionQueued = true;
            }
            finally
            {
                Interlocked.Exchange(ref _isInitiatingTransition, false);
            }
        }

        public void OnSubZoneTransition()
        {
            // Only meant to be called by the zone service.

            if (!_isTransitionQueued)
                return;

            if (!ServiceObjectStore.Remove(this))
            {
                if (log.IsErrorEnabled)
                    log.Error($"SubZone transition for {Node.Value} couldn't be removed from {nameof(ServiceObjectStore)}.");

                return;
            }

            _isTransitionQueued = false;
            DestinationZone = null;
            DestinationSubZone = null;
        }

        public void CheckForRelocation()
        {
            Node.Value.CurrentZone?.CheckForRelocation(Node);
        }
    }
}
