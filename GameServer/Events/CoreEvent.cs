namespace DOL.Events;

/// <summary>
/// This class defines an abstract event in DOL
/// It needs to be overridden in order to create
/// custom events.
/// </summary>
public abstract class CoreEvent
{
	/// <summary>
	/// The event name
	/// </summary>
	protected string m_EventName;
	
	/// <summary>
	/// Constructs a new event
	/// </summary>
	/// <param name="name">The name of the event</param>
	public CoreEvent(string name)
	{
		m_EventName = name;
	}

	/// <summary>
	/// Gets the name of this event
	/// </summary>
	public string Name
	{
		get { return m_EventName; }
	}

	/// <summary>
	/// Returns the string representation of this event
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return "DOLEvent("+m_EventName+")";
	}

	/// <summary>
	/// Returns true if the event target is valid for this event
	/// </summary>
	/// <param name="o">The object that is hooked</param>
	/// <returns></returns>
	public virtual bool IsValidFor(object o)
	{
		return true;
	}
}