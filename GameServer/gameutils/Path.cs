namespace DOL.GS.Movement
{
    public class Path
    {
        public static Path None { get; } = new NullPath();

        public bool IsNone => this == None;
        public virtual string Id { get; set; }
        public virtual PathPoint Point { get; set; }
        public virtual bool IsReversing { get; set; }

        private Path() { }

        private Path(string pathId)
        {
            Id = pathId;
        }

        private Path(PathPoint point)
        {
            Point = point;
        }

        public static Path WithPathId(Path current, string id)
        {
            if (current.IsNone)
                return string.IsNullOrEmpty(id) ? None : new(id);

            if (string.IsNullOrEmpty(id))
                return None;

            current.Id = id;
            return current;
        }

        public static Path WithPathPoint(Path current, PathPoint point)
        {
            if (current.IsNone)
                return point == null ? None : new(point);

            if (point == null)
                return None;

            current.Point = point;
            return current;
        }

        private class NullPath : Path
        {
            public override string Id { get => string.Empty; set { } }
            public override PathPoint Point { get => null; set { } }
            public override bool IsReversing { get => false; set { } }
        }
    }
}
