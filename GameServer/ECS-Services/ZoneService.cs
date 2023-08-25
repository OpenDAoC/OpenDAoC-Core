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

                    if (currentSubZone == destinationSubZone)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"Cancelled a subzone change because both subzones are the same ({nameof(currentZone)}: {currentZone?.ID}) ({nameof(destinationZone)}: {destinationZone?.ID}) (Object: {node.Value})");

                        return;
                    }

                    // Acquire locks on both subzones. We want the removal and addition to happen at the same time from a reader's point of view.
                    ConcurrentLinkedList<GameObject>.Writer currentSubZoneWriter = currentSubZone?.GetObjectWriter(node);
                    ConcurrentLinkedList<GameObject>.Writer destinationSubZoneWriter = destinationSubZone?.GetObjectWriter(node);

                    if (currentSubZoneWriter != null)
                    {
                        currentSubZoneWriter.Lock();

                        if (destinationSubZoneWriter != null)
                        {
                            // Spin until we can acquire a lock on the other subzone.
                            while (!destinationSubZoneWriter.TryLock())
                            {
                                // Relinquish then reacquire our current lock to prevent dead-locks.
                                currentSubZoneWriter.Dispose();
                                Thread.Sleep(0);
                                currentSubZoneWriter.Lock();
                            }

                            RemoveObjectFromCurrentSubZone();
                            AddObjectToDestinationSubZone();
                            currentSubZoneWriter.Dispose();
                            destinationSubZoneWriter.Dispose();
                        }
                        else
                        {
                            RemoveObjectFromCurrentSubZone();
                            currentSubZoneWriter.Dispose();
                        }
                    }
                    else
                    {
                        destinationSubZoneWriter.Lock();
                        AddObjectToDestinationSubZone();
                        destinationSubZoneWriter.Dispose();
                    }

                    void AddObjectToDestinationSubZone()
                    {
                        destinationSubZone.AddObjectNode(node);
                        subZoneObject.CurrentSubZone = destinationSubZone;

                        if (changingZone)
                            destinationZone.OnObjectAddedToZone();
                    }

                    void RemoveObjectFromCurrentSubZone()
                    {
                        currentSubZone.RemoveObjectNode(node);

                        if (changingZone)
                            currentZone.OnObjectRemovedFromZone();

                        subZoneObject.CurrentSubZone = null;
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
