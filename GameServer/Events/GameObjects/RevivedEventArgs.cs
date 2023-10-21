using System;
using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the Revived event of GamePlayer
/// </summary>
public class RevivedEventArgs : EventArgs
{

    /// <summary>
    /// The source of revive (rezzer) or null
    /// </summary>
    private GameObject m_source = null;

    /// <summary>
    /// The spell if one used, else null
    /// </summary>
    private Spell m_spell = null;

    /// <summary>
    /// Constructs a new Revived event args
    /// </summary>
    public RevivedEventArgs(GameObject source, Spell spell)
    {
        m_source = source;
        m_spell = spell;
    }

    public GameObject Source
    {
        get { return m_source; }
    }

    public Spell Spell
    {
        get { return m_spell; }
    }
}