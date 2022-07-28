using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Sarcondina : GameEpicBoss
	{
		public Sarcondina() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Sarcondina Initializing...");
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
			Model = 933;
			Level = 65;
			Name = "Sarcondina";
			Size = 150;

			Strength = 400;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			SarcondinaBrain sbrain = new SarcondinaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "SarcondinaAdd")
					npc.Die(this);
			}
			base.Die(killer);
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(45))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					if(!ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
						CastSpell(Sarcondina_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
		}
		private Spell m_Sarcondina_Dot;
		private Spell Sarcondina_Dot
		{
			get
			{
				if (m_Sarcondina_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 3411;
					spell.Icon = 3411;
					spell.Name = "Poison";
					spell.Description = "Inflicts 80 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 3411;
					spell.Range = 500;
					spell.Damage = 80;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11805;
					spell.Target = "Enemy";
					spell.SpellGroup = 1800;
					spell.EffectGroup = 1500;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Sarcondina_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Sarcondina_Dot);
				}
				return m_Sarcondina_Dot;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class SarcondinaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SarcondinaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool CanSpawnAdd = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnAdd = false;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "SarcondinaAdd")
						npc.Die(Body);
				}
			}
			if (HasAggro)
			{
				if (CanSpawnAdd == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnAdd), Util.Random(25000, 40000));
					CanSpawnAdd = true;
				}
			}
			base.Think();
		}
		private int SpawnAdd(ECSGameTimer timer)
		{
			if (HasAggro && Body.IsAlive)
			{
				GameNPC add = new GameNPC();
				add.Name = Body.Name + "'s servant";
				add.Model = 933;
				add.Size = (byte)Util.Random(45, 55);
				add.Level = (byte)Util.Random(55, 59);
				add.Strength = 120;
				add.Quickness = 80;
				add.MeleeDamageType = eDamageType.Crush;
				add.MaxSpeedBase = 225;
				add.PackageID = "SarcondinaAdd";
				add.RespawnInterval = -1;
				add.X = Body.X + Util.Random(-100, 100);
				add.Y = Body.Y + Util.Random(-100, 100);
				add.Z = Body.Z;
				add.CurrentRegion = Body.CurrentRegion;
				add.Heading = Body.Heading;
				add.Faction = FactionMgr.GetFactionByID(64);
				add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
				StandardMobBrain brain = new StandardMobBrain();
				add.SetOwnBrain(brain);
				brain.AggroRange = 800;
				brain.AggroLevel = 100;
				add.AddToWorld();
			}
			CanSpawnAdd = false;
			return 0;
		}
	}
}

