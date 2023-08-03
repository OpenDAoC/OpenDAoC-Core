﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class BossKieracDestroyer : GameEpicBoss
	{
		public BossKieracDestroyer() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Kierac the Destroyer Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20; // dmg reduction for melee dmg
				case EDamageType.Crush: return 20; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
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
			get { return 30000; }
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
							truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
			Model = 826;
			Level = 69;
			Name = "Kierac the Destroyer";
			Size = 180;
			ParryChance = 50;

			Strength = 550;
			Dexterity = 200;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 2500;
			TetherRange = 1800;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 841, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = EDamageType.Slash;
			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
			SpawnMoP = false;

			KieracDestroyerBrain sbrain = new KieracDestroyerBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			bool success = base.AddToWorld();
			if (success)
            {
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is MasterOfPainBrain)
						{
							npc.RemoveFromWorld();
						}
					}
				}
			}
			return success;
		}
		private static bool SpawnMoP=false;
		public override void Die(GameObject killer)
		{
			if (SpawnMoP == false)
			{
				SpawnMasterOfPain();
				SpawnMoP = true;
			}
			base.Die(killer);
		}
		public void SpawnMasterOfPain()
		{
				MasterOfPain Add1 = new MasterOfPain();
				Add1.X = 33971;
				Add1.Y = 20939;
				Add1.Z = 11611;
				Add1.CurrentRegion = CurrentRegion;
				Add1.Heading = 39;
				Add1.RespawnInterval = -1;
				Add1.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class KieracDestroyerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public KieracDestroyerBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.Bladeturn))
				{
						Body.CastSpell(Bubble, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.Think();
		}
		private Spell m_Bubble;
		private Spell Bubble
		{
			get
			{
				if (m_Bubble == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 10;
					spell.Duration = 10;
					spell.ClientEffect = 5126;
					spell.Icon = 5126;
					spell.TooltipId = 5126;
					spell.Name = "Shield of Pain";
					spell.Range = 0;
					spell.SpellID = 11792;
					spell.Target = "Self";
					spell.Type = ESpellType.Bladeturn.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Bubble = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bubble);
				}
				return m_Bubble;
			}
		}
	}
}
/// <summary>
/// //////////////////////////////////////////////////////////////////Master of Pain//////////////////////////////////////////////////////////////////
/// </summary>
namespace DOL.GS
{
	public class MasterOfPain : GameEpicBoss
	{
		public MasterOfPain() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Master of Pain Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20; // dmg reduction for melee dmg
				case EDamageType.Crush: return 20; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
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
			get { return 30000; }
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
							truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163806);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(93);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
			RespawnInterval = -1;
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 442, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = EDamageType.Crush;

			MasterOfPainBrain sbrain = new MasterOfPainBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MasterOfPainBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MasterOfPainBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				Body.CastSpell(DebuffSC, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.Think();
		}
		private Spell m_DebuffSC;
		private Spell DebuffSC
		{
			get
			{
				if (m_DebuffSC == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = UtilCollection.Random(25,45);
					spell.Duration = 60;
					spell.Value = 75;
					spell.ClientEffect = 4387;
					spell.Icon = 4387;
					spell.TooltipId = 4387;
					spell.Name = "Vitality Dispersal";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11793;
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.StrengthConstitutionDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_DebuffSC = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DebuffSC);
				}
				return m_DebuffSC;
			}
		}
	}
}