using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class GlacierGiant : GameEpicBoss
	{
		public GlacierGiant() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40; // dmg reduction for melee dmg
				case eDamageType.Crush: return 40; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 100000; }
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161360);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			GlacierGiantBrain.Clear_List = false;
			GlacierGiantBrain.RandomTarget = null;

			GlacierGiantBrain sbrain = new GlacierGiantBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameNPC[] npcs;

			npcs = WorldMgr.GetNPCsByNameFromRegion("Glacier Giant", 100, (eRealm)0);
			if (npcs.Length == 0)
			{
				log.Warn("Glacier Giant not found, creating it...");

				log.Warn("Initializing Glacier Giant...");
				GlacierGiant OF = new GlacierGiant();
				OF.Name = "Glacier Giant";
				OF.Model = 1384;
				OF.Realm = 0;
				OF.Level = 80;
				OF.Size = 255;
				OF.CurrentRegionID = 100;//OF odins gate

				OF.Strength = 5;
				OF.Intelligence = 150;
				OF.Piety = 150;
				OF.Dexterity = 200;
				OF.Constitution = 100;
				OF.Quickness = 125;
				OF.Empathy = 300;
				OF.BodyType = (ushort)NpcTemplateMgr.eBodyType.Magical;
				OF.MeleeDamageType = eDamageType.Slash;

				OF.X = 651517;
				OF.Y = 625897;
				OF.Z = 5320;
				OF.MaxDistance = 5500;
				OF.TetherRange = 5600;
				OF.MaxSpeedBase = 280;
				OF.Heading = 4003;

				GlacierGiantBrain ubrain = new GlacierGiantBrain();
				ubrain.AggroLevel = 0;
				ubrain.AggroRange = 600;
				OF.SetOwnBrain(ubrain);
				OF.AddToWorld();
				OF.Brain.Start();
				OF.SaveIntoDatabase();
			}
			else
				log.Warn("Glacier Giant exist ingame, remove it and restart server if you want to add by script code.");
		}
	}
}
namespace DOL.AI.Brain
{
	public class GlacierGiantBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public GlacierGiantBrain() : base()
		{
			AggroLevel = 0;//is neutral
			AggroRange = 600;
			ThinkInterval = 1500;
		}	
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				Clear_List = false;
				RandomTarget = null;
				if (Teleported_Players.Count>0)
					Teleported_Players.Clear();
				if(Enemys_To_Port.Count>0)
					Enemys_To_Port.Clear();
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if(Util.Chance(20))
						TeleportPlayer();
				}
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
				Body.Health = Body.MaxHealth;
			base.Think();
		}
	
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		List<GamePlayer> Enemys_To_Port = new List<GamePlayer>();
		List<GamePlayer> Teleported_Players = new List<GamePlayer>();
		public void TeleportPlayer()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1 && player != Body.TargetObject)
					{
						if (!Enemys_To_Port.Contains(player) && !Teleported_Players.Contains(RandomTarget))
							Enemys_To_Port.Add(player);
					}
				}
			}
			if (Enemys_To_Port.Count == 0)
				return;
			else
			{
				GamePlayer PortTarget = (GamePlayer)Enemys_To_Port[Util.Random(0, Enemys_To_Port.Count - 1)];
				RandomTarget = PortTarget;
				if (RandomTarget.IsAlive && RandomTarget != null && RandomTarget.IsWithinRadius(Body,2000) && !Teleported_Players.Contains(RandomTarget))
				{
					switch(Util.Random(1,6))
                    {
						case 1: RandomTarget.MoveTo(Body.CurrentRegionID, 663537 + Util.Random(-2000,2000), 626415 + Util.Random(-2000, 2000), 7790 + Util.Random(300, 600), Body.Heading); break;
						case 2: RandomTarget.MoveTo(Body.CurrentRegionID, 647342 + Util.Random(-2000, 2000), 617589 + Util.Random(-2000, 2000), 8533 + Util.Random(300, 600), Body.Heading); break;
						case 3: RandomTarget.MoveTo(Body.CurrentRegionID, 645157 + Util.Random(-2000, 2000), 630671 + Util.Random(-2000, 2000), 11530 + Util.Random(300, 600), Body.Heading); break;
						case 4: RandomTarget.MoveTo(Body.CurrentRegionID, 654502 + Util.Random(-2000, 2000), 630523 + Util.Random(-2000, 2000), 8762 + Util.Random(300, 600), Body.Heading); break;
						case 5: RandomTarget.MoveTo(Body.CurrentRegionID, 670626 + Util.Random(-2000, 2000), 630046 + Util.Random(-2000, 2000), 7515 + Util.Random(300, 600), Body.Heading); break;
						case 6: RandomTarget.MoveTo(Body.CurrentRegionID, 642185 + Util.Random(-2000, 2000), 620183 + Util.Random(-2000, 2000), 10014 + Util.Random(300, 600), Body.Heading); break;
					}
					Enemys_To_Port.Remove(RandomTarget);
					foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
					{
						if (player != null)
							player.Out.SendMessage("Glacier Giant kick away " + RandomTarget.Name + "!", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
					}					
					if (RandomTarget != null && RandomTarget.IsAlive && !Teleported_Players.Contains(RandomTarget))
					{
						Teleported_Players.Add(RandomTarget);
						if (Clear_List == false)
						{
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ListCleanTimer), 45000);//clear list of teleported players, so it will not pick instantly already teleported target
							Clear_List = true;
						}
					}					
					RandomTarget = null;
				}
			}
		}
		public static bool Clear_List = false;
		public int ListCleanTimer(ECSGameTimer timer)
        {
			if (Body.IsAlive && Body.InCombat && HasAggro && Teleported_Players.Count > 0)
			{
				Teleported_Players.Clear();
				Clear_List = false;
			}
			return 0;
        }
	}
}
