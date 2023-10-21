using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the Interact event of GameObjects
/// </summary>
public class InteractEventArgs : SourceEventArgs 
{		

	/// <summary>
	/// Constructs a new interact event argument class
	/// </summary>
	/// <param name="source">the player wanting to interact</param>
	public InteractEventArgs(GamePlayer source) : base(source)
	{			
	}

	public new GamePlayer Source
	{
		get {return (GamePlayer) base.Source;}
	}
	
}