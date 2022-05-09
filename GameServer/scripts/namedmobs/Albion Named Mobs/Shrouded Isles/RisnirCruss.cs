using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class RisnirCruss : GameEpicBoss
	{
		public RisnirCruss() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Ris'nir Cruss Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165335);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Flags = eFlags.FLYING;
			Faction = FactionMgr.GetFactionByID(20);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(20));

			RisnirCrussBrain sbrain = new RisnirCrussBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void EnemyKilled(GameLiving enemy)
        {
			if (enemy != null && enemy is GamePlayer)
            {
				GameNPC add = new GameNPC();
				add.Name = "Apparition of " + enemy.Name;
				add.Model = 902;
				add.Size = (byte)Util.Random(45, 55);
				add.Level = (byte)Util.Random(55, 59);
				add.Strength = 150;
				add.Quickness = 80;
				add.MeleeDamageType = eDamageType.Crush;
				add.MaxSpeedBase = 225;
				add.PackageID = "RisnirCrussAdd";
				add.RespawnInterval = -1;
				add.X = enemy.X;
				add.Y = enemy.Y;
				add.Z = enemy.Z;
				add.CurrentRegion = CurrentRegion;
				add.Heading = Heading;
				add.Faction = FactionMgr.GetFactionByID(20);
				add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(20));
				StandardMobBrain brain = new StandardMobBrain();
				add.SetOwnBrain(brain);
				brain.AggroRange = 600;
				brain.AggroLevel = 100;
				add.AddToWorld();
			}
            base.EnemyKilled(enemy);
        }
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "RisnirCrussAdd")
					npc.Die(this);
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class RisnirCrussBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RisnirCrussBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool NotInCombat = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				if(Body.Flags != GameNPC.eFlags.FLYING)
					Body.Flags = GameNPC.eFlags.FLYING;
				if (NotInCombat == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
					NotInCombat = true;
				}
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "RisnirCrussAdd")
						npc.Die(Body);
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				NotInCombat = false;
				if (Body.Flags != 0)
					Body.Flags = 0;
				if (!Body.IsCasting && Util.Chance(30))
				{
					Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
					Body.CastSpell(Boss_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				}
				if (!Body.IsCasting && Util.Chance(20))
				{
					Body.CastSpell(Boss_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				}
			}
			base.Think();
		}
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (Body.IsAlive && !HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(Body, Body, 6085, 0, false, 0x01);
				}
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
			if (Body.IsAlive && !HasAggro)
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			return 0;
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
					spell.RecastDelay = Util.Random(20, 25);
					spell.ClientEffect = 1695;
					spell.Icon = 1695;
					spell.TooltipId = 1695;
					spell.Name = "Thunder Stomp";
					spell.Damage = 650;
					spell.Range = 500;
					spell.Radius = 1000;
					spell.SpellID = 11898;
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
					spell.SpellID = 975;
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
	}
}

