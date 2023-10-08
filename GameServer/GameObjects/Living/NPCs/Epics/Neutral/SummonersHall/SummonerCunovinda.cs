﻿using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SummonerCunovinda : GameEpicBoss
	{
		public SummonerCunovinda() : base() { }
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 40; // dmg reduction for melee dmg
				case EDamageType.Crush: return 40; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 100000; }
		}
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
						|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
						|| damageType == EDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else//take dmg
				{
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18805);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			SummonerCunovindaBrain.RandomTarget = null;
			SummonerCunovindaBrain.CanCast = false;
			Faction = FactionMgr.GetFactionByID(187);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));
			IsCloakHoodUp = true;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(EInventorySlot.TorsoArmor, 305, 43, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(EInventorySlot.ArmsArmor, 307, 43);
			template.AddNPCEquipment(EInventorySlot.LegsArmor, 306, 43);
			template.AddNPCEquipment(EInventorySlot.HandsArmor, 308, 43, 0, 0);
			template.AddNPCEquipment(EInventorySlot.FeetArmor, 309, 43, 0, 0);
			template.AddNPCEquipment(EInventorySlot.Cloak, 57, 54, 0, 0);
			template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 327, 43, 90, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.TwoHanded);

			SummonerCunovindaBrain sbrain = new SummonerCunovindaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			GameNPC[] npcs;

			npcs = WorldMgr.GetNPCsByNameFromRegion("Summoner Cunovinda", 248, (ERealm)0);
			if (npcs.Length == 0)
			{
				log.Warn("Summoner Cunovinda not found, creating it...");

				log.Warn("Initializing Summoner Cunovinda...");
				SummonerCunovinda OF = new SummonerCunovinda();
				OF.Name = "Summoner Cunovinda";
				OF.Model = 162;
				OF.Realm = 0;
				OF.Level = 75;
				OF.Size = 65;
				OF.CurrentRegionID = 248;//OF summoners hall

				OF.Strength = 5;
				OF.Intelligence = 200;
				OF.Piety = 200;
				OF.Dexterity = 200;
				OF.Constitution = 100;
				OF.Quickness = 80;
				OF.Empathy = 300;
				OF.BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;
				OF.MeleeDamageType = EDamageType.Crush;
				OF.Faction = FactionMgr.GetFactionByID(187);
				OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(206));

				OF.X = 26023;
				OF.Y = 36132;
				OF.Z = 15998;
				OF.MaxDistance = 2000;
				OF.TetherRange = 1300;
				OF.MaxSpeedBase = 250;
				OF.Heading = 19;
				OF.IsCloakHoodUp = true;

				SummonerCunovindaBrain ubrain = new SummonerCunovindaBrain();
				ubrain.AggroLevel = 100;
				ubrain.AggroRange = 600;
				OF.SetOwnBrain(ubrain);
				OF.AddToWorld();
				OF.Brain.Start();
				OF.SaveIntoDatabase();
			}
			else
				log.Warn("Summoner Cunovinda exist ingame, remove it and restart server if you want to add by script code.");
		}
	}
}
namespace DOL.AI.Brain
{
	public class SummonerCunovindaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SummonerCunovindaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if(ad.Damage > 0 && ad != null)
            {
				if(Util.Chance(15))//here edit to change teleport chance to happen
					PickRandomTarget();//start teleport here
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				RandomTarget = null;
				CanCast = false;
				if (Enemys_To_DD.Count > 0)
					Enemys_To_DD.Clear();//clear list if it reset
			}
			if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000) && !HasAggro)
			{
				Body.Health = Body.MaxHealth;
				CanCast = false;
				RandomTarget = null;
			}
			base.Think();
		}
		public static GamePlayer randomtarget = null;
		public static GamePlayer RandomTarget
		{
			get { return randomtarget; }
			set { randomtarget = value; }
		}
		public static bool CanCast = false;
		List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
		public void PickRandomTarget()
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DD.Contains(player))
							Enemys_To_DD.Add(player);//add player to list
					}
				}
			}
			if (Enemys_To_DD.Count > 0)
			{
				if (CanCast == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastBolt), 1000);
					CanCast = true;
				}
			}
		}
		public int CastBolt(EcsGameTimer timer)
		{
			GameLiving oldTarget = (GameLiving)Body.TargetObject;//old target
			if (RandomTarget != null && RandomTarget.IsAlive)
			{
				Body.TurnTo(RandomTarget);//turn to randomtarget
				Body.StopFollowing();//stop follow
				Body.CastSpell(CunovindaBolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);//cast bolt

				RandomTarget.MoveTo(Body.CurrentRegionID, 24874, 36116, 17060, 3065);//port player to loc

				if(Body.TargetObject != null && Body.TargetObject != RandomTarget)
					Body.TargetObject = RandomTarget;//set target as randomtarget
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetBolt), Util.Random(8000, 12000));//teleport every 8-12s if melee hit got chance to proc teleport
			return 0;
		}
		public int ResetBolt(EcsGameTimer timer)//reset here so boss can start dot again
		{
			RandomTarget = null;
			CanCast = false;
			return 0;
		}
		#region Cunovinda Spells
		private Spell m_CunovindaBolt;
		public Spell CunovindaBolt
		{
			get
			{
				if (m_CunovindaBolt == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 2970;
					spell.Icon = 2970;
					spell.TooltipId = 2970;
					spell.Damage = 200;
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Summoner Bolt";
					spell.Range = 1800;
					spell.SpellID = 11761;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.Type = ESpellType.Bolt.ToString();
					m_CunovindaBolt = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CunovindaBolt);
				}
				return m_CunovindaBolt;
			}
		}
		#endregion
	}
}