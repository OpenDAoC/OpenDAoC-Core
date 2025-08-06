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

            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<ObjectChangingSubZone>(ServiceObjectType.ObjectChangingSubZone, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                Diagnostics.StopPerfCounter(SERVICE_NAME);
                return;
            }

            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            ObjectChangingSubZone objectChangingSubZone = null;
            SubZoneObject subZoneObject = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                objectChangingSubZone = _list[index];
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
                if (objectChangingSubZone != null)
                    ServiceObjectStore.Remove(objectChangingSubZone);

                subZoneObject?.ResetSubZoneChange();
            }
        }
    }

    // Temporary objects to be added to `ServiceObjectStore` and consumed by `ZoneService`, representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone : IServiceObject
    {
        public SubZoneObject SubZoneObject { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public ServiceObjectId ServiceObjectId { get; set; }

        private ObjectChangingSubZone(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            Initialize(subZoneObject, destinationZone, destinationSubZone);
            ServiceObjectId = new ServiceObjectId(ServiceObjectType.ObjectChangingSubZone);
        }

        public static void Create(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            ObjectChangingSubZone objectChangingSubZone = new(subZoneObject, destinationZone, destinationSubZone);
            ServiceObjectStore.Add(objectChangingSubZone);
        }

        private void Initialize(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            SubZoneObject = subZoneObject;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
        }
    }
}
