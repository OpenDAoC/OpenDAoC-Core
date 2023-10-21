using DOL.GS;

namespace DOL.Events;

/// <summary>
/// Holds the arguments for the WhisperReceive event of GameLivings
/// </summary>
public class WhisperReceiveEventArgs : SayReceiveEventArgs
{
	/// <summary>
	/// Constructs a new WhsiperReceiveEventArgs
	/// </summary>
	/// <param name="source">the source of the whisper</param>
	/// <param name="target">the target of the whisper</param>
	/// <param name="text">the text being whispered</param>
	public WhisperReceiveEventArgs(GameLiving source, GameLiving target, string text) : base(source, target, text)
	{
	}
}