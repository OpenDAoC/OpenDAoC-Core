using System;
using System.Collections;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS.Scripts
{
    public class Gnat : GameEpicBoss
    {
		public Gnat() : base() { }
		public static GameNPC SI_Gnat = new GameNPC();
		public override bool AddToWorld()
		{
			Model = 917;
			Name = "Gnat";
			Size = 50;
			Level = 35;
			Gender = eGender.Neutral;

			BodyType = 6; // Humanoid
			MaxDistance = 1500;
			TetherRange = 2000;
			RoamingRange = 400;
			GnatBrain sBrain = new GnatBrain();
			SetOwnBrain(sBrain);
			sBrain.AggroLevel = 100;
			sBrain.AggroRange = 500;
			GnatBrain.spawnants = true;
			base.AddToWorld();
			return true;
		}
		
		public override void Die(GameObject killer)
		{
			// debug
			log.Debug($"{Name} killed by {killer.Name}");

			GamePlayer playerKiller = killer as GamePlayer;

			if (playerKiller?.Group != null)
			{
				foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
				{
					AtlasROGManager.GenerateOrbAmount(groupPlayer,OrbsReward);
				}
			}

			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class GnatBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GnatBrain() : base() { }

		public static bool spawnants = true;

		public override void Think()
		{
			if (Body.InCombat == true && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if(Body.HealthPercent < 95 && spawnants==true)
                    {
						Spawn();//spawn adds here
						spawnants = false;//we check here to avoid spawning adds multiple times
						foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
						{
							player.Out.SendMessage("Lets loose a high pitch whistle.", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
						}
					}

					foreach (GameNPC mob_c in Body.GetNPCsInRadius(2000, false))
					{
						if (mob_c != null)
						{
							if ((mob_c.Name.ToLower() == "fiery ant") && mob_c.IsAlive && mob_c.IsAvailable)
							{
								if (mob_c.Brain is GnatAntsBrain && mob_c.RespawnInterval == -1)
								{
									AddAggroListTo(mob_c.Brain as StandardMobBrain);//add ants to boss agrro brain
								}
							}
						}
					}
				}
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
			{
				spawnants = true;//reset so he can actually spawn adds if player decide to leave combat
			}
			base.Think();
		}
		public void Spawn() // We define here adds
		{
			for (int i = 0; i < 7; i++)//Spawn 8 ants
			{
				GnatAnts Add = new GnatAnts();
				Add.X = Body.X + Util.Random(50, 80);
				Add.Y = Body.Y + Util.Random(50, 80);
				Add.Z = Body.Z;
				Add.CurrentRegion = Body.CurrentRegion;
				Add.IsWorthReward=false;
				Add.Heading = Body.Heading;
				Add.AddToWorld();
			}
		}
		
		
		
	}
}

namespace DOL.GS
{
	public class GnatAnts : GameNPC
	{
		public GnatAnts() : base() { }
		public static GameNPC SI_Gnatants = new GameNPC();
		public override int MaxHealth
		{
			get { return 450 * Constitution / 100; }
		}

		public override bool AddToWorld()
		{
			Model = 115;
			Name = "fiery ant";
			MeleeDamageType = eDamageType.Thrust;
			RoamingRange = 350;
			RespawnInterval = -1;
			MaxDistance = 1500;
			TetherRange = 2000;
			IsWorthReward = false;//worth no reward
			Size = (byte)Util.Random(8, 12);
			Level = (byte)Util.Random(30, 34);
			Realm = eRealm.None;
			GnatAntsBrain adds = new GnatAntsBrain();
			LoadedFromScript = true;
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}
		public override void DropLoot(GameObject killer)//no loot
		{
		}
		public override void Die(GameObject killer)
		{
			base.Die(null);//null to not gain experience
		}
	}
}
namespace DOL.AI.Brain
{
	public class GnatAntsBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GnatAntsBrain() 
			: base()
		{
			AggroLevel = 100;
			AggroRange = 450;
		}

		public override void Think()
		{
			Body.IsWorthReward = false;
			base.Think();
		}
	}
}