using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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

                try
                {
                    LinkedListNode<GameObject> node = objectChangingSubZone.Node;
                    SubZoneObject subZoneObject = objectChangingSubZone.SubZoneObject;
                    Zone currentZone = subZoneObject.CurrentSubZone?.ParentZone;
                    Zone destinationZone = objectChangingSubZone.DestinationZone;
                    bool changingZone = currentZone != destinationZone;

                    // Remove object from subzone.
                    if (currentZone != null)
                    {
                        // Temporary debug
                        try
                        {
                            subZoneObject.CurrentSubZone.RemoveObjectNode(node);
                        }
                        catch (InvalidOperationException e)
                        {
                            StringBuilder builder = new();
                            builder.AppendLine(e.Message);
                            builder.AppendLine($"Info from objectChangingSubZone.SubZoneObject:");
                            builder.AppendLine($"- object: {node.Value})");
                            builder.AppendLine($"- currentZone: {currentZone.ID}");
                            builder.AppendLine($"- destinationZone: {destinationZone.ID}");
                            builder.AppendLine($"Looking for real node...");
                            LinkedListNode<GameObject> realNode = null;

                            foreach (Region region in WorldMgr.Regions.Values)
                            {
                                foreach (Zone zone in region.Zones)
                                {
                                    realNode = zone.FindObject(node.Value);

                                    if (realNode == null)
                                        continue;
                                    else
                                        goto leave;
                                }
                            }

                            leave:
                            if (realNode == null)
                                builder.AppendLine($"real node not found, object already removed?");
                            else
                            {
                                builder.AppendLine($"realNode equals node? {realNode == node})");
                                builder.AppendLine($"realNode.Value equals node.Value? {realNode.Value == node.Value})");
                                builder.AppendLine($"realNode.Value.SubZoneObject == node.Value.SubZoneObject? {realNode.Value.SubZoneObject == node.Value.SubZoneObject})");
                                builder.AppendLine($"realNode.Value.SubZoneObject.CurrentSubZone == node.Value.SubZoneObject.CurrentSubZone? {realNode.Value.SubZoneObject.CurrentSubZone == node.Value.SubZoneObject.CurrentSubZone})");
                                builder.AppendLine($"realNodeValueSubZoneObjectParentZone: {realNode.Value.SubZoneObject.CurrentSubZone.ParentZone.ID}");
                            }

                            log.Error(builder.ToString());
                        }

                        if (changingZone)
                            currentZone.OnObjectRemovedFromZone();

                        subZoneObject.CurrentSubZone = null;
                    }

                    // Add object to subzone.
                    if (destinationZone != null)
                    {
                        SubZone destinationSubZone = objectChangingSubZone.DestinationSubZone;

                        destinationSubZone.AddObjectNode(node);
                        subZoneObject.CurrentSubZone = destinationSubZone;

                        if (changingZone)
                            destinationZone.OnObjectAddedToZone();
                    }

                    subZoneObject.ResetSubZoneChange();
                    EntityManager.Remove(objectChangingSubZone);
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, objectChangingSubZone, objectChangingSubZone.SubZoneObject?.Node?.Value);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }

    // Temporary objects to be added to 'EntityManager' and consumed by 'ZoneService', representing an object to be moved from one 'SubZone' to another.
    public class ObjectChangingSubZone : IManagedEntity
    {
        public LinkedListNode<GameObject> Node { get; private set; }
        public SubZoneObject SubZoneObject { get; private set; }
        public Zone DestinationZone { get; private set; }
        public SubZone DestinationSubZone { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.ObjectChangingSubZone, true);

        private ObjectChangingSubZone(LinkedListNode<GameObject> node, SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            Initialize(node, subZoneObject, destinationZone, destinationSubZone);
        }

        public static void Create(LinkedListNode<GameObject> node, SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            if (EntityManager.TryReuse(EntityManager.EntityType.ObjectChangingSubZone, out ObjectChangingSubZone objectChangingSubZone))
                objectChangingSubZone.Initialize(node, subZoneObject, destinationZone, destinationSubZone);
            else
            {
                objectChangingSubZone = new(node, subZoneObject, destinationZone, destinationSubZone);
                EntityManager.Add(objectChangingSubZone);
            }
        }

        private void Initialize(LinkedListNode<GameObject> node, SubZoneObject subZoneObject, Zone destinationZone, SubZone destinationSubZone)
        {
            Node = node;
            SubZoneObject = subZoneObject;
            DestinationZone = destinationZone;
            DestinationSubZone = destinationSubZone;
        }
    }
}
