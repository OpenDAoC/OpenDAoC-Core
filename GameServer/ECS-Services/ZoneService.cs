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
        private static List<ObjectChangingSubZone> _list;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = EntityManager.UpdateAndGetAll<ObjectChangingSubZone>(EntityManager.EntityType.ObjectChangingSubZone, out int lastValidIndex);
            Parallel.For(0, lastValidIndex + 1, TickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            ObjectChangingSubZone objectChangingSubZone = _list[index];

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

                eGameObjectType objectType = node.Value.GameObjectType;

                // Acquire locks on both subzones. We want the removal and addition to happen at the same time from a reader's point of view.
                using SimpleDisposableLock currentSubZoneLock = currentSubZone?[objectType].GetLock();
                using SimpleDisposableLock destinationSubZoneLock = destinationSubZone?[objectType].GetLock();

                if (currentSubZoneLock != null)
                {
                    if (destinationSubZoneLock != null)
                    {
                        currentSubZoneLock.EnterWriteLock();

                        // Spin until we can acquire a lock on the other subzone.
                        while (!destinationSubZoneLock.TryEnterWriteLock())
                        {
                            // Relinquish then reacquire our current lock to prevent dead-locks.
                            currentSubZoneLock.Dispose();
                            Thread.Sleep(0);
                            currentSubZoneLock.EnterWriteLock();
                        }

                        RemoveObjectFromCurrentSubZone();
                        AddObjectToDestinationSubZone();
                    }
                    else
                        RemoveObjectFromCurrentSubZone();
                }
                else
                    AddObjectToDestinationSubZone();

                void AddObjectToDestinationSubZone()
                {
                    destinationSubZone[objectType].AddLast(node);
                    subZoneObject.CurrentSubZone = destinationSubZone;

                    if (changingZone)
                        destinationZone.OnObjectAddedToZone();
                }

                void RemoveObjectFromCurrentSubZone()
                {
                    currentSubZone[objectType].Remove(node);

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
            if (EntityManager.TryReuse(EntityManager.EntityType.ObjectChangingSubZone, out ObjectChangingSubZone objectChangingSubZone, out int index))
            {
                objectChangingSubZone.Initialize(subZoneObject, destinationZone, destinationSubZone);
                objectChangingSubZone.EntityManagerId.Value = index;
            }
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
