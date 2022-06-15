using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

#region WoW scorp
namespace DOL.GS
{
	public class WrethOfWinterScorp : GameEpicBoss
	{
		public WrethOfWinterScorp() : base() { }

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
			return base.AttackDamage(weapon) * Strength / 150;
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
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 300; }
        public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 470;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 72;
			MaxSpeedBase = 280;
			Flags = eFlags.GHOST;
			MeleeDamageType = eDamageType.Thrust;
			WrethOfWinterScorpBrain sbrain = new WrethOfWinterScorpBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(25) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(WoWPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		private Spell m_WoWPoison;
		public Spell WoWPoison
		{
			get
			{
				if (m_WoWPoison == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 3475;
					spell.Icon = 3475;
					spell.TooltipId = 3475;
					spell.Name = "Poison";
					spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.Damage = 100;
					spell.Duration = 30;
					spell.Frequency = 30;
					spell.Range = 500;
					spell.SpellID = 11875;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.DamageType = (int)eDamageType.Body;
					spell.Uninterruptible = true;
					m_WoWPoison = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_WoWPoison);
				}
				return m_WoWPoison;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class WrethOfWinterScorpBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WrethOfWinterScorpBrain() : base()
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
			base.Think();
		}
	}
}
#endregion

#region WoW jotun
namespace DOL.GS
{
	public class WrethOfWinterJotun : GameEpicBoss
	{
		public WrethOfWinterJotun() : base() { }

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
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 300; }
		public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 1770;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 73;
			MaxSpeedBase = 280;
			MeleeDamageType = eDamageType.Crush;
			Flags = eFlags.GHOST;
			WrethOfWinterJotunBrain sbrain = new WrethOfWinterJotunBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(WoWDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			base.OnAttackEnemy(ad);
		}
		private Spell m_WoWDD;
		public Spell WoWDD
		{
			get
			{
				if (m_WoWDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 4075;
					spell.Icon = 4075;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Frost Shock";
					spell.Range = 500;
					spell.SpellID = 11876;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_WoWDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_WoWDD);
				}
				return m_WoWDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class WrethOfWinterJotunBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WrethOfWinterJotunBrain() : base()
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
			base.Think();
		}
	}
}
#endregion

#region WoW lich
namespace DOL.GS
{
	public class WrethOfWinterLich : GameEpicBoss
	{
		public WrethOfWinterLich() : base() { }

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
			return base.AttackDamage(weapon) * Strength / 80;
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
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 300; }
		public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 441;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 74;
			MaxSpeedBase = 280;
			MeleeDamageType = eDamageType.Crush;
			Flags = eFlags.GHOST;
			WrethOfWinterLichBrain sbrain = new WrethOfWinterLichBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				CastSpell(WoWDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			base.OnAttackEnemy(ad);
		}
		private Spell m_WoWDD;
		public Spell WoWDD
		{
			get
			{
				if (m_WoWDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 4075;
					spell.Icon = 4075;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Frost Shock";
					spell.Range = 500;
					spell.SpellID = 11877;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_WoWDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_WoWDD);
				}
				return m_WoWDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class WrethOfWinterLichBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WrethOfWinterLichBrain() : base()
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
			base.Think();
		}
	}
}
#endregion

#region WoW arachite
namespace DOL.GS
{
	public class WrethOfWinterArachite : GameEpicBoss
	{
		public WrethOfWinterArachite() : base() { }

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
			return base.AttackDamage(weapon) * Strength / 40;
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
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 300; }
		public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 466;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 75;
			MaxSpeedBase = 280;
			MeleeDamageType = eDamageType.Thrust;
			Flags = eFlags.GHOST;
			WrethOfWinterArachiteBrain sbrain = new WrethOfWinterArachiteBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(25) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(WoWPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		
		private Spell m_WoWPoison;
		public Spell WoWPoison
		{
			get
			{
				if (m_WoWPoison == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 3475;
					spell.Icon = 3475;
					spell.TooltipId = 3475;
					spell.Name = "Poison";
					spell.Description = "Inflicts 150 damage to the target every 3 sec for 30 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.Damage = 150;
					spell.Duration = 30;
					spell.Frequency = 30;
					spell.Range = 500;
					spell.SpellID = 11878;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.DamageType = (int)eDamageType.Body;
					spell.Uninterruptible = true;
					m_WoWPoison = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_WoWPoison);
				}
				return m_WoWPoison;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class WrethOfWinterArachiteBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WrethOfWinterArachiteBrain() : base()
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
			base.Think();
		}
	}
}
#endregion