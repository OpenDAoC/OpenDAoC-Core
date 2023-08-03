﻿using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class EpicKelic : GameEpicNpc
	{
		public EpicKelic() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
				if (!source.IsWithinRadius(spawn, TetherRange))//dont take any dmg 
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
							truc.Out.SendMessage(Name + " is immune to damage form this distance!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
			get { return 10000; }
		}
		public override bool AddToWorld()
		{
			foreach (GameNpc npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is KelicBrain)
					return false;
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50035);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			KelicBrain sbrain = new KelicBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class KelicBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public KelicBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
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
			if (Body.TargetObject != null && HasAggro)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Name.ToLower() == "servant of kelic" && npc.Brain is StandardMobBrain brain && npc != Body)
					{
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 100);
					}
				}
				if(UtilCollection.Chance(25) && target != null && target.IsAlive)
                {
					if(!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
						Body.CastSpell(KelicDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (UtilCollection.Chance(25) && target != null && target.IsAlive)
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
						Body.CastSpell(KelicDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				if (UtilCollection.Chance(25) && target != null && target.IsAlive)
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity))
						Body.CastSpell(KelicRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_KelicDD;
		private Spell KelicDD
		{
			get
			{
				if (m_KelicDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 562;
					spell.Icon = 562;
					spell.Damage = 89;
					spell.Frequency = 20;
					spell.Duration = 24;
					spell.DamageType = (int)EDamageType.Matter;
					spell.Name = "Delaceration";
					spell.Description = "Inflicts 89 damage to the target every 2 sec for 24 seconds";
					spell.Message1 = "Your body is covered with painful sores!";
					spell.Message2 = "{0}'s skin erupts in open wounds!";
					spell.Message3 = "The destructive energy wounding you fades.";
					spell.Message4 = "The destructive energy around {0} fades.";
					spell.Range = 1500;
					spell.SpellID = 12006;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_KelicDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_KelicDD);
				}
				return m_KelicDD;
			}
		}
		private Spell m_KelicDisease;
		private Spell KelicDisease
		{
			get
			{
				if (m_KelicDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 735;
					spell.Icon = 735;
					spell.Name = "Cursed Essence";
					spell.Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 735;
					spell.Range = 1500;
					spell.Duration = 120;
					spell.SpellID = 12008;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
					m_KelicDisease = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_KelicDisease);
				}
				return m_KelicDisease;
			}
		}
		private Spell m_KelicRoot;
		private Spell KelicRoot
		{
			get
			{
				if (m_KelicRoot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 688;
					spell.Icon = 688;
					spell.Duration = 73;
					spell.Value = 99;
					spell.DamageType = (int)EDamageType.Body;
					spell.Name = "Superior Leg Twisting";
					spell.Message1 = "Your feet are frozen to the ground!";
					spell.Message2 = "{0}'s feet are frozen to the ground!";
					spell.Range = 1500;
					spell.SpellID = 12009;
					spell.Target = "Enemy";
					spell.Type = ESpellType.SpeedDecrease.ToString();
					spell.Uninterruptible = true;
					m_KelicRoot = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_KelicRoot);
				}
				return m_KelicRoot;
			}
		}
		#endregion
	}
}