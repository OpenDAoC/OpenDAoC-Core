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

        private List<SubZoneObject> _list;

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
                _list = ServiceObjectStore.UpdateAndGetAll<SubZoneObject>(ServiceObjectType.SubZoneObject, out lastValidIndex);
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

        private static void TickInternal(SubZoneObject subZoneObject)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                LinkedListNode<GameObject> node = subZoneObject.Node;
                SubZone currentSubZone = subZoneObject.CurrentSubZone;
                Zone currentZone = currentSubZone?.ParentZone;
                SubZone destinationSubZone = subZoneObject.DestinationSubZone;
                Zone destinationZone = subZoneObject.DestinationZone;
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
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, subZoneObject, subZoneObject.Node?.Value);
            }
            finally
            {
                subZoneObject?.OnSubZoneTransition();
            }
        }
    }
}
