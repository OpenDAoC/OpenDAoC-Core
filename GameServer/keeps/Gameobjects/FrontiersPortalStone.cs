using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Keeps
{
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
			// 1. Performance-Check: Wenn das Level nicht reicht, direkt raus.
			if (component.Keep.DBKeep.BaseLevel < 50)
			{
				// Falls ein Stein existiert, der jetzt nicht mehr da sein darf: Weg damit.
				if (component.Keep.TeleportStone != null)
				{
					component.Keep.TeleportStone.Delete();
					component.Keep.TeleportStone = null;
				}
				return;
			}

			// 2. Dubletten-Check: Wenn die Referenz bereits auf ein aktives Objekt zeigt,
			// erstelle KEIN neues Objekt. Das spart CPU-Zyklen und RAM.
			if (component.Keep.TeleportStone != null && 
				component.Keep.TeleportStone.ObjectState == GameObject.eObjectState.Active)
			{
				return; 
			}

			// 3. Falls das Objekt im Speicher als gelöscht markiert ist, aber die Referenz noch da ist:
			// Altes "Wrack" aufräumen.
			if (component.Keep.TeleportStone != null)
			{
				component.Keep.TeleportStone = null;
			}

			// 4. Erst jetzt (wenn wirklich nötig) die teure Logik ausführen:
			m_component = component;
			PositionMgr.LoadKeepItemPosition(pos, this);
			
			this.m_component.Keep.TeleportStone = this;
			this.AddToWorld();
		}

		public void MoveToPosition(DbKeepPosition position)
		{ }

		public override eRealm Realm
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
				if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
				{
					if (player.Realm != this.Realm)
						return false;

					if (Component != null && Component.Keep is GameKeep)
					{
						if ((Component.Keep as GameKeep).OwnsAllTowers == false)
							return false;
					}

					if (GameRelic.IsPlayerCarryingRelic(player))
						return false;
				}

				// open up the warmap window
				eDialogCode code = eDialogCode.WarmapWindowAlbion;
				switch (player.Realm)
				{
					case eRealm.Albion: code = eDialogCode.WarmapWindowAlbion; break;
					case eRealm.Midgard: code = eDialogCode.WarmapWindowMidgard; break;
					case eRealm.Hibernia: code = eDialogCode.WarmapWindowHibernia; break;
				}

				player.Out.SendDialogBox(code, 0, 0, 0, 0, eDialogType.Warmap, false, string.Empty);
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
			ushort originalHeading = Heading;
			Heading = (ushort)Util.Random((Heading - 500), (Heading + 500));
			int distance = Util.Random(50, 150);
            Point2D portloc = this.GetPointFromHeading( this.Heading, distance );
            x = portloc.X;
            y = portloc.Y;
			Heading = originalHeading;
		}

		public class TeleporterEffect : GameNPC
		{
			public TeleporterEffect()
				: base()
			{
				m_name = "teleport spell effect";
				m_flags = eFlags.PEACE | eFlags.DONTSHOWNAME;
				m_size = 255;
				m_model = 0x783;
				MaxSpeedBase = 0;
			}
		}
		

		#region Teleporter Effect

		// protected TeleporterEffect sfx; // Remove Effect from Teleport stone

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
			//if (mob.AddToWorld()) // Remove Effect from Teleport stone
			//	sfx = mob; // Remove Effect from Teleport stone
			return true;
		}

		public override bool RemoveFromWorld()
		{
			if (!base.RemoveFromWorld()) return false;
			return true;
		 }
		#endregion
	}
}