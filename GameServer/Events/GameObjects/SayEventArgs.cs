using System;

namespace Core.GS.Events;

public class SayEventArgs : EventArgs
{
	private string text;

	/// <summary>
	/// Constructs a new SayEventArgs
	/// </summary>
	/// <param name="text">the text being said</param>
	public SayEventArgs(string text)
	{
		this.text = text;
	}

	/// <summary>
	/// Gets the text being said
	/// </summary>
	public string Text
	{
		get { return text; }
	}
}