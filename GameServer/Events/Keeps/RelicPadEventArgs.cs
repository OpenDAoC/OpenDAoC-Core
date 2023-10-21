using System;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the RelicPad event
/// </summary>
public class RelicPadEventArgs : EventArgs
{

    /// <summary>
    /// The player
    /// </summary>
    private GamePlayer m_player;

    /// <summary>
    /// The player
    /// </summary>
    private GameRelic m_relic;

    /// <summary>
    /// Constructs a new KeepEventArgs
    /// </summary>
    public RelicPadEventArgs(GamePlayer player, GameRelic relic)
    {
        this.m_player = player;
        this.m_relic = relic;
    }

    /// <summary>
    /// Gets the player
    /// </summary>
    public GamePlayer Player
    {
        get { return m_player; }
    }

    /// <summary>
    /// Gets the player
    /// </summary>
    public GameRelic Relic
    {
        get { return m_relic; }
    }
}