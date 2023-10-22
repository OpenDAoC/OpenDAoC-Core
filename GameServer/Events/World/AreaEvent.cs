using Core.Events;
using Core.GS.World;

namespace Core.GS.Events;

/// <summary>
/// This class holds all possible Area events.
/// Only constants defined here!
/// </summary>
public class AreaEvent : CoreEvent
{
	public AreaEvent(string name) : base(name)
	{			
	}	

	/// <summary>
	/// Tests if this event is valid for the specified object
	/// </summary>
	/// <param name="o">The object for which the event wants to be registered</param>
	/// <returns>true if valid, false if not</returns>
	public override bool IsValidFor(object o)
	{
		return o is IArea;
	}

	/// <summary>
	/// The PlayerEnter event is fired whenever the player enters an area		
	/// </summary>
	public static readonly AreaEvent PlayerEnter = new AreaEvent("AreaEvent.PlayerEnter");

	/// <summary>
	/// The PlayerLeave event is fired whenever the player leaves an area		
	/// </summary>
	public static readonly AreaEvent PlayerLeave = new AreaEvent("AreaEvent.PlayerLeave");
}