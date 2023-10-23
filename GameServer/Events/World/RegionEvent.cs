using Core.GS.World;

namespace Core.GS.Events;

/// <summary>
/// This class holds all possible region events.
/// Only constants defined here!
/// </summary>
public class RegionEvent : CoreEvent
{
	public RegionEvent(string name) : base(name)
	{			
	}	

	/// <summary>
	/// Tests if this event is valid for the specified object
	/// </summary>
	/// <param name="o">The object for which the event wants to be registered</param>
	/// <returns>true if valid, false if not</returns>
	public override bool IsValidFor(object o)
	{
		return o is Region;
	}

	/// <summary>
	/// The PlayerEnter event is fired whenever the player enters an region		
	/// </summary>
	public static readonly RegionEvent PlayerEnter = new RegionEvent("RegionEvent.PlayerEnter");

	/// <summary>
	/// The PlayerLeave event is fired whenever the player leaves an region		
	/// </summary>
	public static readonly RegionEvent PlayerLeave = new RegionEvent("RegionEvent.PlayerLeave");
	
	/// <summary>
	/// The RegionLoaded event is fired whenever the region is loaded	
	/// </summary>
	public static readonly RegionEvent RegionStart = new RegionEvent("RegionEvent.RegionStart");

	/// <summary>
	/// The RegionUnLoaded event is fired whenever the region is unloaded	
	/// </summary>
	public static readonly RegionEvent RegionStop = new RegionEvent("RegionEvent.RegionStop");
}