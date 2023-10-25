using Core.GS.Keeps;

namespace Core.GS.Events;

/// <summary>
/// This class holds all possible relic events.
/// Only constants defined here!
/// </summary>
public class RelicPadEvent : CoreEvent
{
    public RelicPadEvent(string name)
        : base(name)
    {
    }

    /// <summary>
    /// Tests if this event is valid for the specified object
    /// </summary>
    /// <param name="o">The object for which the event wants to be registered</param>
    /// <returns>true if valid, false if not</returns>
    public override bool IsValidFor(object o)
    {
        return o is GameRelicPad;
    }

    /// <summary>
    /// The RelicStolen event is fired whenever a relic has been removed from the pad
    /// </summary>
    public static readonly RelicPadEvent RelicStolen = new RelicPadEvent("RelicEvent.RelicStolen");

    /// <summary>
    /// The RelicMounted event is fired whenever a relic is stored to the pad	
    /// </summary>
    public static readonly RelicPadEvent RelicMounted = new RelicPadEvent("RelicEvent.RelicMounted");
}