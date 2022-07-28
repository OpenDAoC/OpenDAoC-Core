using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class QanrisDuros : GameEpicBoss
	{
		public QanrisDuros() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Qan'ris Duros Initializing...");
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165072);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(20);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(20));

			QanrisDurosBrain sbrain = new QanrisDurosBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class QanrisDurosBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public QanrisDurosBrain() : base()
		{
			AggroLevel = 100;
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
			}
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(3500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "DurosBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				if (!Body.IsCasting && Util.Chance(30))
				{
					Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
					Body.CastSpell(Boss_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				}
			}
			base.Think();
		}
		private Spell m_Boss_PBAOE;
		private Spell Boss_PBAOE
		{
			get
			{
				if (m_Boss_PBAOE == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = Util.Random(8, 18);
					spell.ClientEffect = 1695;
					spell.Icon = 1695;
					spell.TooltipId = 1695;
					spell.Name = "Thunder Stomp";
					spell.Damage = 400;
					spell.Range = 500;
					spell.Radius = 1000;
					spell.SpellID = 11905;
					spell.Target = eSpellTarget.Area.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Energy;
					spell.Uninterruptible = true;
					m_Boss_PBAOE = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_PBAOE);
				}
				return m_Boss_PBAOE;
			}
		}
	}
}

