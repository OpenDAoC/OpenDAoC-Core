using Core.GS.Enums;
using Core.GS.Spells;

namespace Core.GS.Events;

/// <summary>
/// Arguments for a cast failed event, stating the reason
/// why a particular spell cast failed.
/// </summary>
public class CastFailedEventArgs : CastingEventArgs
{
	/// <summary>
	/// Constructs arguments for a cast failed event.
	/// </summary>
	public CastFailedEventArgs(ISpellHandler handler, ECastFailedReasons reason) 
		: base(handler)
	{
		this.m_reason = reason;
	}

	private ECastFailedReasons m_reason;

	/// <summary>
	/// The reason why the spell cast failed.
	/// </summary>
	public ECastFailedReasons Reason
	{
		get { return m_reason; }
	}
}