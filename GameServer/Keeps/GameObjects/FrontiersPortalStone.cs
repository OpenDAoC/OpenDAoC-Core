using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.Keeps;

public class FrontiersPortalStone : GameStaticItem, IKeepItem
{
	private string m_templateID = string.Empty;
	public string TemplateID
	{
		get { return m_templateID; }
	}

	private GameKeepComponent m_component;
	public GameKeepComponent Component
	{
		get { return m_component; }
		set { m_component = value; }
	}

	private DbKeepPosition m_position;
	public DbKeepPosition Position
	{
		get { return m_position; }
		set { m_position = value; }
	}

	public void LoadFromPosition(DbKeepPosition pos, GameKeepComponent component)
	{
		if (component.Keep.DBKeep.BaseLevel < 50)
			return;
		m_component = component;
		GuardPositionMgr.LoadKeepItemPosition(pos, this);
		this.m_component.Keep.TeleportStone = this;
		this.AddToWorld();
	}

	public void MoveToPosition(DbKeepPosition position)
	{ }

	public override ERealm Realm
	{
		get
		{
			if (m_component != null)
				return m_component.Keep.Realm;
			if (CurrentRegion.ID == 163)
				return CurrentZone.Realm;
			return base.Realm;
		}
	}

	public override string Name
	{
		get { return "Frontiers Portal Stone"; }
	}

	public override ushort Model
	{
		get { return 2603; }
	}

	public override bool Interact(GamePlayer player)
	{
		if (!base.Interact(player))
			return false;

		//For players in frontiers only
		if (GameServer.KeepManager.FrontierRegionsList.Contains(player.CurrentRegionID))
		{
			if (player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
			{
				if (player.Realm != this.Realm)
					return false;

				if (Component != null && Component.Keep is GameKeep)
				{
					if ((Component.Keep as GameKeep).OwnsAllTowers == false || (Component.Keep as GameKeep).InCombat)
						return false;
				}

				if (GameRelic.IsPlayerCarryingRelic(player))
					return false;
			}

			// open up the warmap window

			EDialogCode code = EDialogCode.WarmapWindowAlbion;
			switch (player.Realm)
			{
				case ERealm.Albion: code = EDialogCode.WarmapWindowAlbion; break;
				case ERealm.Midgard: code = EDialogCode.WarmapWindowMidgard; break;
				case ERealm.Hibernia: code = EDialogCode.WarmapWindowHibernia; break;
			}

			player.Out.SendDialogBox(code, 0, 0, 0, 0, EDialogType.Warmap, false, "");
		}

		//if no component assigned, teleport to the border keep
		if (Component == null && GameServer.KeepManager.FrontierRegionsList.Contains(player.CurrentRegionID) == false)
		{
			GameServer.KeepManager.ExitBattleground(player);
		}

		return true;
	}

	public void GetTeleportLocation(out int x, out int y)
	{
		ushort originalHeading = _heading;
		_heading = (ushort)Util.Random((_heading - 500), (_heading + 500));
		int distance = Util.Random(50, 150);
        Point2D portloc = this.GetPointFromHeading( this.Heading, distance );
        x = portloc.X;
        y = portloc.Y;
		_heading = originalHeading;
	}

	public class TeleporterEffect : GameNpc
	{
		public TeleporterEffect()
			: base()
		{
			m_name = "teleport spell effect";
			m_flags = ENpcFlags.PEACE | ENpcFlags.DONTSHOWNAME;
			m_size = 255;
			m_model = 0x783;
			MaxSpeedBase = 0;
		}
	}

	#region Teleporter Effect

	protected TeleporterEffect sfx;

	public override bool AddToWorld()
	{
		if (!base.AddToWorld()) return false;
		TeleporterEffect mob = new TeleporterEffect();
		mob.CurrentRegion = this.CurrentRegion;
		mob.X = this.X;
		mob.Y = this.Y;
		mob.Z = this.Z;
		mob.Heading = this.Heading;
		mob.Health = mob.MaxHealth;
		mob.MaxSpeedBase = 0;
		if (mob.AddToWorld())
			sfx = mob;
		return true;
	}

	public override bool RemoveFromWorld()
	{
		if (!base.RemoveFromWorld()) return false;
		if (sfx != null)
			sfx.Delete();
		return true;
	}
	#endregion
}