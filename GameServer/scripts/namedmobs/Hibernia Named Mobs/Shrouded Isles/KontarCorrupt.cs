using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class KontarCorrupt : GameEpicBoss
	{
		public KontarCorrupt() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Kontar the Corrupt Initializing...");
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
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 350; }
		#endregion

		public override bool AddToWorld()
		{
			Name = "Kontar the Corrupt";
			Model = 767;
			Size = 120;
			Level = 65;
			MaxDistance = 2500;
			TetherRange = 2600;
			Faction = FactionMgr.GetFactionByID(96);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
			MaxSpeedBase = 280;

			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			KontarCorruptBrain sbrain = new KontarCorruptBrain();
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
				if (npc != null && npc.IsAlive && npc.Brain is CorruptorBodyguardBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class KontarCorruptBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public KontarCorruptBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		private bool spawnAdds = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				spawnAdds = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
					{
						if (npc != null && npc.IsAlive && npc.Brain is CorruptorBodyguardBrain)
							npc.RemoveFromWorld();
					}
					RemoveAdds = true;
				}
			}
			if(HasAggro && Body.TargetObject != null)
            {
				RemoveAdds = false;
				if(!spawnAdds)
                {
					SpawnAdds();
					spawnAdds = true;
                }
            }
			base.Think();
		}
		private void SpawnAdds()
		{
			for (int i = 0; i < Util.Random(8, 10); i++)
			{
				CorruptorBodyguard add = new CorruptorBodyguard();
				add.X = Body.X + Util.Random(-500, 500);
				add.Y = Body.Y + Util.Random(-500, 500);
				add.Z = Body.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
		}
	}
}

#region Kontar's adds
namespace DOL.GS
{
	public class CorruptorBodyguard : GameNPC
	{
		public CorruptorBodyguard() : base()
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
			Model = 767;
			Name = "corruptor bodyguard";
			Level = (byte)Util.Random(50, 55);
			Size = (byte)Util.Random(35, 45);
			RespawnInterval = -1;
			RoamingRange = 200;
			MaxDistance = 2500;
			TetherRange = 2600;
			Faction = FactionMgr.GetFactionByID(96);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

			LoadedFromScript = true;
			CorruptorBodyguardBrain sbrain = new CorruptorBodyguardBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(25))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(CorruptorBodyguardDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		private Spell m_CorruptorBodyguardDD;
		public Spell CorruptorBodyguardDD
		{
			get
			{
				if (m_CorruptorBodyguardDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 5;
					spell.ClientEffect = 4359;
					spell.Icon = 4359;
					spell.Damage = 150;
					spell.DamageType = (int)eDamageType.Energy;
					spell.Name = "Energy Blast";
					spell.Range = 500;
					spell.Radius = 300;
					spell.SpellID = 11907;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_CorruptorBodyguardDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CorruptorBodyguardDD);
				}
				return m_CorruptorBodyguardDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class CorruptorBodyguardBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CorruptorBodyguardBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				foreach(GameNPC npc in Body.GetNPCsInRadius(1500))
                {
					if(npc != null && npc.IsAlive && npc.Brain is KontarCorruptBrain && npc.HealthPercent < 100)
                    {					
						if (!Body.IsCasting)
						{
							Body.TargetObject = npc;
							Body.TurnTo(npc);
							Body.CastSpell(CorruptorBodyguardHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
						}
					}
                }
			}
			base.Think();
		}
		#region Spells
		private Spell m_CorruptorBodyguardHeal;
		private Spell CorruptorBodyguardHeal
		{
			get
			{
				if (m_CorruptorBodyguardHeal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 5;
					spell.ClientEffect = 1340;
					spell.Icon = 1340;
					spell.TooltipId = 1340;
					spell.Value = 200;
					spell.Name = "Corruptor Bodyguard's Heal";
					spell.Range = 1500;
					spell.SpellID = 11906;
					spell.Target = "Realm";
					spell.Type = eSpellType.Heal.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_CorruptorBodyguardHeal = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CorruptorBodyguardHeal);
				}
				return m_CorruptorBodyguardHeal;
			}
		}		
		#endregion
	}
}
#endregion