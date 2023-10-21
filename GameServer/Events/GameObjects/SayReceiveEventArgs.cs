using DOL.GS;

namespace DOL.Events;

public class SayReceiveEventArgs : SourceEventArgs
{
	
	private GameLiving target;
	private string text;

	/// <summary>
	/// Constructs a new SayReceiveEventArgs
	/// </summary>
	/// <param name="source">the source that is saying something</param>
	/// <param name="target">the target that listened to the say</param>
	/// <param name="text">the text being said</param>
	public SayReceiveEventArgs(GameLiving source, GameLiving target,  string text) : base(source)
	{			
		this.target = target;
		this.text = text;
	}		
	
	/// <summary>
	/// Gets the GameLiving target who listened to the say
	/// </summary>
	public GameLiving Target
	{
		get { return target; }
	}

	/// <summary>
	/// Gets the text being said
	/// </summary>
	public string Text
	{
		get { return text; }
	}
}