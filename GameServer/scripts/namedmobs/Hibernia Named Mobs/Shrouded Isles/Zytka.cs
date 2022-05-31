using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Zytka : GameEpicBoss
	{
		public Zytka() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Zytka Initializing...");
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
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 50; }
		public override short Strength { get => base.Strength; set => base.Strength = 250; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Zytka";
			Model = 904;
			Size = 120;
			Level = (byte)Util.Random(65,70);
			MaxDistance = 2500;
			TetherRange = 2600;
			Faction = FactionMgr.GetFactionByID(96);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
			MaxSpeedBase = 280;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			ZytkaBrain sbrain = new ZytkaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			foreach (GameNPC npc in GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive && npc.Brain is ZytkaAddBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(ZytkaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			base.OnAttackEnemy(ad);
		}
		private Spell m_ZytkaDD;
		public Spell ZytkaDD
		{
			get
			{
				if (m_ZytkaDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 4111;
					spell.Icon = 4111;
					spell.Damage = 250;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Fire Blast";
					spell.Range = 500;
					spell.SpellID = 11908;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_ZytkaDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ZytkaDD);
				}
				return m_ZytkaDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class ZytkaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ZytkaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		private bool spawnAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				spawnAdds = false;
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Brain is ZytkaAddBrain)
						npc.RemoveFromWorld();
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				if (!spawnAdds)
				{
					SpawnAdds();
					spawnAdds = true;
				}
			}
			base.Think();
		}
		private void SpawnAdds()
		{
			for (int i = 0; i < Util.Random(5, 6); i++)
			{
				ZytkaAdd add = new ZytkaAdd();
				add.X = Body.X + Util.Random(-300, 300);
				add.Y = Body.Y + Util.Random(-300, 300);
				add.Z = Body.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
		}
	}
}

#region Zytka's adds
namespace DOL.GS
{
	public class ZytkaAdd : GameNPC
	{
		public ZytkaAdd() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get { return 2000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		#endregion

		public override bool AddToWorld()
		{
			Model = 904;
			Name = "young octonoid";
			Level = 50;
			Size = (byte)Util.Random(35, 45);
			RespawnInterval = -1;
			RoamingRange = 200;
			MaxDistance = 2500;
			TetherRange = 2600;
			Faction = FactionMgr.GetFactionByID(96);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

			LoadedFromScript = true;
			ZytkaAddBrain sbrain = new ZytkaAddBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class ZytkaAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ZytkaAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion