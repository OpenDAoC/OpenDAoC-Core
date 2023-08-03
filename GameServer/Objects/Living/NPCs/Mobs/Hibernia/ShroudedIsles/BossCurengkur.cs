﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class BossCurengkur : GameEpicBoss
	{
		public BossCurengkur() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Curengkur Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
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
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159530);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			SpawnNest();

			Faction = FactionMgr.GetFactionByID(69);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(69));
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			CurengkurBrain sbrain = new CurengkurBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNpc npc in GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is CurengkurNestBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
        }
        private void SpawnNest()
        {
			foreach (GameNpc npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is CurengkurNestBrain)
					return;
			}
			CurengkurNest nest = new CurengkurNest();
			nest.X = X;
			nest.Y = Y;
			nest.Z = Z;
			nest.Heading = Heading;
			nest.CurrentRegion = CurrentRegion;
			nest.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class CurengkurBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CurengkurBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
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
			Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
			if(Body.IsWithinRadius(spawn, 800))
            {
				Body.Health += 200;
            }
			if (HasAggro && Body.TargetObject != null)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is CurengkurNestBrain brain)
					{
						if (!brain.HasAggro && target.IsAlive && target != null)
							brain.AddToAggroList(target, 10);
					}
				}				
				if (UtilCollection.Chance(50) && !Body.IsCasting)
					Body.CastSpell(CurengkurDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				if (UtilCollection.Chance(50) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
					Body.CastSpell(CurengkurPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			base.Think();
		}
		#region Spells
		private Spell m_CurengkurDD;
		public Spell CurengkurDD
		{
			get
			{
				if (m_CurengkurDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 10;
					spell.Power = 0;
					spell.ClientEffect = 4159;
					spell.Icon = 4159;
					spell.Damage = 400;
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Curengkur's Strike";
					spell.Range = 1500;
					spell.SpellID = 11903;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_CurengkurDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CurengkurDD);
				}
				return m_CurengkurDD;
			}
		}		
		private Spell m_CurengkurPoison;
		private Spell CurengkurPoison
		{
			get
			{
				if (m_CurengkurPoison == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 3475;
					spell.Icon = 3475;
					spell.TooltipId = 3475;
					spell.Name = "Poison";
					spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.Damage = 100;
					spell.Duration = 30;
					spell.Frequency = 30;
					spell.Range = 500;
					spell.SpellID = 11904;
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.DamageOverTime.ToString();
					spell.DamageType = (int)EDamageType.Body;
					spell.Uninterruptible = true;
					m_CurengkurPoison = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CurengkurPoison);
				}
				return m_CurengkurPoison;
			}
		}
		#endregion
	}
}
////////////////////////////////////////////////////////////////////Nest////////////////////////////////////
#region Curengkur Nest
namespace DOL.GS
{
	public class CurengkurNest : GameNpc
	{
		public CurengkurNest() : base()
		{
		}
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 400; }
        #endregion
        public override void StartAttack(GameObject target)
        {
        }
        public override bool AddToWorld()
		{
			Model = 665;
			Name = "Curengkur's Nest Radiation";
			Level = 70;
			Size = (byte)UtilCollection.Random(50, 55);
			RespawnInterval = 5000;
			Flags = (GameNpc.eFlags)42;
			MaxSpeedBase = 0;
			Faction = FactionMgr.GetFactionByID(69);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(69));

			LoadedFromScript = true;
			CurengkurNestBrain sbrain = new CurengkurNestBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class CurengkurNestBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CurengkurNestBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro)
				Body.CastSpell(CurengkurDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);

			base.Think();
		}

		private Spell m_CurengkurDD2;
		public Spell CurengkurDD2
		{
			get
			{
				if (m_CurengkurDD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 4;
					spell.Power = 0;
					spell.ClientEffect = 1141;
					spell.Icon = 1141;
					spell.Damage = 200;
					spell.DamageType = (int)EDamageType.Heat;
					spell.Name = "Curengkur's Radiation";
					spell.Range = 0;
					spell.Radius = 800;
					spell.SpellID = 11903;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_CurengkurDD2 = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CurengkurDD2);
				}
				return m_CurengkurDD2;
			}
		}
	}
}
#endregion