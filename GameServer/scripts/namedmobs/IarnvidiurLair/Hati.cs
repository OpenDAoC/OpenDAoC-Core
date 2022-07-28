using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;

namespace DOL.GS
{
	public class Hati : GameEpicBoss
	{
		public Hati() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Hati Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20; // dmg reduction for melee dmg
				case eDamageType.Crush: return 20; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
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
			get { return 30000; }
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(35))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					CastSpell(HatiDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
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
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(83026);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(159);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(159));

			HatiBrain sbrain = new HatiBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		private Spell m_HatiDD;
		private Spell HatiDD
		{
			get
			{
				if (m_HatiDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 4568;
					spell.Icon = 4568;
					spell.Name = "Hati's Bite";
					spell.TooltipId = 4568;
					spell.Damage = 260;
					spell.Range = 350;
					spell.SpellID = 11815;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Cold;
					m_HatiDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HatiDD);
				}
				return m_HatiDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class HatiBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public HatiBrain() : base()
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
			if (Body.InCombat && HasAggro && Body.TargetObject != null)
			{
				if (IsPulled == false)
				{
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.PackageID == "HatiBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain);
							}
						}
					}
					IsPulled = true;
				}
				if (Body.TargetObject != null)
				{
					Body.CastSpell(Hati_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.Think();
		}
		private Spell m_Hati_Mezz;
		private Spell Hati_Mezz
		{
			get
			{
				if (m_Hati_Mezz == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 30;
					spell.Duration = 29;
					spell.ClientEffect = 2619;
					spell.Icon = 2619;
					spell.Name = "Hati's Mezz";
					spell.TooltipId = 2619;
					spell.Radius = 450;
					spell.Range = 0;
					spell.SpellID = 11813;
					spell.Target = "Enemy";
					spell.Type = "Mesmerize";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Spirit;
					m_Hati_Mezz = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hati_Mezz);
				}
				return m_Hati_Mezz;
			}
		}	
	}
}

