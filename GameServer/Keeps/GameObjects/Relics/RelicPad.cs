using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.Keeps;

public class RelicPad : GameObject
{
	/// <summary>
	/// The pillar this pad triggers.
	/// </summary>
	private RelicPillar m_relicPillar;

	public override EGameObjectType GameObjectType => EGameObjectType.KEEP_COMPONENT;

	public RelicPad(RelicPillar relicPillar)
	{
		m_relicPillar = relicPillar;
	}

	/// <summary>
	/// Relic pad radius.
	/// </summary>
	static public int Radius
	{
		get { return 200; }
	}

	private int m_playersOnPad = 0;

	/// <summary>
	/// The number of players currently on the pad.
	/// </summary>
	public int PlayersOnPad
	{
		get { return m_playersOnPad; }
		set 
		{
			if (value < 0)
				return;

			m_playersOnPad = value;

			if (m_playersOnPad >= ServerProperty.RELIC_PLAYERS_REQUIRED_ON_PAD &&
				m_relicPillar.State == EDoorState.Closed)
				m_relicPillar.Open();
			else if (m_relicPillar.State == EDoorState.Open && m_playersOnPad <= 0)
				m_relicPillar.Close();
		}
	}

	/// <summary>
	/// Called when a players steps on the pad.
	/// </summary>
	/// <param name="player"></param>
	public void OnPlayerEnter(GamePlayer player)
	{
		PlayersOnPad++;
	}

	/// <summary>
	/// Called when a player steps off the pad.
	/// </summary>
	/// <param name="player"></param>
	public void OnPlayerLeave(GamePlayer player)
	{
		PlayersOnPad--;
	}

	/// <summary>
	/// Class to register players entering or leaving the pad.
	/// </summary>
	public class Surface : Area.Circle
	{
		private RelicPad m_relicPad;

		public Surface(RelicPad relicPad)
			: base("", relicPad.X, relicPad.Y, relicPad.Z, RelicPad.Radius)
		{
			m_relicPad = relicPad;
		}

		public override void OnPlayerEnter(GamePlayer player)
		{
			m_relicPad.OnPlayerEnter(player);
		}

		public override void OnPlayerLeave(GamePlayer player)
		{
			m_relicPad.OnPlayerLeave(player);
		}
	}
}