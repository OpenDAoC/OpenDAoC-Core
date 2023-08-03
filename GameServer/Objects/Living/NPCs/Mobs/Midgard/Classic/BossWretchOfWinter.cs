﻿using DOL.AI.Brain;
using DOL.Database;

#region WoW scorp
namespace DOL.GS
{
	public class BossWretchOfWinterScorp : GameEpicBoss
	{
		public BossWretchOfWinterScorp() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 470;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 74;
			MaxSpeedBase = 280;
			Flags = eFlags.GHOST;
			MeleeDamageType = EDamageType.Thrust;
			WretchOfWinterScorpBrain sbrain = new WretchOfWinterScorpBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (UtilCollection.Chance(25) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
			{
				if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
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
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.DamageOverTime.ToString();
					spell.DamageType = (int)EDamageType.Body;
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
	public class WretchOfWinterScorpBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterScorpBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
#endregion

#region WoW spider
namespace DOL.GS
{
	public class BossWretchOfWinterSpider : GameEpicBoss
	{
		public BossWretchOfWinterSpider() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 453;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 70;
			MaxSpeedBase = 280;
			Flags = eFlags.GHOST;
			MeleeDamageType = EDamageType.Thrust;
			WretchOfWinterSpiderBrain sbrain = new WretchOfWinterSpiderBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (UtilCollection.Chance(25) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
			{
				if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
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
					spell.SpellID = 11956;
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.DamageOverTime.ToString();
					spell.DamageType = (int)EDamageType.Body;
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
	public class WretchOfWinterSpiderBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterSpiderBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
#endregion

#region WoW crab
namespace DOL.GS
{
	public class BossWretchOfWinterCrab : GameEpicBoss
	{
		public BossWretchOfWinterCrab() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 70;
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
		public override short Strength { get => base.Strength; set => base.Strength = 330; }
		public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 577;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 74;
			MaxSpeedBase = 280;
			Flags = eFlags.GHOST;
			MeleeDamageType = EDamageType.Thrust;
			WretchOfWinterCrabBrain sbrain = new WretchOfWinterCrabBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (UtilCollection.Chance(25) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
			{
				if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
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
					spell.SpellID = 11959;
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.DamageOverTime.ToString();
					spell.DamageType = (int)EDamageType.Body;
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
	public class WretchOfWinterCrabBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterCrabBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
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
	public class BossWretchOfWinterJotun : GameEpicBoss
	{
		public BossWretchOfWinterJotun() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
		public override short Strength { get => base.Strength; set => base.Strength = 330; }
		public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 1770;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 77;
			MaxSpeedBase = 280;
			MeleeDamageType = EDamageType.Crush;
			Flags = eFlags.GHOST;
			WretchOfWinterJotunBrain sbrain = new WretchOfWinterJotunBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				if (UtilCollection.Chance(25))
					CastSpell(WoWDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
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
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Frost Shock";
					spell.Range = 500;
					spell.SpellID = 11876;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
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
	public class WretchOfWinterJotunBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterJotunBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
#endregion

#region WoW Ogre
namespace DOL.GS
{
	public class BossWretchOfWinterOgre : GameEpicBoss
	{
		public BossWretchOfWinterOgre() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 460;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 71;
			MaxSpeedBase = 280;
			MeleeDamageType = EDamageType.Crush;
			Flags = eFlags.GHOST;
			WretchOfWinterOgreBrain sbrain = new WretchOfWinterOgreBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				if(UtilCollection.Chance(25))
					CastSpell(WoWDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
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
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Frost Shock";
					spell.Range = 500;
					spell.SpellID = 11957;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
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
	public class WretchOfWinterOgreBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterOgreBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
#endregion

#region WoW Ice Creature
namespace DOL.GS
{
	public class BossWretchOfWinterIce : GameEpicBoss
	{
		public BossWretchOfWinterIce() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 60;
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 126;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 73;
			MaxSpeedBase = 280;
			MeleeDamageType = EDamageType.Cold;
			Flags = eFlags.GHOST;
			WretchOfWinterIceBrain sbrain = new WretchOfWinterIceBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				if (UtilCollection.Chance(25))
					CastSpell(WoWDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
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
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Frost Shock";
					spell.Range = 500;
					spell.SpellID = 11958;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
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
	public class WretchOfWinterIceBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterIceBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
#endregion

#region WoW Big Raumarik
namespace DOL.GS
{
	public class BossWretchOfWinterRaumarik : GameEpicBoss
	{
		public BossWretchOfWinterRaumarik() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 70;
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 440;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 74;
			MaxSpeedBase = 280;
			MeleeDamageType = EDamageType.Crush;
			Flags = eFlags.GHOST;
			WretchOfWinterRaumarikBrain sbrain = new WretchOfWinterRaumarikBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				if (UtilCollection.Chance(25))
					CastSpell(WoWDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
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
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Frost Shock";
					spell.Range = 500;
					spell.SpellID = 11958;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
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
	public class WretchOfWinterRaumarikBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterRaumarikBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
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
	public class BossWretchOfWinterLich : GameEpicBoss
	{
		public BossWretchOfWinterLich() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
		public override short Strength { get => base.Strength; set => base.Strength = 400; }
		public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 441;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 74;
			MaxSpeedBase = 280;
			MeleeDamageType = EDamageType.Crush;
			Flags = eFlags.GHOST;
			WretchOfWinterLichBrain sbrain = new WretchOfWinterLichBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				if (UtilCollection.Chance(25))
					CastSpell(WoWDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
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
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Frost Shock";
					spell.Range = 500;
					spell.SpellID = 11877;
					spell.Target = "Enemy";
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
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
	public class WretchOfWinterLichBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterLichBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
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
	public class BossWretchOfWinterArachite : GameEpicBoss
	{
		public BossWretchOfWinterArachite() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 60;
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
		public override short Strength { get => base.Strength; set => base.Strength = 400; }
		public override bool AddToWorld()
		{
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			Model = 466;
			Name = "Wretch of Winter";
			Size = 250;
			Level = 75;
			MaxSpeedBase = 280;
			MeleeDamageType = EDamageType.Thrust;
			Flags = eFlags.GHOST;
			WretchOfWinterArachiteBrain sbrain = new WretchOfWinterArachiteBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (UtilCollection.Chance(25) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
			{
				if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
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
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.DamageOverTime.ToString();
					spell.DamageType = (int)EDamageType.Body;
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
	public class WretchOfWinterArachiteBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WretchOfWinterArachiteBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			base.Think();
		}
	}
}
#endregion