using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Events;

namespace DOL.GS
{
	public class AncientSyver : GameEpicBoss
	{
		public AncientSyver() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Ancient Syver Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 50;// dmg reduction for rest resists
			}
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(35))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					CastSpell(SyverDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
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
			if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 700;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.55;
		}
		public override int MaxHealth
		{
			get { return 15000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(83007);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Level = Convert.ToByte(npcTemplate.Level);
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(159);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(159));

			AncientSyverBrain sbrain = new AncientSyverBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		private Spell m_SyverDD;
		private Spell SyverDD
		{
			get
			{
				if (m_SyverDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 4568;
					spell.Icon = 4568;
					spell.Name = "Ancient Syver's Strike";
					spell.TooltipId = 4568;
					spell.Damage = 350;
					spell.Range = 350;
					spell.Radius = 350;
					spell.SpellID = 11824;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Cold;
					m_SyverDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SyverDD);
				}
				return m_SyverDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class AncientSyverBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AncientSyverBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled = false;
			}
			if (Body.InCombat && HasAggro)
			{
				if (IsPulled == false)
				{
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.PackageID == "AncientSyverBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain);
							}
						}
					}
					IsPulled = true;
				}
				if (Body.TargetObject != null)
				{
					if (Util.Chance(15))
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
						{
							new RegionTimer(Body, new RegionTimerCallback(CastDisease), 1000);
						}
					}
					if (Util.Chance(15))
					{
						if (Syver_Str_Debuff.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
						{
							new RegionTimer(Body, new RegionTimerCallback(CastStrengthDebuff), 1000);
						}
					}
					if (Util.Chance(15))
					{
						if (!Body.effectListComponent.ContainsEffectForEffectType(eEffect.MeleeHasteBuff))
						{
							new RegionTimer(Body, new RegionTimerCallback(CastHasteBuff), 1000);
						}
					}
				}
			}
			base.Think();
		}
		public int CastStrengthDebuff(RegionTimer timer)
		{
			if (Body.TargetObject != null && HasAggro && Body.IsAlive)
			{
				Body.CastSpell(Syver_Str_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
		public int CastDisease(RegionTimer timer)
		{
			if (Body.TargetObject != null && HasAggro && Body.IsAlive)
			{
				Body.CastSpell(SyverDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
		public int CastHasteBuff(RegionTimer timer)
		{
			if (HasAggro && Body.IsAlive)
			{
				Body.CastSpell(Syver_Haste_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			return 0;
		}
        #region Spells
        private Spell m_Syver_Str_Debuff;
		private Spell Syver_Str_Debuff
		{
			get
			{
				if (m_Syver_Str_Debuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 30;
					spell.Duration = 60;
					spell.ClientEffect = 4537;
					spell.Icon = 4537;
					spell.Name = "Ancient Syver's Strength Debuff";
					spell.TooltipId = 4537;
					spell.Range = 1500;
					spell.Value = 46;
					spell.SpellID = 11826;
					spell.Target = "Enemy";
					spell.Type = eSpellType.StrengthDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Syver_Str_Debuff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Syver_Str_Debuff);
				}
				return m_Syver_Str_Debuff;
			}
		}
		private Spell m_SyverDisease;
		private Spell SyverDisease
		{
			get
			{
				if (m_SyverDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4375;
					spell.Icon = 4375;
					spell.Name = "Black Plague";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 4375;
					spell.Radius = 450;
					spell.Range = 0;
					spell.Duration = 210;
					spell.SpellID = 11825;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_SyverDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SyverDisease);
				}
				return m_SyverDisease;
			}
		}
		private Spell m_Syver_Haste_Buff;
		private Spell Syver_Haste_Buff
		{
			get
			{
				if (m_Syver_Haste_Buff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 35;
					spell.Duration = 20;
					spell.ClientEffect = 10535;
					spell.Icon = 10535;
					spell.Name = "Ancient Syver's Haste";
					spell.Message2 = "{0} begins attacking faster!";
					spell.Message4 = "{0}'s attacks return to normal.";
					spell.TooltipId = 10535;
					spell.Range = 0;
					spell.Value = 50;
					spell.SpellID = 11827;
					spell.Target = "Self";
					spell.Type = eSpellType.CombatSpeedBuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Syver_Haste_Buff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Syver_Haste_Buff);
				}
				return m_Syver_Haste_Buff;
			}
		}
        #endregion
    }
}


