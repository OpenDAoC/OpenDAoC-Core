using System;
using System.Numerics;

namespace DOL.GS
{
    public class GroundTarget : Point3D
    {
        protected const int UNSET = -1;

        public bool IsValid => m_x > UNSET && m_y > UNSET && m_z > UNSET;
        public bool InView { get; set; }

        public GroundTarget()
        {
            Unset();
        }

        public void Set(Point3D point)
        {
            Set(point.X, point.Y, point.Z);
        }

        public void Set(Vector3 position)
        {
            Set((int) Math.Round(position.X), (int) Math.Round(position.Y), (int) Math.Round(position.Z));
        }

        public void Set(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Unset()
        {
            X = UNSET;
            Y = UNSET;
            Z = UNSET;
            InView = false;
        }
    }
}
