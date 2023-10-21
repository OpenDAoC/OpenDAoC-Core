using System;
using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the Dying event of GameLivings
/// </summary>
public class HealthChangedEventArgs : EventArgs
{

    /// <summary>
    /// The source of changing
    /// </summary>
    private GameObject m_changesource;

    /// <summary>
    /// The type of changing
    /// </summary>
    private EHealthChangeType m_changetype;


    /// <summary>
    /// The amount of changing
    /// </summary>
    private int m_changeamount;

    /// <summary>
    /// Constructs a new Dying event args
    /// </summary>
    public HealthChangedEventArgs(GameObject source, EHealthChangeType type, int amount)
    {
        m_changesource = source;
        m_changetype = type;
        m_changeamount = amount;
    }

    public GameObject ChangeSource
    {
        get { return m_changesource; }
    }

    public EHealthChangeType ChangeType
    {
        get { return m_changetype; }
    }

    public int ChangeAmount
    {
        get { return m_changeamount; }
    }
}