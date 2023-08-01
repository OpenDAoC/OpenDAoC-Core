using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class ZoneService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(ZoneService);
        private static int _failedAdd;
        private static int _failedRemove;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<ObjectChangingSubZone> list = EntityManager.UpdateAndGetAll<ObjectChangingSubZone>(EntityManager.EntityType.ObjectChangingSubZone, out int lastValidIndex);

            // Remove objects from one sub zone, and add them to another.
            Parallel.For(0, lastValidIndex + 1, i =>
            {
                ObjectChangingSubZone objectChangingSubZone = list[i];

                if (objectChangingSubZone?.EntityManagerId.IsSet != true)
                    return;

                try
                {
                    LightConcurrentLinkedList<GameObject>.Node node = objectChangingSubZone.Node;
                    SubZoneObject subZoneObject = objectChangingSubZone.SubZoneObject;
                    Zone currentZone = subZoneObject.CurrentSubZone?.ParentZone;
                    Zone destinationZone = objectChangingSubZone.DestinationZone;
                    bool changingZone = currentZone != destinationZone;

                    // Remove object from subzone.
                    if (currentZone != null)
                    {
                        // Abord if we can't remove this node (due to a lock timeout), but keep the object in the entity manager.
                        if (!subZoneObject.CurrentSubZone.RemoveObjectNode(node))
                        {
                            if (log.IsWarnEnabled)
                                Interlocked.Increment(ref _failedRemove);

                            return;
                        }

                        if (changingZone)
                            currentZone.OnObjectRemovedFromZone();

                        subZoneObject.CurrentSubZone = null;
                    }

                    // Add object to subzone.
                    if (destinationZone != null)
                    {
                        SubZone destinationSubZone = objectChangingSubZone.DestinationSubZone;

                        // Abord if we can't add this node (due to a lock timeout), but keep the object in the entity manager.
                        if (!destinationSubZone.AddObjectNode(node))
                        {
                            if (log.IsWarnEnabled)
                                Interlocked.Increment(ref _failedAdd);

                            return;
                        }

                        subZoneObject.CurrentSubZone = destinationSubZone;

                        if (changingZone)
                            destinationZone.OnObjectAddedToZone();
                    }

                    subZoneObject.ResetSubZoneChange();
                    EntityManager.Remove(objectChangingSubZone);
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, objectChangingSubZone, objectChangingSubZone.SubZoneObject?.Node?.Item);
                }
            });

            if (log.IsWarnEnabled)
            {
                if (_failedRemove > 0)
                {
                    log.Warn($"'{nameof(SubZone)}.{nameof(SubZone.AddObjectNode)}' has failed {_failedRemove} time{(_failedRemove > 1 ? "s" : "")} during this tick.");
                    _failedRemove = 0;
                }

                if (_failedAdd > 0)
                {
                    log.Warn($"'{nameof(SubZone)}.{nameof(SubZone.RemoveObjectNode)}' has failed {_failedAdd} time{(_failedAdd > 1 ? "s" : "")} during this tick.");
                    _failedAdd = 0;
                }
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }

    // Temporary objects to be added to 'EntityManager' and consumed by 'ZoneService', representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone : IManagedEntity
    {
        public LightConcurrentLinkedList<GameObject>.Node Node { get; private set; }
        public SubZoneObject SubZoneObject { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.ObjectChangingSubZone, true);

        private ObjectChangingSubZone(LightConcurrentLinkedList<GameObject>.Node node, SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            Initialize(node, subZoneObject, destinationZone, destinationSubZone);
        }

        public static void Create(LightConcurrentLinkedList<GameObject>.Node node, SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            if (EntityManager.TryReuse(EntityManager.EntityType.ObjectChangingSubZone, out ObjectChangingSubZone objectChangingSubZone))
                objectChangingSubZone.Initialize(node, subZoneObject, destinationZone, destinationSubZone);
            else
            {
                objectChangingSubZone = new(node, subZoneObject, destinationZone, destinationSubZone);
                EntityManager.Add(objectChangingSubZone);
            }
        }

        private void Initialize(LightConcurrentLinkedList<GameObject>.Node node, SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            Node = node;
            SubZoneObject = subZoneObject;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
        }
    }
}
