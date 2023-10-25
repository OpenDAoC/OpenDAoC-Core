using System;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the GainedRealmPoints event of GameLivings
/// </summary>
public class GainedRealmPointsEventArgs : EventArgs
{
	private long m_realmPoints;

	/// <summary>
	/// Constructs new GainedRealmPointsEventArgs
	/// </summary>
	/// <param name="realmPoints">the amount of realm points gained</param>
	public GainedRealmPointsEventArgs(long realmPoints)
	{
		m_realmPoints = realmPoints;
	}

	/// <summary>
	/// Gets the amount of realm points gained
	/// </summary>
	public long RealmPoints
	{
		get { return m_realmPoints; }
	}
}
public class GainedBountyPointsEventArgs : EventArgs
{
    private long m_realmPoints;

    /// <summary>
    /// Constructs new GainedRealmPointsEventArgs
    /// </summary>
    /// <param name="realmPoints">the amount of realm points gained</param>
    public GainedBountyPointsEventArgs(long realmPoints)
    {
        m_realmPoints = realmPoints;
    }

    /// <summary>
    /// Gets the amount of realm points gained
    /// </summary>
    public long BountyPoints
    {
        get { return m_realmPoints; }
    }
}