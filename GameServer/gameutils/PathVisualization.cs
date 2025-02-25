using System.Collections.Generic;

namespace DOL.GS.Movement
{
    public class PathVisualization
    {
        private const int AUTOMATIC_CLEANUP_INTERVAL = 120000;

        private List<GameNPC> _markers;
        private ECSGameTimer _cleanupTimer;

        public PathVisualization() { }

        public void Visualize(Queue<WrappedPathPoint> pathPoints, Region region)
        {
            if (pathPoints == null)
                return;

            Prepare();

            foreach (WrappedPathPoint point in pathPoints)
                _markers.Add(CreateMarker((int) point.Position.X, (int) point.Position.Y, (int) point.Position.Z, region, GetModel(point.Flags), 30));

            StartCleanupTimer(AUTOMATIC_CLEANUP_INTERVAL);

            static EMarkerModel GetModel(EDtPolyFlags flags)
            {
                if ((flags & EDtPolyFlags.SWIM) != 0)
                    return EMarkerModel.Blue;

                if ((flags & EDtPolyFlags.DOOR) != 0)
                    return EMarkerModel.Red;

                if ((flags & EDtPolyFlags.WALK) != 0)
                    return EMarkerModel.Yellow;

                return EMarkerModel.Green;
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
