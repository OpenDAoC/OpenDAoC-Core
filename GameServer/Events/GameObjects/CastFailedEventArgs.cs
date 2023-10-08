using DOL.GS.Spells;

namespace DOL.Events;

/// <summary>
/// Arguments for a cast failed event, stating the reason
/// why a particular spell cast failed.
/// </summary>
public class CastFailedEventArgs : CastingEventArgs
{
	public enum Reasons
	{
		TargetTooFarAway,
		TargetNotInView,
		AlreadyCasting,
		CrowdControlled,
		NotEnoughPower,
	};
			
	/// <summary>
	/// Constructs arguments for a cast failed event.
	/// </summary>
	public CastFailedEventArgs(ISpellHandler handler, Reasons reason) 
		: base(handler)
	{
		this.m_reason = reason;
	}

	private Reasons m_reason;

	/// <summary>
	/// The reason why the spell cast failed.
	/// </summary>
	public Reasons Reason
	{
		get { return m_reason; }
	}
}