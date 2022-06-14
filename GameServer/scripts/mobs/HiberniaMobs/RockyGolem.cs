using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class RockyGolem : GameNPC
	{
		public RockyGolem() : base() { }
        #region Stats
        public override short Constitution { get => base.Constitution; set => base.Constitution = 200; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 180; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "rocky golem";
			Model = 114;
			Level = (byte)Util.Random(40, 42);
			Size = 100;
			MeleeDamageType = eDamageType.Crush;
			Race = 2003;
			Flags = 0;
			
			RockyGolemBrain sbrain = new RockyGolemBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(PrepareTeleport), 1000);			
			}
			return success;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(1000))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_SystemWindow);
			}
		}
		private protected int PrepareTeleport(ECSGameTimer timer)
        {
			BroadcastMessage(String.Format("The {0} says, \"Ahh, I feel the power of the stone... through the living rock I send you!\"",Name));
			foreach (GamePlayer player in GetPlayersInRadius(2000))
			{
				if (player != null)
					player.Out.SendSpellCastAnimation(this, 2803, 3);
			}

			new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(TeleportPlayers), 3000);
			return 0;
        }
		private protected int TeleportPlayers(ECSGameTimer timer)
        {
			//region 224
			//1st teleports
			Point3D port1a = new Point3D(34172, 30368, 14748); //heading 16
			Point3D port1b = new Point3D(34173, 30381, 15310); //heading 1999
			Point3D port1c = new Point3D(34171, 30381, 16366); //heading 2017
			//2nd teleports
			Point3D port2a = new Point3D(30876, 32132, 14492); //heading 3058
			Point3D port2b = new Point3D(30878, 32133, 16410); //heading 3042
			Point3D port2c = new Point3D(30900, 32131, 15654); //heading 997
			//3th teleports
			Point3D port3a = new Point3D(30112, 32645, 13980); //heading 3018
			Point3D port3b = new Point3D(30131, 32643, 14940); //heading 1029
			Point3D port3c = new Point3D(30131, 32643, 13021); //heading 865

			switch(PackageID)
			{
                #region 1st teleports
                case "TreibhPort1a":
				switch (Util.Random(1, 2))
				{
					case 1:
						foreach (GamePlayer player in GetPlayersInRadius(100))
						{
								if (player != null && player.IsAlive && player.IsWithinRadius(port1a, 70))
									player.MoveTo(CurrentRegionID, 34173, 30381, 15310, 1999);
						}
						break;
					case 2:
						foreach (GamePlayer player in GetPlayersInRadius(100))
						{
								if (player != null && player.IsAlive && player.IsWithinRadius(port1a, 70))
									player.MoveTo(CurrentRegionID, 34171, 30381, 16366, 2017);
						}
						break;
				}
				break;
				case "TreibhPort1b":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port1b, 70))
									player.MoveTo(CurrentRegionID, 34172, 30368, 14748, 16);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port1b, 70))
									player.MoveTo(CurrentRegionID, 34171, 30381, 16366, 2017);
							}
							break;
					}
					break;
				case "TreibhPort1c":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port1c, 70))
									player.MoveTo(CurrentRegionID, 34172, 30368, 14748, 16);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port1c, 70))
									player.MoveTo(CurrentRegionID, 34173, 30381, 15310, 1999);
							}
							break;
					}
					break;
				#endregion
				#region 2nd teleports
				case "TreibhPort2a":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port2a, 70))
									player.MoveTo(CurrentRegionID, 30878, 32133, 16410, 3042);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port2a, 70))
									player.MoveTo(CurrentRegionID, 30900, 32131, 15654, 997);
							}
							break;
					}
				break;
				case "TreibhPort2b":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port2b, 70))
									player.MoveTo(CurrentRegionID, 30876, 32132, 14492, 3058);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port2b, 70))
									player.MoveTo(CurrentRegionID, 30900, 32131, 15654, 997);
							}
							break;
					}
					break;
				case "TreibhPort2c":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port2c, 70))
									player.MoveTo(CurrentRegionID, 30876, 32132, 14492, 3058);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port2c, 70))
									player.MoveTo(CurrentRegionID, 30878, 32133, 16393, 3042);
							}
							break;
					}
					break;
				#endregion
				#region 3th teleports
				case "TreibhPort3a":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port3a, 70))
									player.MoveTo(CurrentRegionID, 30131, 32643, 14940, 1029);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port3a, 70))
									player.MoveTo(CurrentRegionID, 30131, 32643, 13021, 865);
							}
							break;
					}
				break;
				case "TreibhPort3b":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port3b, 70))
									player.MoveTo(CurrentRegionID, 30112, 32645, 13980, 3018);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port3b, 70))
									player.MoveTo(CurrentRegionID, 30131, 32643, 13021, 865);
							}
							break;
					}
					break;
				case "TreibhPort3c":
					switch (Util.Random(1, 2))
					{
						case 1:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port3c, 70))
									player.MoveTo(CurrentRegionID, 30112, 32645, 13980, 3018);
							}
							break;
						case 2:
							foreach (GamePlayer player in GetPlayersInRadius(100))
							{
								if (player != null && player.IsAlive && player.IsWithinRadius(port3c, 70))
									player.MoveTo(CurrentRegionID, 30131, 32643, 14940, 1029);
							}
							break;
					}
					break;
					#endregion
			}
            return 0;
        }		
	}
}
namespace DOL.AI.Brain
{
	public class RockyGolemBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RockyGolemBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}