using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class ZoneService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(ZoneService);
        private static List<ObjectChangingSubZone> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = EntityManager.UpdateAndGetAll<ObjectChangingSubZone>(EntityManager.EntityType.ObjectChangingSubZone, out int lastValidIndex);
            GameLoop.Work(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            ObjectChangingSubZone objectChangingSubZone = _list[index];

            if (objectChangingSubZone?.EntityManagerId.IsSet != true)
                return;

            if (Diagnostics.CheckEntityCounts)
                Interlocked.Increment(ref _entityCount);

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

                if (currentSubZone != null)
                {
                    if (destinationSubZone != null)
                    {
                        destinationSubZone.AddObjectToThisAndRemoveFromOther(node, currentSubZone);

                        if (changingZone)
                        {
                            currentZone.OnObjectRemovedFromZone();
                            destinationZone.OnObjectAddedToZone();
                        }
                    }
                    else
                    {
                        currentSubZone.RemoveObject(node);

                        if (changingZone)
                            currentZone.OnObjectRemovedFromZone();
                    }
                }
                else
                {
                    destinationSubZone.AddObject(node);

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
        }
    }

    // Temporary objects to be added to 'EntityManager' and consumed by 'ZoneService', representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone : IManagedEntity
    {
        public SubZoneObject SubZoneObject { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public EntityManagerId EntityManagerId { get; set; }

        private ObjectChangingSubZone(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            Initialize(subZoneObject, destinationZone, destinationSubZone);
            EntityManagerId = new EntityManagerId(EntityManager.EntityType.ObjectChangingSubZone, CleanUp);
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

        private void CleanUp()
        {
            SubZoneObject = null;
            DestinationZone = null;
            DestinationSubZone = null;
        }
    }
}
