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
            WriteLockedLinkedList<GameObject>.Move(node, otherSubZone._objects[(int) objectType], _objects[(int) objectType], otherSubZone._id, _id, this, OnMoveObject);

            static void OnMoveObject(LinkedListNode<GameObject> node, SubZone subZone)
            {
                node.Value.SubZoneObject.CurrentSubZone = subZone;
            }
        }

        public WriteLockedLinkedList<GameObject> this[eGameObjectType objectType] => _objects[(int) objectType];
    }

    // A wrapper for a 'LinkedListNode<GameObject>'.
    public class SubZoneObject
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private SubZoneTransition _currentSubZoneTransition { get; set; }

        public LinkedListNode<GameObject> Node { get; }
        public SubZone CurrentSubZone { get; set; }

        public SubZoneObject(GameObject obj)
        {
            Node = new(obj);
            _currentSubZoneTransition = new();
        }

        public bool InitiateSubZoneTransition(Zone destinationZone, SubZone destinationSubZone)
        {
            // If there's a pending subzone transition already, update it.
            if (_currentSubZoneTransition?.ServiceObjectId.IsSet == true)
            {
                _currentSubZoneTransition.Init(this, destinationZone, destinationSubZone);
                return true;
            }

            // Work around the fact that AddObject is called during server startup, when the game loop thread pool isn't initialized yet.
            SubZoneTransition subZoneTransition = GameLoop.GameLoopTime == 0 ? new() : PooledObjectFactory.GetForTick<SubZoneTransition>();

            if (!ServiceObjectStore.Add(subZoneTransition.Init(this, destinationZone, destinationSubZone)))
            {
                if (log.IsErrorEnabled)
                    log.Error($"SubZoneTransition couldn't be added to ServiceObjectStore. {Node.Value}");

                return false;
            }

            _currentSubZoneTransition = subZoneTransition;
            return true;
        }

        public void OnSubZoneTransition()
        {
            // Only meant to be called by the zone service.

            if (_currentSubZoneTransition == null)
                return;

            _currentSubZoneTransition.ReleasePooledObject();
            ServiceObjectStore.Remove(_currentSubZoneTransition);
            _currentSubZoneTransition = null;
        }

        public void CheckForRelocation()
        {
            Node.Value.CurrentZone?.CheckForRelocation(Node);
        }
    }
}
