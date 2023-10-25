using Core.Database.Enums;

namespace Core.GS.World;

/// <summary>
/// represents a point in a way path
/// </summary>
public class PathPoint : Point3D
{
	protected short m_maxspeed;
	protected PathPoint m_next = null;
	protected PathPoint m_prev = null;
	protected EPathType m_type;
	protected bool m_flag;
	protected int m_waitTime = 0;

	public PathPoint(PathPoint pp) : this(pp, pp.MaxSpeed,pp.Type) {}

	public PathPoint(Point3D p, short maxspeed, EPathType type) : this(p.X,  p.Y,  p.Z, maxspeed, type) {}

	public PathPoint(int x, int y, int z, short maxspeed, EPathType type) : base(x, y, z)
	{
		m_maxspeed = maxspeed;
		m_type = type;
		m_flag = false;
		m_waitTime = 0;
	}

	/// <summary>
	/// Speed allowed after that waypoint in forward direction
	/// </summary>
	public short MaxSpeed
	{
		get { return m_maxspeed; }
		set { m_maxspeed = value; }
	}

	/// <summary>
	/// next waypoint in path
	/// </summary>
	public PathPoint Next
	{
		get { return m_next; }
		set { m_next = value; }
	}

	/// <summary>
	/// previous waypoint in path
	/// </summary>
	public PathPoint Prev
	{
		get { return m_prev; }
		set { m_prev = value; }
	}

	/// <summary>
	/// flag toggle when go through pathpoint
	/// </summary>
	public bool FiredFlag
	{
		get { return m_flag; }
		set { m_flag = value; }
	}

	/// <summary>
	/// path type
	/// </summary>
	public EPathType Type
	{
		get { return m_type; }
		set { m_type = value; }
	}

	/// <summary>
	/// path type
	/// </summary>
	public int WaitTime
	{
		get { return m_waitTime; }
		set { m_waitTime = value; }
	}
}