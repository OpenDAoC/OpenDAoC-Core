using DOL.Database;

namespace DOL.GS.Movement
{
    public class PathPoint : Point3D
    {
        public short MaxSpeed { get; set; }
        public PathPoint Next { get; set; }
        public PathPoint Prev { get; set; }
        public EPathType Type { get; set; }
        public bool FiredFlag { get; set; }
        public int WaitTime { get; set; }

        public PathPoint(PathPoint pathPoint) : this(pathPoint, pathPoint.MaxSpeed,pathPoint.Type) { }

        public PathPoint(Point3D point, short maxSpeed, EPathType type) : this(point.X,  point.Y,  point.Z, maxSpeed, type) { }

        public PathPoint(int x, int y, int z, short maxSpeed, EPathType type) : base(x, y, z)
        {
            MaxSpeed = maxSpeed;
            Type = type;
        }
    }
}
