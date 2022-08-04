using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Durval : GameEpicBoss
	{
		public Durval() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Durval Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
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
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160233);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			DurvalBrain sbrain = new DurvalBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void StartAttack(GameObject target)
        {
        }
    }
}
namespace DOL.AI.Brain
{
	public class DurvalBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DurvalBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(DurvalDisease))
					Body.Spells.Add(DurvalDisease);
				if (!Body.Spells.Contains(Durval_DD))
					Body.Spells.Add(Durval_DD);
				if (!Body.Spells.Contains(Bubble))
					Body.Spells.Add(Bubble);
				if (!Body.Spells.Contains(Boss_Mezz))
					Body.Spells.Add(Boss_Mezz);
			}
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro)
			{
				foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(3500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "DurvalBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				if (Body.TargetObject != null)
				{
					if (!Body.IsCasting && !Body.IsMoving)
					{
						foreach (Spell spells in Body.Spells)
						{
							if (spells != null)
							{
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

								Body.TurnTo(Body.TargetObject);
								if (Util.Chance(100))
								{
									GameLiving target = Body.TargetObject as GameLiving;
									if (target != null)
									{
										if (!Body.IsCasting && Body.GetSkillDisabledDuration(Boss_Mezz) == 0 && !target.effectListComponent.ContainsEffectForEffectType(eEffect.Mez) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.MezImmunity))
											Body.CastSpell(Boss_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
										else if (!Body.IsCasting && Body.GetSkillDisabledDuration(DurvalDisease) == 0 && !target.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
											Body.CastSpell(DurvalDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
										else if (!Body.IsCasting && Body.GetSkillDisabledDuration(Bubble) == 0 && !target.effectListComponent.ContainsEffectForEffectType(eEffect.Bladeturn))
											Body.CastSpell(Bubble, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
										else
											Body.CastSpell(Durval_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									}
								}
							}
						}
					}
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_Boss_Mezz;
		private Spell Boss_Mezz
		{
			get
			{
				if (m_Boss_Mezz == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 40;
					spell.Duration = 60;
					spell.ClientEffect = 975;
					spell.Icon = 975;
					spell.Name = "Mesmerize";
					spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
					spell.TooltipId = 2619;
					spell.Radius = 450;
					spell.Range = 1500;
					spell.SpellID = 18912;
					spell.Target = "Enemy";
					spell.Type = "Mesmerize";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Spirit;
					m_Boss_Mezz = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Mezz);
				}
				return m_Boss_Mezz;
			}
		}
		private Spell m_DurvalDisease;
		private Spell DurvalDisease
		{
			get
			{
				if (m_DurvalDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 40;
					spell.ClientEffect = 731;
					spell.Icon = 731;
					spell.Name = "Durval's Plague";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 731;
					spell.Range = 1500;
					spell.Duration = 120;
					spell.SpellID = 11911;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_DurvalDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DurvalDisease);
				}
				return m_DurvalDisease;
			}
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
					spell.Name = "Shield of Bones";
					spell.Range = 0;
					spell.SpellID = 11910;
					spell.Target = "Self";
					spell.Type = eSpellType.Bladeturn.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Bubble = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bubble);
				}
				return m_Bubble;
			}
		}
		private Spell m_Durval_DD;
		private Spell Durval_DD
		{
			get
			{
				if (m_Durval_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = 0;
					spell.ClientEffect = 1609;
					spell.Icon = 1609;
					spell.TooltipId = 1609;
					spell.Damage = 550;
					spell.Name = "Smite";
					spell.Range = 1800;
					spell.SpellID = 11913;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Spirit;
					m_Durval_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Durval_DD);
				}
				return m_Durval_DD;
			}
		}
		#endregion
	}
}

