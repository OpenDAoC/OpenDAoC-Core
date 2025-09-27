using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class ZoneService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<SubZoneTransition> _list;

        public static ZoneService Instance { get; }

        static ZoneService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<SubZoneTransition>(ServiceObjectType.SubZoneTransition, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                return;
            }

            GameLoop.ExecuteForEach(_list, lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref EntityCount, _list.Count);
        }

        private static void TickInternal(SubZoneTransition subZoneTransition)
        {
            SubZoneObject subZoneObject = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                subZoneObject = subZoneTransition.SubZoneObject;
                LinkedListNode<GameObject> node = subZoneObject.Node;
                SubZone currentSubZone = subZoneObject.CurrentSubZone;
                Zone currentZone = currentSubZone?.ParentZone;
                SubZone destinationSubZone = subZoneTransition.DestinationSubZone;
                Zone destinationZone = subZoneTransition.DestinationZone;
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
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, subZoneTransition, subZoneTransition.SubZoneObject?.Node?.Value);
            }
            finally
            {
                subZoneObject?.OnSubZoneTransition();
            }
        }
    }

    // Temporary objects to be added to `ServiceObjectStore` and consumed by `ZoneService`, representing an object to be moved from one 'SubZone' to another.
    public class SubZoneTransition : IServiceObject, IPooledObject<SubZoneTransition>
    {
        public SubZoneObject SubZoneObject { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public ServiceObjectId ServiceObjectId { get; set; } =  new(ServiceObjectType.SubZoneTransition);

        public SubZoneTransition() { }

        public SubZoneTransition Init(SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            SubZoneObject = subZoneObject;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
            return this;
        }

        // IPooledObject<T> implementation.
        public long IssuedTimestamp { get; set; }
    }
}
