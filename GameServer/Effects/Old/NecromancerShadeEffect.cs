namespace Core.GS.Effects.Old;

public class NecromancerShadeEffect : ShadeEffect
{
	/// <summary>
	/// Creates a new shade effect.
	/// </summary>
	public NecromancerShadeEffect()	{ }

	protected int m_timeRemaining = -1;

	/// <summary>
	/// Remaining time of the effect in seconds.
	/// </summary>
	public override int RemainingTime
	{
		get { return (m_timeRemaining < 0) ? 0 : m_timeRemaining * 1000; }
	}

	/// <summary>
	/// Set timer when pet is out of range.
	/// </summary>
	/// <param name="seconds"></param>
	public void SetTetherTimer(int seconds)
	{
		m_timeRemaining = seconds;
	}
}