using DOL.Database;

namespace DOL.GS.Movement
{
    public class PathPoint : Point3D
    {
        public short MaxSpeed { get; set; }
        public EPathType Type { get; set; }
        public int WaitTime { get; set; }
        public string TriggerName { get; set; }

        public PathPoint Next { get; set; }
        public PathPoint Prev { get; set; }
        public bool FiredFlag { get; set; } // Used by path reverse, doesn't support more than one NPC on the path.

        public PathPoint(int x, int y, int z, short maxSpeed, EPathType type) : base(x, y, z)
        {
            MaxSpeed = maxSpeed;
            Type = type;
        }

        public PathPoint(int x, int y, int z, short maxSpeed, EPathType type, int waitTime, string triggerName) : this(x, y, z, maxSpeed, type)
        {
            WaitTime = waitTime;
            TriggerName = triggerName;
        }
    }
}
