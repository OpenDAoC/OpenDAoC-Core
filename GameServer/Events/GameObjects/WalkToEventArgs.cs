using System;

namespace Core.GS.Events;

public class WalkToEventArgs : EventArgs
{
    public WalkToEventArgs(IPoint3D target, int speed)
    {
        Target = target;
        Speed = speed;
    }

    /// <summary>
    /// The spot to walk to.
    /// </summary>
    public IPoint3D Target { get; private set; }

	/// <summary>
	/// The speed to walk at.
	/// </summary>
    public int Speed { get; private set; }
}