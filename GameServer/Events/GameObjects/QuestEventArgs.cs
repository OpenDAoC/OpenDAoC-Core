using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the Quest event of GamePlayer
/// </summary>
public class QuestEventArgs : SourceEventArgs
{
	private ushort questid;
    private GamePlayer player;

	/// <summary>
	/// Constrcuts a new QuesteventArgument
	/// </summary>
	/// <param name="source">Inviting NPC</param>
	/// <param name="player">Player associated with quest</param>
	/// <param name="questid">id of quest</param>
	public QuestEventArgs(GameLiving source,GamePlayer player,ushort questid) : base (source)
	{
		this.questid = questid;
        this.player = player;
	}

	/// <summary>
	/// Gets the Id of quest
	/// </summary>
	public ushort QuestID
	{
		get { return questid; }
	}

    public GamePlayer Player
    {
        get { return player; }
    }
}