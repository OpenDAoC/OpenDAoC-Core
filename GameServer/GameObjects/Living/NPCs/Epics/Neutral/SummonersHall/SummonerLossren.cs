﻿using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SummonerLossren : GameEpicBoss
	{
		public SummonerLossren() : base() { }
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
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (IsOutOfTetherRange)//dont take any dmg if is too far away from spawn point
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else//take dmg
				{
					if (source is GameSummonedPet)
					{
						base.TakeDamage(source, damageType, 5, 5);
					}
					else
						base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override double AttackDamage(DbInventoryItem weapon)
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18806);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Faction = FactionMgr.GetFactionByID(206);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			IsCloakHoodUp = true;
			SummonerLossrenBrain.IsCreatingSouls = false;
			TorturedSouls.TorturedSoulKilled = 0;

			SummonerLossrenBrain sbrain = new SummonerLossrenBrain();
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

			npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Lossren", 248, (eRealm)0);
			if (npcs.Length == 0)
			{
				log.Warn("Summoner Lossren not found, creating it...");

				log.Warn("Initializing Summoner Lossren...");
				SummonerLossren OF = new SummonerLossren();
				OF.Name = "Summoner Lossren";
				OF.Model = 343;
				OF.Realm = 0;
				OF.Level = 75;
				OF.Size = 65;
				OF.CurrentRegionID = 248;//OF summoners hall

				OF.Strength = 5;
				OF.Intelligence = 200;
				OF.Piety = 200;
				OF.Dexterity = 200;
				OF.Constitution = 100;
				OF.Quickness = 125;
				OF.Empathy = 300;
				OF.BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;
				OF.MeleeDamageType = eDamageType.Crush;
				OF.Faction = FactionMgr.GetFactionByID(206);
				OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

				OF.X = 39273;
				OF.Y = 41166;
				OF.Z = 15998;
				OF.MaxDistance = 2000;
				OF.TetherRange = 1300;
				OF.MaxSpeedBase = 250;
				OF.Heading = 967;
				OF.IsCloakHoodUp = true;

				SummonerLossrenBrain ubrain = new SummonerLossrenBrain();
				ubrain.AggroLevel = 100;
				ubrain.AggroRange = 600;
				OF.SetOwnBrain(ubrain);
				OF.AddToWorld();
				OF.Brain.Start();
				OF.SaveIntoDatabase();
			}
			else
				log.Warn("Summoner Lossren exist ingame, remove it and restart server if you want to add by script code.");
		}
	}
}
namespace DOL.AI.Brain
{
	public class SummonerLossrenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SummonerLossrenBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsCreatingSouls = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				TorturedSouls.TorturedSoulKilled = 0;
				TorturedSouls.TorturedSoulCount = 0;
				if (!RemoveAdds)
				{
					foreach (GameNPC souls in Body.GetNPCsInRadius(5000))
					{
						if (souls != null)
						{
							if (souls.IsAlive && (souls.Brain is TorturedSoulsBrain || souls.Brain is ExplodeUndeadBrain))
							{
								souls.RemoveFromWorld();
							}
						}
					}
					RemoveAdds = true;
				}
			}
			if (Body.InCombat && Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if(IsCreatingSouls==false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoSpawn), Util.Random(5000, 8000));//every 5-8s it will spawn tortured souls
					IsCreatingSouls =true;
                }
				foreach(GameNPC souls in Body.GetNPCsInRadius(4000))
                {
					if(souls != null)
                    {
						if(souls.IsAlive && souls.Brain is TorturedSoulsBrain)
                        {
							AddAggroListTo(souls.Brain as TorturedSoulsBrain);
						}
                    }
                }
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
			{
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
		public int DoSpawn(ECSGameTimer timer)
        {
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if(TorturedSouls.TorturedSoulCount == 0)
					SpawnSouls();
			}
			SpawnBigZombie();
			IsCreatingSouls = false;
			return 0;
        }
		public void SpawnSouls()
		{
			Point3D point1 = new Point3D(39189, 41889, 16000);
			Point3D point2 = new Point3D(38505, 41211, 16001);
			Point3D point3 = new Point3D(39180, 40583, 16000);
			Point3D point4 = new Point3D(39745, 41176, 16001);

			for (int i = 0; i < Util.Random(18, 25); i++)//create 18-25 souls every time timer will launch
			{

				TorturedSouls add = new TorturedSouls();
				switch (Util.Random(1, 4))
				{
					case 1:
						{
							add.X = point1.X + Util.Random(-100, 100);
							add.Y = point1.Y + Util.Random(-100, 100);
							add.Z = point1.Z;
						}
						break;
					case 2:
						{
							add.X = point2.X + Util.Random(-100, 100);
							add.Y = point2.Y + Util.Random(-100, 100);
							add.Z = point2.Z;
						}
						break;
					case 3:
						{
							add.X = point3.X + Util.Random(-100, 100);
							add.Y = point3.Y + Util.Random(-100, 100);
							add.Z = point3.Z;
						}
						break;
					case 4:
						{
							add.X = point4.X + Util.Random(-100, 100);
							add.Y = point4.Y + Util.Random(-100, 100);
							add.Z = point4.Z;
						}
						break;
				}
				add.CurrentRegion = Body.CurrentRegion;
				add.Heading = Body.Heading;
				add.AddToWorld();
				add.LoadedFromScript = true;
				++TorturedSouls.TorturedSoulCount;
			}
		}
		public void SpawnBigZombie()
        {
			Point3D point1 = new Point3D(39189, 41889, 16000);
			Point3D point2 = new Point3D(38505, 41211, 16001);
			Point3D point3 = new Point3D(39180, 40583, 16000);
			Point3D point4 = new Point3D(39745, 41176, 16001);
			if (TorturedSouls.TorturedSoulKilled == 20 && ExplodeUndead.ExplodeZombieCount==0)//spawn explode zombie
			{
				ExplodeUndead add2 = new ExplodeUndead();
				switch (Util.Random(1, 4))
				{
					case 1:
						{
							add2.X = point1.X + Util.Random(-100, 100);
							add2.Y = point1.Y + Util.Random(-100, 100);
							add2.Z = point1.Z;
						}
						break;
					case 2:
						{
							add2.X = point2.X + Util.Random(-100, 100);
							add2.Y = point2.Y + Util.Random(-100, 100);
							add2.Z = point2.Z;
						}
						break;
					case 3:
						{
							add2.X = point3.X + Util.Random(-100, 100);
							add2.Y = point3.Y + Util.Random(-100, 100);
							add2.Z = point3.Z;
						}
						break;
					case 4:
						{
							add2.X = point4.X + Util.Random(-100, 100);
							add2.Y = point4.Y + Util.Random(-100, 100);
							add2.Z = point4.Z;
						}
						break;
				}
				add2.CurrentRegion = Body.CurrentRegion;
				add2.Heading = Body.Heading;
				add2.AddToWorld();
				add2.LoadedFromScript = true;
				TorturedSouls.TorturedSoulKilled = 0;
				++ExplodeUndead.ExplodeZombieCount;
			}
		}
	}
}
/////////////////////////////////////////////////////////////Adds...many adds... and even more adds !!!!!!!! /////////////////////////////////////////
namespace DOL.AI.Brain
{
	public class TorturedSoulsBrain : StandardMobBrain
	{
		public TorturedSoulsBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
			}
			base.Think();
		}
	}
}
namespace DOL.GS
{
	public class TorturedSouls : GameNPC
	{
		public override int MaxHealth
		{
			get { return 600; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.15;
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 25;// dmg reduction for melee dmg
				case eDamageType.Crush: return 25;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 25;// dmg reduction for melee dmg
				default: return 25;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(DbInventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 500;
		}
		public static int TorturedSoulCount = 0;
		public static int TorturedSoulKilled = 0;
		public override void Die(GameObject killer)
        {
			--TorturedSoulCount;
			++TorturedSoulKilled;
            base.Die(killer);
        }
        public override void DropLoot(GameObject killer)//dont drop loot
        {
        }
        List<string> soul_names = new List<string>()
		{
			"Aphryx's Tortured Soul","Arus's Tortured Soul","Briandina's Tortured Soul","Dwuanne's Tortured Soul",
			"Feraa's Tortured Soul","Klose's Tortured Soul","Lonar's Tortured Soul","Threepwood's Tortured Soul"
		};
		public override bool AddToWorld()
		{
			switch(Util.Random(1,2))
            {
				case 1:
                    {
						Model = 123;
						Name = (string)soul_names[Util.Random(0, soul_names.Count - 1)];
					}
					break;
				case 2:
                    {
						Model = 659;
						Name = (string)soul_names[Util.Random(0, soul_names.Count - 1)];
					}
					break;
			}
			RespawnInterval = -1;
			MaxSpeedBase = 200;
			RoamingRange = 150;
			Size = (byte)Util.Random(45,55);
			Level = (byte)Util.Random(48, 53);
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			TorturedSoulsBrain souls = new TorturedSoulsBrain();
			SetOwnBrain(souls);			
			base.AddToWorld();
			return true;
		}
	}
}
//////////////////////////////////////////////////////////////////////slow explode zombie///////////////////////////////////////////////////
namespace DOL.AI.Brain
{
	public class ExplodeUndeadBrain : StandardMobBrain
	{
		public ExplodeUndeadBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public static bool IsKilled = false;
		public static bool SetAggroAmount = false;
		public override void Think()
		{
			if (Body.IsAlive)
			{
				if (Body.TargetObject == null && ExplodeUndead.RandomTarget != null)
				{
					Body.TargetObject = ExplodeUndead.RandomTarget;
				}
				if (Body.TargetObject != null)
				{
					if (Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
					{
						if (IsKilled == false)
						{
							Body.CastSpell(Zombie_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(KillZombie), 500);
							IsKilled = true;
						}
					}
				}
			}
			if (Body.IsAlive && ExplodeUndead.RandomTarget != null )
            {
				if (SetAggroAmount == false)
				{
					AddToAggroList(ExplodeUndead.RandomTarget, 2000);
					SetAggroAmount = true;
				}
			}
			base.Think();
		}
		public int KillZombie(ECSGameTimer timer)
        {
			Body.Die(Body);
			return 0;
        }
		private Spell m_Zombie_aoe;
		private Spell Zombie_aoe
		{
			get
			{
				if (m_Zombie_aoe == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.ClientEffect = 6159;
					spell.Icon = 6159;
					spell.TooltipId = 6169;
					spell.Damage = 1000;
					spell.Name = "Plague";
					spell.Radius = 500;
					spell.Range = 500;
					spell.SpellID = 11760;
					spell.Target = eSpellTarget.ENEMY.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Zombie_aoe = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Zombie_aoe);
				}
				return m_Zombie_aoe;
			}
		}
	}
}
namespace DOL.GS
{
	public class ExplodeUndead : GameNPC
	{
		public override int MaxHealth
		{
			get { return 4000; }
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int AttackRange
		{
			get { return 200; }
			set { }
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 25;// dmg reduction for melee dmg
				case eDamageType.Crush: return 25;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 25;// dmg reduction for melee dmg
				default: return 25;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(DbInventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 150;
		}
		public static int ExplodeZombieCount = 0;
		public override void Die(GameObject killer)
		{
			--ExplodeZombieCount;
			RandomTarget = null;
			base.Die(killer);
		}
        public override void DropLoot(GameObject killer)//dont drop loot
        {
        }
        public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		List<GamePlayer> Zombie_Targets = new List<GamePlayer>();
		public override bool AddToWorld()
		{
			Model = 923;
			RandomTarget = null;
			ExplodeUndeadBrain.IsKilled = false;
			ExplodeUndeadBrain.SetAggroAmount = false;
			ExplodeZombieCount = 0;
			Zombie_Targets.Clear();
			Name = "infected ghoul";
			RespawnInterval = -1;
			MaxSpeedBase = 110;//slow so players can kite it
			Size = 70;
			Level = (byte)Util.Random(62, 65);
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			ExplodeUndeadBrain souls = new ExplodeUndeadBrain();
			SetOwnBrain(souls);
			bool success = base.AddToWorld();
			if (success)
			{
				foreach(GamePlayer player in GetPlayersInRadius(2000))
                {
					if(player != null)
                    {
						if(player.IsAlive && player.CharacterClass.ID != 12 && player.Client.Account.PrivLevel==1)
                        {
							if(!Zombie_Targets.Contains(player))
                            {
								Zombie_Targets.Add(player);
                            }
						}
                    }
                }
				if(Zombie_Targets.Count>0)
                {
					GamePlayer Target = (GamePlayer)Zombie_Targets[Util.Random(0, Zombie_Targets.Count - 1)];
					RandomTarget = Target;						
					BroadcastMessage(String.Format(this.Name+" crawls toward "+RandomTarget.Name+"!"));
				}
			}
			return success;
		}
	}
}
