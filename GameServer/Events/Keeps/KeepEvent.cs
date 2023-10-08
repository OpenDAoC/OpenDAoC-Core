using DOL.GS.Keeps;

namespace DOL.Events;

/// <summary>
/// This class holds all possible keep events.
/// Only constants defined here!
/// </summary>
public class KeepEvent : CoreEvent
{
	public KeepEvent(string name) : base(name)
	{			
	}	

	/// <summary>
	/// Tests if this event is valid for the specified object
	/// </summary>
	/// <param name="o">The object for which the event wants to be registered</param>
	/// <returns>true if valid, false if not</returns>
	public override bool IsValidFor(object o)
	{
		return o is AGameKeep;
	}

	/// <summary>
	/// The KeepClaimed event is fired whenever the keep is claimed by a guild	
	/// </summary>
	public static readonly KeepEvent KeepClaimed = new KeepEvent("KeepEvent.KeepClaimed");

	/// <summary>
	/// The KeepTaken event is fired whenever the keep is taken by another realm (lord killed)	
	/// </summary>
	public static readonly KeepEvent KeepTaken = new KeepEvent("KeepEvent.KeepTaken");

	/// <summary>
	/// The TowerRaized event is fired when a tower is raized
	/// </summary>
	public static readonly KeepEvent TowerRaized = new KeepEvent("KeepEvent.TowerRaized");
}