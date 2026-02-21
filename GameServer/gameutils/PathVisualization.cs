using System.Collections.Generic;

namespace DOL.GS.Movement
{
    public class PathVisualization
    {
        private const int AUTOMATIC_CLEANUP_INTERVAL = 120000;

        private List<GameNPC> _markers;
        private ECSGameTimer _cleanupTimer;

        public PathVisualization() { }

        public void Visualize(RingQueue<WrappedPathfindingNode> nodes, Region region)
        {
            if (nodes == null)
                return;

            Prepare();

            foreach (WrappedPathfindingNode node in nodes)
                _markers.Add(CreateMarker((int) node.Position.X, (int) node.Position.Y, (int) node.Position.Z, region, GetModel(node.Flags), 30));

            StartCleanupTimer(AUTOMATIC_CLEANUP_INTERVAL);

            static EMarkerModel GetModel(EDtPolyFlags flags)
            {
                if ((flags & EDtPolyFlags.Swim) != 0)
                    return EMarkerModel.Blue;

                if ((flags & EDtPolyFlags.AnyDoor) != 0)
                    return EMarkerModel.Red;

                if ((flags & EDtPolyFlags.Walk) != 0)
                    return EMarkerModel.Green;

                return EMarkerModel.Yellow;
            }
        }

        public void Visualize(PathPoint pathPoint, Region region)
        {
            if (pathPoint == null)
                return;

            Prepare();

            do
            {
                _markers.Add(CreateMarker(pathPoint.X, pathPoint.Y, pathPoint.Z, region, EMarkerModel.Yellow, 40));
                pathPoint = pathPoint.Next;
            } while (pathPoint != null);

            StartCleanupTimer(AUTOMATIC_CLEANUP_INTERVAL);
        }

        public void StartCleanupTimer(int interval)
        {
            if (_cleanupTimer != null)
            {
                _cleanupTimer.Start(interval);
                return;
            }

            _cleanupTimer = new ECSGameTimer(null, (timer) =>
            {
                CleanUp();
                return 0;
            }, interval);
        }

        public void CleanUp()
        {
            if (_markers == null)
                return;

            foreach (GameNPC marker in _markers)
                marker.RemoveFromWorld();

            _markers.Clear();
        }

        private static GameNPC CreateMarker(int x, int y, int z, Region region, EMarkerModel model, byte size)
        {
            GameNPC npc = new()
            {
                X = x,
                Y = y,
                Z = z,
                Name = string.Empty,
                CurrentRegion = region,
                Level = 1,
                Model = (ushort) model,
                Size = size
            };

            npc.Flags |= GameNPC.eFlags.PEACE | GameNPC.eFlags.FLYING;
            npc.AddToWorld();
            return npc;
        }

        private void Prepare()
        {
            if (_markers == null)
                _markers = new();
            else
                CleanUp();
        }

        private enum EMarkerModel : ushort
        {
            Red = 408,
            Yellow = 409,
            Blue = 410,
            Green = 411
        }
    }
}
