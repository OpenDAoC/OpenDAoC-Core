using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;
using static DOL.GS.Zone;

namespace DOL.GS
{
    public static class ZoneService
    {
        private const string SERVICE_NAME = "ZoneService";
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static int _failedAdd;
        private static int _failedRemove;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<ObjectChangingSubZone> list = EntityManager.GetAll<ObjectChangingSubZone>(EntityManager.EntityType.ObjectChangingSubZone);

            // Remove objects from one sub zone, and add them to another.
            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.ObjectChangingSubZone) + 1, i =>
            {
                ObjectChangingSubZone objectChangingSubZone = list[i];

                if (objectChangingSubZone == null)
                    return;

                eGameObjectType objectType = objectChangingSubZone.ObjectType;
                LightConcurrentLinkedList<SubZoneObject>.Node node = objectChangingSubZone.SubZoneObject;
                SubZoneObject subZoneObject = node.Item;
                Zone currentZone = subZoneObject.CurrentSubZone?.ParentZone;
                Zone destinationZone = objectChangingSubZone.DestinationZone;
                bool changingZone = currentZone != destinationZone;

                // Remove object from subzone.
                if (currentZone != null)
                {
                    // Abord if we can't remove this node (due to a lock timeout), but keep the object in the entity manager.
                    if (!subZoneObject.CurrentSubZone.RemoveObjectNode(node, objectType))
                    {
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
                    if (!destinationSubZone.AddObjectNode(node, objectType))
                    {
                        Interlocked.Increment(ref _failedAdd);
                        return;
                    }

                    subZoneObject.CurrentSubZone = destinationSubZone;
                    subZoneObject.IsChangingSubZone = false;

                    if (changingZone)
                        destinationZone.OnObjectAddedToZone();
                }

                EntityManager.Remove(EntityManager.EntityType.ObjectChangingSubZone, i);
            });

            if (_failedRemove > 0)
            {
                log.Error($"'{nameof(SubZone)}.{nameof(SubZone.AddObjectNode)}' has failed {_failedRemove} time{(_failedRemove > 1 ? "s" : "")} during this tick.");
                _failedRemove = 0;
            }

            if (_failedAdd > 0)
            {
                log.Error($"'{nameof(SubZone)}.{nameof(SubZone.RemoveObjectNode)}' has failed {_failedAdd} time{(_failedAdd > 1 ? "s" : "")} during this tick.");
                _failedAdd = 0;
            }

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
