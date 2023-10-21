namespace Core.GS.Events;

public class YellEventArgs : SayEventArgs
{
	/// <summary>
	/// Constructs a new YellEventArgs
	/// </summary>
	/// <param name="text">the text being yelled</param>
	public YellEventArgs(string text) : base(text)
	{
	}
}