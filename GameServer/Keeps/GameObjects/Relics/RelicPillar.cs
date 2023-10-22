using Core.GS.ECS;
using Core.GS.World;

namespace Core.GS.Keeps;

public class RelicPillar : GameDoorBase
{
	/// <summary>
	/// Creates a new relic pillar.
	/// </summary>
	public RelicPillar() : base()
	{
		Realm = 0;
		Close();
	}

	/// <summary>
	/// Object used for thread synchronization.
	/// </summary>
	private object m_syncPillar = new object();

	private int m_pillarID;

	/// <summary>
	/// ID for this pillar.
	/// </summary>
	public override int DoorID
	{
		get { return m_pillarID; }
		set { m_pillarID = value; }
	}

	/// <summary>
	/// Pillars behave like regular doors.
	/// </summary>
	public override uint Flag
	{
		get { return 0; }
		set { }
	}

	private EDoorState m_pillarState;

	/// <summary>
	/// State of this pillar (up == closed, down == open).
	/// </summary>
	public override EDoorState State
	{
		get { return m_pillarState; }
		set
		{
			if (m_pillarState != value)
			{
				lock (m_syncPillar)
				{
					m_pillarState = value;

					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						ClientService.UpdateObjectForPlayer(player, this);
				}
			}
		}
	}

	/// <summary>
	/// Make the pillar start moving down.
	/// </summary>
	public override void Open(GameLiving opener = null)
	{
		State = EDoorState.Open;
	}

	/// <summary>
	/// Reset pillar.
	/// </summary>
	public override void Close(GameLiving closer = null)
	{
		State = EDoorState.Closed;
	}

	/// <summary>
	/// NPCs cannot make pillars move.
	/// </summary>
	/// <param name="npc"></param>
	/// <param name="open"></param>
	public override void NPCManipulateDoorRequest(GameNpc npc, bool open)
	{
	}
}