using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class ZoneService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(ZoneService);

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

                EntityManager.Remove(objectChangingSubZone);
                SubZoneObject subZoneObject = null;

                try
                {
                    subZoneObject = objectChangingSubZone.SubZoneObject;
                    LinkedListNode<GameObject> node = subZoneObject.Node;
                    SubZone currentSubZone = subZoneObject.CurrentSubZone;
                    Zone currentZone = currentSubZone?.ParentZone;
                    SubZone destinationSubZone = objectChangingSubZone.DestinationSubZone;
                    Zone destinationZone = objectChangingSubZone.DestinationZone;
                    bool changingZone = currentZone != destinationZone;

                    // Acquire locks on both subzones. We want the removal and addition to happen at the same time from a reader's point of view.
                    // Abort if we can't get a lock on the destination subzone.
                    using ConcurrentLinkedList<GameObject>.Writer currentSubZoneWriter = currentSubZone?.GetObjectWriter(node);
                    bool destinationZoneWriterSuccess = false;
                    using ConcurrentLinkedList<GameObject>.Writer destinationSubZoneWriter = destinationSubZone?.TryGetObjectWriter(node, out destinationZoneWriterSuccess);

                    // If we couldn't acquire a lock, try again later.
                    if (!destinationZoneWriterSuccess)
                        return;

                    // Remove object from current subzone.
                    if (currentSubZoneWriter != null)
                    {
                        currentSubZone.RemoveObjectNode(node);

                        if (changingZone)
                            currentZone.OnObjectRemovedFromZone();

                        subZoneObject.CurrentSubZone = null;
                    }

                    // Add object to destination subzone.
                    if (destinationSubZoneWriter != null)
                    {
                        destinationSubZone.AddObjectNode(node);
                        subZoneObject.CurrentSubZone = destinationSubZone;

                        if (changingZone)
                            destinationZone.OnObjectAddedToZone();
                    }
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, objectChangingSubZone, objectChangingSubZone.SubZoneObject?.Node?.Value);
                }
                finally
                {
                    subZoneObject?.ResetSubZoneChange();
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }

    // Temporary objects to be added to 'EntityManager' and consumed by 'ZoneService', representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone : IManagedEntity
    {
        public SubZoneObject SubZoneObject { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.ObjectChangingSubZone, true);

        private ObjectChangingSubZone(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            Initialize(subZoneObject, destinationZone, destinationSubZone);
        }

        public static void Create(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            if (EntityManager.TryReuse(EntityManager.EntityType.ObjectChangingSubZone, out ObjectChangingSubZone objectChangingSubZone))
                objectChangingSubZone.Initialize(subZoneObject, destinationZone, destinationSubZone);
            else
            {
                objectChangingSubZone = new(subZoneObject, destinationZone, destinationSubZone);
                EntityManager.Add(objectChangingSubZone);
            }
        }

        private void Initialize(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            SubZoneObject = subZoneObject;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
        }
    }
}
