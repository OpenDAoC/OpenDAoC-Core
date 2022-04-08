using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;

namespace DOL.GS
{
	public class Mahattava : GameEpicBoss
	{
		public Mahattava() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mahattava Initializing...");
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
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackSpeed(params InventoryItem[] weapon)
		{
			return base.AttackSpeed(weapon) * 2;
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
			return 0.45;
		}
		public override int MaxHealth
		{
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(83022);
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

			Faction = FactionMgr.GetFactionByID(159); //Servants of Iarnvidiur
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(159)); //Servants of Iarnvidiur
			MahattavaBrain sbrain = new MahattavaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MahattavaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MahattavaBrain() : base()
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
							if (npc.IsAlive && npc.PackageID == "MahattavaBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain);
							}
						}
					}
					IsPulled = true;
				}
				if(Body.TargetObject != null)
                {
					GameLiving target = Body.TargetObject as GameLiving;
					if (target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
						Body.CastSpell(Mahattava_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					else
						Body.CastSpell(Mahattava_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.Think();
		}
		private Spell m_Mahattava_Dot;
		private Spell Mahattava_Dot
		{
			get
			{
				if (m_Mahattava_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3411;
					spell.Icon = 3411;
					spell.Name = "Mahattava's Infection";
					spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 3411;
					spell.Range = 1500;
					spell.Damage = 80;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11804;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Mahattava_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mahattava_Dot);
				}
				return m_Mahattava_Dot;
			}
		}
		private Spell m_Mahattava_DD;
		private Spell Mahattava_DD
		{
			get
			{
				if (m_Mahattava_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = Util.Random(10,15);
					spell.ClientEffect = 3494;
					spell.Icon = 3494;
					spell.Name = "Mahattava's Strike";
					spell.TooltipId = 3494;
					spell.Range = 1500;
					spell.Damage = 300;
					spell.SpellID = 11804;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter; 
					m_Mahattava_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mahattava_DD);
				}
				return m_Mahattava_DD;
			}
		}
	}
}

