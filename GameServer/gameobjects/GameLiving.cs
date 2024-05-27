using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.Language;
using static DOL.GS.AttackData;

namespace DOL.GS
{
	/// <summary>
	/// This class holds all information that each
	/// living object in the world uses
	/// </summary>
	public abstract class GameLiving : GameObject
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		public static int IN_COMBAT_DURATION = 10000;

		public int UsedConcentration;

		public ConcurrentDictionary<eSpellType, Spell> ActivePulseSpells { get; } = new();

		#region Combat
		/// <summary>
		/// Holds the AttackData object of last attack
		/// </summary>
		public const string LAST_ATTACK_DATA = "LastAttackData";

		public bool isDeadOrDying = false;

		protected string m_lastInterruptMessage;
		public string LastInterruptMessage
		{
			get { return m_lastInterruptMessage; }
			set { m_lastInterruptMessage = value; }
		}

		/// <summary>
		/// Can this living accept any item regardless of tradable or droppable?
		/// </summary>
		public virtual bool CanTradeAnyItem
		{
			get { return false; }
		}

		/// <summary>
		/// Chance to fumble an attack.
		/// </summary>
		public virtual double ChanceToFumble
		{
			get
			{
				double chanceToFumble = GetModified(eProperty.FumbleChance);
				chanceToFumble *= 0.001;

				if (chanceToFumble > 0.99) chanceToFumble = 0.99;
				if (chanceToFumble < 0) chanceToFumble = 0;

				return chanceToFumble;
			}
		}

		/// <summary>
		/// Chance to be missed by an attack.
		/// </summary>
		public virtual double ChanceToBeMissed
		{
			get
			{
				double chanceToBeMissed = GetModified(eProperty.MissHit);
				chanceToBeMissed *= 0.001;

				if (chanceToBeMissed > 0.99) chanceToBeMissed = 0.99;
				if (chanceToBeMissed < 0) chanceToBeMissed = 0;

				return chanceToBeMissed;
			}
		}

		protected short m_race;
		public virtual short Race
		{
			get { return m_race; }
			set { m_race = value; }
		}

		/// <summary>
		/// say if player is stunned or not
		/// </summary>
		protected bool m_stunned;
		/// <summary>
		/// Gets the stunned flag of this living
		/// </summary>
		public bool IsStunned
		{
			get { return m_stunned; }
			set { m_stunned = value; }
		}
		/// <summary>
		/// say if player is mezzed or not
		/// </summary>
		protected bool m_mezzed;
		/// <summary>
		/// Gets the mesmerized flag of this living
		/// </summary>
		public bool IsMezzed
		{
			get
			{
				return m_mezzed;
			}
			set { m_mezzed = value; }
		}

		protected bool m_disarmed = false;
		protected long m_disarmedTime = 0;

		/// <summary>
		/// Is the living disarmed
		/// </summary>
		public bool IsDisarmed
		{
			get { return (m_disarmedTime > 0 && m_disarmedTime > CurrentRegion.Time); }
		}

		/// <summary>
		/// How long is this living disarmed for?
		/// </summary>
		public long DisarmedTime
		{
			get { return m_disarmedTime; }
			set { m_disarmedTime = value; }
		}

		protected bool m_isSilenced = false;
		protected long m_silencedTime = 0;

		/// <summary>
		/// Has this living been silenced?
		/// </summary>
		public bool IsSilenced
		{
			get { return (m_silencedTime > 0 && m_silencedTime > CurrentRegion.Time); }
		}

		/// <summary>
		/// How long is this living silenced for?
		/// </summary>
		public long SilencedTime
		{
			get { return m_silencedTime; }
			set { m_silencedTime = value; }
		}

		/// <summary>
		/// Gets the current strafing mode
		/// </summary>
		public virtual bool IsStrafing
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// Holds disease counter
		/// </summary>
		protected int m_diseasedCount;

		/// <summary>
		/// Sets disease state
		/// </summary>
		/// <param name="add">true if disease counter should be increased</param>
		public virtual void Disease(bool active)
		{
			if (active)
				Interlocked.Increment(ref m_diseasedCount);
			else
				Interlocked.Decrement(ref m_diseasedCount);
		}

		/// <summary>
		/// Gets diseased state
		/// </summary>
		public virtual bool IsDiseased => m_diseasedCount > 0;
		public virtual bool IsPoisoned => effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime);

		protected bool m_isEngaging = false;
		public virtual bool IsEngaging
		{
			get { return m_isEngaging; }
			set { m_isEngaging = value; }
		}

		/// <summary>
		/// List of objects that will gain XP after this living dies
		/// consists of GameObject -> damage(float)
		/// Damage in float because it might contain small amounts
		/// </summary>
		protected readonly HybridDictionary m_xpGainers;
		/// <summary>
		/// Holds the weaponslot to be used
		/// </summary>
		protected eActiveWeaponSlot m_activeWeaponSlot;
		/// <summary>
		/// AttackAction used for making an attack every weapon speed intervals
		/// </summary>
		//protected AttackAction m_attackAction;
		///// <summary>
		///// The objects currently attacking this living
		///// To be more exact, the objects that are in combat
		///// and have this living as target.
		///// </summary>
		//protected readonly List<GameObject> m_attackers;

        /// <summary>
        /// Returns the current active weapon slot of this living
        /// </summary>
        public virtual eActiveWeaponSlot ActiveWeaponSlot
		{
			get { return m_activeWeaponSlot; }
		}

		public object _xpGainersLock = new object();
		/// <summary>
		/// Gets a hashtable holding
		/// gameobject->float
		/// key-value pairs that will define how much
		/// XP these objects get when this n
		/// </summary>
		public virtual HybridDictionary XPGainers
		{
			get
			{
				return m_xpGainers;
			}
		}

		/// <summary>
		/// Create a pet for this living
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		public virtual GameSummonedPet CreateGamePet(INpcTemplate template)
		{
			return new GameSummonedPet(template);
		}

		/// <summary>
		/// A new pet has been summoned, do we do anything?
		/// </summary>
		/// <param name="pet"></param>
		public virtual void OnPetSummoned(GameSummonedPet pet)
		{
		}

		/// <summary>
		/// last attack tick in either pve or pvp
		/// </summary>
		public virtual long LastAttackTick
		{
			get
			{
				if (m_lastAttackTickPvE > m_lastAttackTickPvP)
					return m_lastAttackTickPvE;
				return m_lastAttackTickPvP;
			}
		}

		/// <summary>
		/// last attack tick for pve
		/// </summary>
		protected long m_lastAttackTickPvE;
		/// <summary>
		/// gets/sets gametick when this living has attacked its target in pve
		/// </summary>
		public virtual long LastAttackTickPvE
		{
			get => m_lastAttackTickPvE;
			set => m_lastAttackTickPvE = value;
		}

		/// <summary>
		/// last attack tick for pvp
		/// </summary>
		protected long m_lastAttackTickPvP;
		/// <summary>
		/// gets/sets gametick when this living has attacked its target in pvp
		/// </summary>
		public virtual long LastAttackTickPvP
		{
			get => m_lastAttackTickPvP;
			set => m_lastAttackTickPvP = value;
		}

		/// <summary>
		/// gets the last attack or attackedbyenemy tick in pvp
		/// </summary>
		public long LastCombatTickPvP
		{
			get
			{
				if (m_lastAttackTickPvP > m_lastAttackedByEnemyTickPvP)
					return m_lastAttackTickPvP;
				else return m_lastAttackedByEnemyTickPvP;
			}
		}

		/// <summary>
		/// gets the last attack or attackedbyenemy tick in pve
		/// </summary>
		public long LastCombatTickPvE
		{
			get
			{
				if (m_lastAttackTickPvE > m_lastAttackedByEnemyTickPvE)
					return m_lastAttackTickPvE;
				else return m_lastAttackedByEnemyTickPvE;
			}
		}

		/// <summary>
		/// last attacked by enemy tick in either pvp or pve
		/// </summary>
		public virtual long LastAttackedByEnemyTick
		{
			get
			{
				if (m_lastAttackedByEnemyTickPvP > m_lastAttackedByEnemyTickPvE)
					return m_lastAttackedByEnemyTickPvP;
				return m_lastAttackedByEnemyTickPvE;
			}
		}

		/// <summary>
		/// last attacked by enemy tick in pve
		/// </summary>
		protected long m_lastAttackedByEnemyTickPvE;
		/// <summary>
		/// gets/sets gametick when this living was last time attacked by an enemy in pve
		/// </summary>
		public virtual long LastAttackedByEnemyTickPvE
		{
			get => m_lastAttackedByEnemyTickPvE;
			set => m_lastAttackedByEnemyTickPvE = value;
		}

		/// <summary>
		/// last attacked by enemy tick in pve
		/// </summary>
		protected long m_lastAttackedByEnemyTickPvP;
		/// <summary>
		/// gets/sets gametick when this living was last time attacked by an enemy in pvp
		/// </summary>
		public virtual long LastAttackedByEnemyTickPvP
		{
			get => m_lastAttackedByEnemyTickPvP;
			set => m_lastAttackedByEnemyTickPvP = value;
		}

		/// <summary>
		/// Total damage RvR Value
		/// </summary>
		protected long m_damageRvRMemory;
		/// <summary>
		/// gets the DamageRvR Memory of this living (always 0 for Gameliving)
		/// </summary>
		public virtual long DamageRvRMemory
		{
			get { return 0; }
			set
			{
				m_damageRvRMemory = 0;
			}
		}

        /// <summary>
        /// Gets the current attackspeed of this living in milliseconds
        /// </summary>
        /// <returns>effective speed of the attack. average if more than one weapon.</returns>
        public virtual int AttackSpeed(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon = null)
        {
			return attackComponent.AttackSpeed(mainWeapon, leftWeapon);
        }

        /// <summary>
        /// Minimum reduction possible to spell casting speed (CastTime * CastingSpeedCap)
        /// </summary>
        public virtual double CastingSpeedReductionCap
		{
			get { return 0.4; }
		}

		/// <summary>
		/// Minimum casting speed allowed, in ticks (milliseconds)
		/// </summary>
		public virtual int MinimumCastingSpeed
		{
			get { return 500; }
		}

		/// <summary>
		/// Can this living cast while attacking?
		/// </summary>
		public virtual bool CanCastWhileAttacking()
		{
			return false;
		}

		/// <summary>
		/// Calculate how fast this living can cast a given spell
		/// </summary>
		public virtual int CalculateCastingTime(SpellLine line, Spell spell)
		{
			int ticks = spell.CastTime;

			if (spell.InstrumentRequirement != 0 ||
			    line.KeyName == GlobalSpellsLines.Item_Spells ||
			    line.KeyName.StartsWith(GlobalSpellsLines.Champion_Lines_StartWith))
			{
				return ticks;
			}


			double percent = DexterityCastTimeReduction;

			ticks = (int)(ticks * Math.Max(CastingSpeedReductionCap, percent));
			if (ticks < MinimumCastingSpeed)
				ticks = MinimumCastingSpeed;

			return ticks;
		}

		/// <summary>
		/// The casting time reduction based on dexterity bonus.
		/// http://daoc.nisrv.com/modules.php?name=DD_DMG_Calculator
		/// Q: Would you please give more detail as to how dex affects a caster?
		/// For instance, I understand that when I have my dex maxed I will cast 25% faster.
		/// How does this work incrementally? And will a lurikeen be able to cast faster in the end than another race?
		/// A: From a dex of 50 to a dex of 250, the formula lets you cast 1% faster for each ten points.
		/// From a dex of 250 to the maximum possible (which as you know depends on your starting total),
		/// your speed increases 1% for every twenty points.
		/// </summary>
		public virtual double DexterityCastTimeReduction
		{
			get
			{
				int dex = GetModified(eProperty.Dexterity);
				if (dex < 60) return 1.0;
				else if (dex < 250) return 1.0 - (dex - 60) * 0.15 * 0.01;
				else return 1.0 - ((dex - 60) * 0.15 + (dex - 250) * 0.05) * 0.01;
			}
		}

        public virtual int MeleeAttackRange => 200;

        /// <summary>
        /// calculates weapon stat
        /// </summary>
        /// <param name="weapon"></param>
        /// <returns></returns>
        public virtual int GetWeaponStat(DbInventoryItem weapon)
		{
			return GetModified(eProperty.Strength);
		}

		/// <summary>
		/// calculate item armor factor influenced by quality, con and duration
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public virtual double GetArmorAF(eArmorSlot slot)
		{
			return GetModified(eProperty.ArmorFactor);
		}

		/// <summary>
		/// Calculates armor absorb level
		/// </summary>
		public virtual double GetArmorAbsorb(eArmorSlot slot)
		{
			double baseAbsorb = 0;

			if (this is NecromancerPet nPet)
			{
				if (nPet.Owner.Level == 50)
					baseAbsorb = 0.5;
				else if (nPet.Owner.Level >= 40)
					baseAbsorb = 0.40;
				else if (nPet.Owner.Level >= 30)
					baseAbsorb = 0.27;
				else if (nPet.Owner.Level >= 20)
					baseAbsorb = 0.19;
				else if (nPet.Owner.Level >= 10)
					baseAbsorb = 0.10;
			}
			else
			{
				if (Level >= 30)
					baseAbsorb = 0.27;
				else if (Level >= 20)
					baseAbsorb = 0.19;
				else if (Level >= 10)
					baseAbsorb = 0.10;
			}

			double absorbBonus = GetModified(eProperty.ArmorAbsorption) / 100.0;
			double absorptionFromConstitution = StatCalculator.CalculateBuffContributionToAbsorbOrResist(this, eProperty.Constitution) / 4;
			double absorptionFromDexterity = StatCalculator.CalculateBuffContributionToAbsorbOrResist(this, eProperty.Dexterity) / 4;
			double absorb = 1 - (1 - baseAbsorb) * (1 - absorbBonus) * (1 - absorptionFromConstitution) * (1 - absorptionFromDexterity);
			return Math.Clamp(absorb, 0, 1);
		}

		/// <summary>
		/// Gets the weaponskill of weapon
		/// </summary>
		public virtual double GetWeaponSkill(DbInventoryItem weapon)
		{
			// Needs to be overridden.
			return 0;
		}

		private (DbInventoryItem item, eActiveWeaponSlot slot, long time) _cachedActiveWeapon;

        /// <summary>
        /// Returns the currently active weapon, null=natural
        /// </summary>
        public virtual DbInventoryItem ActiveWeapon
        {
            get
            {
                if (Inventory == null)
                    return null;

                // We cache the weapon since 'ActiveWeapon' can be called multiple times per tick and 'GameInventory.GetItem' is potentially expensive.
                if (_cachedActiveWeapon.item != null && _cachedActiveWeapon.slot == ActiveWeaponSlot && _cachedActiveWeapon.time >= GameLoop.GameLoopTime)
                    return _cachedActiveWeapon.item;

                _cachedActiveWeapon.time = GameLoop.GameLoopTime;
                _cachedActiveWeapon.slot = ActiveWeaponSlot;

                switch (ActiveWeaponSlot)
                {
                    case eActiveWeaponSlot.Standard:
                    {
                        _cachedActiveWeapon.item = Inventory.GetItem(eInventorySlot.RightHandWeapon);
                        break;
                    }
                    case eActiveWeaponSlot.TwoHanded:
                    {
                        _cachedActiveWeapon.item = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
                        break;
                    }
                    case eActiveWeaponSlot.Distance:
                    {
                        _cachedActiveWeapon.item = Inventory.GetItem(eInventorySlot.DistanceWeapon);
                        break;
                    }
                    default:
                    {
                        _cachedActiveWeapon.item = null;
                        break;
                    }
                }

                return _cachedActiveWeapon.item;
            }
        }

        /// <summary>
        /// Returns the chance for a critical hit with a spell
        /// </summary>
        public virtual int SpellCriticalChance
		{
			get { return GetModified(eProperty.CriticalSpellHitChance); }
			set { }
		}

		/// <summary>
		/// Returns the chance for a critical hit with a spell
		/// </summary>
		public virtual int DotCriticalChance
		{
			get { return GetModified(eProperty.CriticalDotHitChance); }
			set { }
		}

		/// <summary>
		/// Gets the attack-state of this living
		/// </summary>
		public virtual bool AttackState { get; set; }

        /// <summary>
        /// Whether or not the living can be attacked.
        /// </summary>
        public override bool IsAttackable
		{
			get
			{
				return (IsAlive &&
				        !IsStealthed &&
				        EffectList.GetOfType<NecromancerShadeEffect>() == null &&
				        ObjectState == GameObject.eObjectState.Active);
			}
		}

        /// <summary>
        /// Whether the living is actually attacking something.
        /// </summary>
        public virtual bool IsAttacking
        {
            get { return attackComponent.AttackState; }
        }

        /// <summary>
        /// Gets the effective AF of this living
        /// </summary>
        public virtual int EffectiveOverallAF
		{
			get { return 0; }
		}

		public virtual int WeaponSpecLevel(eObjectType objectType, int slotPosition)
		{
			return 0;
		}

		/// <summary>
		/// determines the spec level for current AttackWeapon
		/// </summary>
		public virtual int WeaponSpecLevel(DbInventoryItem weapon)
		{
			return 0;
		}

		/// <summary>
		/// Gets the weapondamage of currently used weapon
		/// </summary>
		/// <param name="weapon">the weapon used for attack</param>
		public virtual double WeaponDamage(DbInventoryItem weapon)
		{
			return 0;
		}

		/// <summary>
		/// Whether this living is crowd controlled.
		/// </summary>
		public virtual bool IsCrowdControlled
		{
			get
			{
				return (IsStunned || IsMezzed);
			}
		}

		/// <summary>
		/// Whether this living can actually do anything.
		/// </summary>
		public virtual bool IsIncapacitated
		{
			get
			{
				return (ObjectState != eObjectState.Active || !IsAlive || IsStunned || IsMezzed);
			}
		}

		/// <summary>
		/// returns if this living is alive
		/// </summary>
		public virtual bool IsAlive
		{
			get { return Health > 0; }
		}

		/// <summary>
		/// True if living is low on health, else false.
		/// </summary>
		public virtual bool IsLowHealth
		{
			get
			{
				return (Health < 0.1 * MaxHealth);
			}
		}

		protected bool m_isMuted = false;
		/// <summary>
		/// returns if this living is muted
		/// </summary>
		public virtual bool IsMuted
		{
			get { return m_isMuted; }
			set
			{
				m_isMuted = value;
			}
		}

		/// <summary>
		/// Check this flag to see if this living is involved in combat
		/// </summary>
		public virtual bool InCombat => InCombatPvE || InCombatPvP;

		/// <summary>
		/// Check this flag to see if this living has been involved in combat in the given milliseconds
		/// </summary>
		public virtual bool InCombatInLast(int milliseconds)
		{
			return InCombatPvEInLast(milliseconds) || InCombatPvPInLast(milliseconds);
		}

		/// <summary>
		/// checks if the living is involved in pvp combat
		/// </summary>
		public virtual bool InCombatPvP => LastCombatTickPvP > 0 && LastCombatTickPvP + IN_COMBAT_DURATION >= GameLoop.GameLoopTime;

		/// <summary>
		/// checks if the living is involved in pvp combat in the given milliseconds
		/// </summary>
		public virtual bool InCombatPvPInLast(int milliseconds)
		{
			return LastCombatTickPvP > 0 && LastCombatTickPvP + milliseconds >= GameLoop.GameLoopTime;
		}

		/// <summary>
		/// checks if the living is involved in pve combat
		/// </summary>
		public virtual bool InCombatPvE => LastCombatTickPvE > 0 && LastCombatTickPvE + IN_COMBAT_DURATION >= GameLoop.GameLoopTime;

		/// <summary>
		/// checks if the living is involved in pve combat in the given milliseconds
		/// </summary>
		public virtual bool InCombatPvEInLast(int milliseconds)
		{
			return LastCombatTickPvE > 0 && LastCombatTickPvE + milliseconds >= GameLoop.GameLoopTime;
		}

		/// <summary>
		/// Returns the amount of experience this living is worth
		/// </summary>
		public virtual long ExperienceValue
		{
			get
			{
				return GetExperienceValueForLevel(Level);
			}
		}

		/// <summary>
		/// Realm point value of this living
		/// </summary>
		public virtual int RealmPointsValue
		{
			get { return 0; }
		}

		/// <summary>
		/// Bounty point value of this living
		/// </summary>
		public virtual int BountyPointsValue
		{
			get { return 0; }
		}

		/// <summary>
		/// Money value of this living
		/// </summary>
		public virtual long MoneyValue
		{
			get { return 0; }
		}

		/// <summary>
		/// How much over the XP cap can this living reward.
		/// 1.0 = none
		/// 2.0 = twice cap
		/// etc.
		/// </summary>
		public virtual double ExceedXPCapAmount
		{
			get { return 1.0; }
		}

		#region XP array

		/// <summary>
		/// Holds pre calculated experience values of the living for special levels
		/// </summary>
		public static readonly long[] XPForLiving =
		{
			// noret: first 52 are from exp table, think mythic has changed some values
			// cause they don't fit the formula; rest are calculated.
			// with this formula group with 8 lv50 players should hit cap on lv67 mobs what looks about correct
			// http://www.daocweave.com/daoc/general/experience_table.htm
			5,					// xp for level 0
			10,					// xp for level 1
			20,					// xp for level 2
			40,					// xp for level 3
			80,					// xp for level 4
			160,				// xp for level 5
			320,				// xp for level 6
			640,				// xp for level 7
			1280,				// xp for level 8
			2560,				// xp for level 9
			5120,				// xp for level 10
			7240,				// xp for level 11
			10240,				// xp for level 12
			14480,				// xp for level 13
			20480,				// xp for level 14
			28980,				// xp for level 15
			40960,				// xp for level 16
			57930,				// xp for level 17
			81920,				// xp for level 18
			115850,				// xp for level 19
			163840,				// xp for level 20
			206435,				// xp for level 21
			231705,				// xp for level 22
			327680,				// xp for level 23
			412850,				// xp for level 24
			520160,				// xp for level 25
			655360,				// xp for level 26
			825702,				// xp for level 27
			1040319,			// xp for level 28
			1310720,			// xp for level 29
			1651404,			// xp for level 30
			2080638,			// xp for level 31
			2621440,			// xp for level 32
			3302807,			// xp for level 33
			4161277,			// xp for level 34
			5242880,			// xp for level 35
			6022488,			// xp for level 36
			6918022,			// xp for level 37
			7946720,			// xp for level 38
			9128384,			// xp for level 39
			10485760,			// xp for level 40
			12044975,			// xp for level 41
			13836043,			// xp for level 42
			15893440,			// xp for level 43
			18258769,			// xp for level 44
			20971520,			// xp for level 45
			24089951,			// xp for level 46
			27672087,			// xp for level 47
			31625241,			// xp for level 48; sshot505.tga
			36513537,			// xp for level 49
			41943040,			// xp for level 50
			48179911,			// xp for level 51
			52428800,			// xp for level 52
			63573760,			// xp for level 53
			73027074,			// xp for level 54
			83886080,			// xp for level 55
			96359802,			// xp for level 56
			110688346,			// xp for level 57
			127147521,			// xp for level 58
			146054148,			// xp for level 59
			167772160,			// xp for level 60
			192719604,			// xp for level 61
			221376692,			// xp for level 62
			254295042,			// xp for level 63
			292108296,			// xp for level 64
			335544320,			// xp for level 65
			385439208,			// xp for level 66
			442753384,			// xp for level 67
			508590084,			// xp for level 68
			584216593,			// xp for level 69
			671088640,			// xp for level 70
			770878416,			// xp for level 71
			885506769,			// xp for level 72
			1017180169,			// xp for level 73
			1168433187,			// xp for level 74
			1342177280,			// xp for level 75
			1541756833,			// xp for level 76
			1771013538,			// xp for level 77
			2034360338,			// xp for level 78
			2336866374,			// xp for level 79
			2684354560,			// xp for level 80
			3083513667,			// xp for level 81
			3542027077,			// xp for level 82
			4068720676,			// xp for level 83
			4673732748,			// xp for level 84
			5368709120,			// xp for level 85
			6167027334,			// xp for level 86
			7084054154,			// xp for level 87
			8137441353,			// xp for level 88
			9347465497,			// xp for level 89
			10737418240,		// xp for level 90
			12334054669,		// xp for level 91
			14168108308,		// xp for level 92
			16274882707,		// xp for level 93
			18694930994,		// xp for level 94
			21474836480,		// xp for level 95
			24668109338,		// xp for level 96
			28336216617,		// xp for level 97
			32549765415,		// xp for level 98
			37389861988,		// xp for level 99
			42949672960			// xp for level 100
		};

		#endregion

		/// <summary>
		/// Checks whether object is grey con to this living
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public virtual bool IsObjectGreyCon(GameObject obj)
		{
			return (ConColor) GetConLevel(obj) <= ConColor.GREY;
		}

		/// <summary>
		/// Calculates the experience value of this living for special levels
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		public virtual long GetExperienceValueForLevel(int level)
		{
			return GameServer.ServerRules.GetExperienceForLiving(level);
		}

		/// <summary>
		/// Gets/sets the targetObject's visibility
		/// </summary>
		public virtual bool TargetInView
		{
			get => true;
			set { }
		}

		public virtual int TargetInViewAlwaysTrueMinRange => 0;

		/// <summary>
		/// Gets or sets the GroundTargetObject's visibility
		/// </summary>
		public virtual bool GroundTargetInView
		{
			get => true;
			set { }
		}

		/// <summary>
		/// Starts the interrupt timer on this living.
		/// </summary>
		public virtual void StartInterruptTimer(int duration, eAttackType attackType, GameLiving attacker)
		{
			if (attacker == this)
			{
				SelfInterruptTime = GameLoop.GameLoopTime + duration;
				return;
			}

			if (!Util.Chance(100 + GetConLevel(attacker) * 15))
				return;

			// Don't replace the current interrupt with a shorter one.
			// Otherwise a slow melee hit's interrupt duration will be made shorter by a proc for example.
			InterruptTime = Math.Max(InterruptTime, GameLoop.GameLoopTime + duration);
			LastInterrupter = attacker;

			if (castingComponent?.SpellHandler != null)
				castingComponent.SpellHandler.CasterIsAttacked(attacker);
			else if (ActiveWeaponSlot == eActiveWeaponSlot.Distance)
			{
				if (attackComponent.AttackState)
					CheckRangedAttackInterrupt(attacker, attackType);
				else if (effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
				{
					AtlasOF_VolleyECSEffect volley = (AtlasOF_VolleyECSEffect) EffectListService.GetEffectOnTarget(this, eEffect.Volley);
					volley?.OnAttacked();
				}
			}
		}

		public virtual bool StartInterruptTimerOnItselfOnMeleeAttack()
		{
			return true;
		}

		public GameObject LastInterrupter { get; private set; }
		public long InterruptTime { get; private set; }
		public long SelfInterruptTime { get; private set; }
		public long InterruptRemainingDuration => !IsBeingInterrupted ? 0 : Math.Max(InterruptTime, SelfInterruptTime) - GameLoop.GameLoopTime;
		public virtual bool IsBeingInterrupted => IsBeingInterruptedIgnoreSelfInterrupt || SelfInterruptTime > GameLoop.GameLoopTime;
		public virtual bool IsBeingInterruptedIgnoreSelfInterrupt => InterruptTime > GameLoop.GameLoopTime;

		/// <summary>
		/// How long does an interrupt last?
		/// </summary>
		public virtual int SpellInterruptDuration => Properties.SPELL_INTERRUPT_DURATION;

		/// <summary>
		/// Additional interrupt time if interrupted again
		/// </summary>
		public virtual int SpellInterruptRecastAgain => Properties.SPELL_INTERRUPT_AGAIN;

		protected virtual bool CheckRangedAttackInterrupt(GameLiving attacker, eAttackType attackType)
		{
			if (rangeAttackComponent.RangedAttackType == eRangedAttackType.SureShot)
			{
				if (attackType is not eAttackType.MeleeOneHand
					and not eAttackType.MeleeTwoHand
					and not eAttackType.MeleeDualWield)
					return false;
			}

			long rangeAttackHoldStart = rangeAttackComponent.AttackStartTime;

			if (rangeAttackHoldStart > 0)
			{
				long elapsedTime = GameLoop.GameLoopTime - rangeAttackHoldStart;
				long halfwayPoint = attackComponent.AttackSpeed(ActiveWeapon) / 2;
				
				if (rangeAttackComponent.RangedAttackState is not eRangedAttackState.ReadyToFire and not eRangedAttackState.None && elapsedTime > halfwayPoint)
					return false;
			}

			attackComponent.StopAttack();
			return true;
		}

		/// <summary>
		/// Does this living allow procs to be cast on it?
		/// </summary>
		/// <param name="ad"></param>
		/// <param name="weapon"></param>
		/// <returns></returns>
		public virtual bool AllowWeaponMagicalEffect(AttackData ad, DbInventoryItem weapon, Spell weaponSpell)
		{
			if (weapon.Flags == 10) //Itemtemplates with "Flags" set to 10 will not proc on living (ex. Bruiser)
				return false;
			else return true;
		}

		/// <summary>
		/// Check if we can make a proc on a weapon go off.  Weapon Procs
		/// </summary>
		/// <param name="ad"></param>
		/// <param name="weapon"></param>
		public virtual void CheckWeaponMagicalEffect(AttackData ad, DbInventoryItem weapon)
		{
			if (weapon == null || (ad.AttackResult != eAttackResult.HitStyle && ad.AttackResult != eAttackResult.HitUnstyled))
				return;

			// Proc chance is 2.5% per SPD, i.e. 10% for a 3.5 SPD weapon. - Tolakram, changed average speed to 3.5

			int procChance = (int)Math.Ceiling(((weapon.ProcChance > 0 ? weapon.ProcChance : 10) * (weapon.SPD_ABS / 35.0)));

            //Error protection and log for Item Proc's
            Spell procSpell = null;
            Spell procSpell1 = null;
            if (this is GamePlayer)
            {
                SpellLine line = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
                if (line != null)
                {
                    procSpell = SkillBase.FindSpell(weapon.ProcSpellID, line);
                    procSpell1 = SkillBase.FindSpell(weapon.ProcSpellID1, line);

                    if (procSpell == null && weapon.ProcSpellID != 0)
                    {
                        log.ErrorFormat("- Proc ID {0} Not Found on item: {1} ", weapon.ProcSpellID, weapon.Template.Id_nb);
                    }
                    if (procSpell1 == null && weapon.ProcSpellID1 != 0)
                    {
                        log.ErrorFormat("- Proc1 ID {0} Not Found on item: {1} ", weapon.ProcSpellID1, weapon.Template.Id_nb);
                    }
                }
            }

            // Proc #1
            if (procSpell != null && Util.Chance(procChance))

                StartWeaponMagicalEffect(weapon, ad, SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects), weapon.ProcSpellID, false);

            // Proc #2
            if (procSpell1 != null && Util.Chance(procChance))

                StartWeaponMagicalEffect(weapon, ad, SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects), weapon.ProcSpellID1, false);

			// Poison

			if (weapon.PoisonSpellID != 0)
			{
				if (ad.Target.EffectList.GetOfType<RemedyEffect>() != null)
				{
					if (this is GamePlayer)
						(this as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((this as GamePlayer).Client.Account.Language, "GameLiving.CheckWeaponMagicalEffect.Protected"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
					return;
				}

				StartWeaponMagicalEffect(weapon, ad, SkillBase.GetSpellLine(GlobalSpellsLines.Mundane_Poisons), weapon.PoisonSpellID, true);

				// Spymaster Enduring Poison

				if (ad.Attacker is GamePlayer)
				{
					GamePlayer PlayerAttacker = ad.Attacker as GamePlayer;
					if (PlayerAttacker.GetSpellLine("Spymaster") != null)
						if (Util.ChanceDouble((double)(15 * 0.0001))) return;
				}
				weapon.PoisonCharges--;
				if (weapon.PoisonCharges <= 0) { weapon.PoisonMaxCharges = 0; weapon.PoisonSpellID = 0; }
			}
		}

		/// <summary>
		/// Make a proc or poison on the weapon go off.
		/// Will assume spell is in GlobalSpellsLines.Item_Effects even if it's not and use the weapons LevelRequirement
		/// Item_Effects must be used here because various spell handlers recognize this line to alter variance and other spell parameters
		/// </summary>
		protected virtual void StartWeaponMagicalEffect(DbInventoryItem weapon, AttackData ad, SpellLine spellLine, int spellID, bool ignoreLevel)
		{
			if (weapon == null)
				return;

			if (spellLine == null)
			{
				spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
			}

			if (spellLine != null && ad != null && weapon != null)
			{
				Spell procSpell = SkillBase.FindSpell(spellID, spellLine);

				if (procSpell != null)
				{
					// check with target to see if it allows procs to cast on it (primarily used for keep components)
					if (ad.Target.AllowWeaponMagicalEffect(ad, weapon, procSpell))
					{
						if (ignoreLevel == false)
						{
							int requiredLevel = weapon.Template.LevelRequirement > 0 ? weapon.Template.LevelRequirement : Math.Min(50, weapon.Level);

							if (requiredLevel > Level)
							{
								if (this is GamePlayer)
								{
									(this as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((this as GamePlayer).Client.Account.Language, "GameLiving.StartWeaponMagicalEffect.NotPowerful"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
								}
								return;
							}
						}

						ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(ad.Attacker, procSpell, spellLine);

						if (spellHandler != null)
						{
							bool rangeCheck = spellHandler.Spell.Target == eSpellTarget.ENEMY && spellHandler.Spell.Range > 0;

							if (!rangeCheck || ad.Attacker.IsWithinRadius(ad.Target, spellHandler.CalculateSpellRange()))
								spellHandler.StartSpell(ad.Target, weapon);
						}
					}
				}
			}
		}

		/// <summary>
		/// When a ranged attack is finished this is called in order to check LOS for next attack
		/// </summary>
		public virtual void RangedAttackFinished()
		{
		}

		/// <summary>
		/// Remove engage effect on this living if it is present.
		/// </summary>
		public void CancelEngageEffect()
		{
			EngageECSGameEffect effect = (EngageECSGameEffect)EffectListService.GetEffectOnTarget(this, eEffect.Engage);

			if (effect != null)
				effect.Cancel(false, false);

			IsEngaging = false;
		}

		/// <summary>
		/// Our target is dead or we don't have a target
		/// </summary>
		public virtual void OnTargetDeadOrNoTarget()
		{
			if (ActiveWeaponSlot != eActiveWeaponSlot.Distance)
			{
				attackComponent.StopAttack();
			}

			if (this is GameNPC && ActiveWeaponSlot != eActiveWeaponSlot.Distance &&
			    ((GameNPC)this).Inventory != null &&
			    ((GameNPC)this).Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
			{
				SwitchWeapon(eActiveWeaponSlot.Distance);
			}
		}

		public virtual double TryEvade(AttackData ad, AttackData lastAD, int attackerConLevel, int attackerCount)
		{
			// 1. A: It isn't possible to give a simple answer. The formula includes such elements
			// as your level, your target's level, your level of evade, your QUI, your DEX, your
			// buffs to QUI and DEX, the number of people attacking you, your target's weapon
			// level, your target's spec in the weapon he is wielding, the kind of attack (DW,
			// ranged, etc), attack radius, angle of attack, the style you used most recently,
			// target's offensive RA, debuffs, and a few others. (The type of weapon - large, 1H,
			// etc - doesn't matter.) ...."

			double evadeChance = 0;
			GamePlayer player = this as GamePlayer;
			ECSGameEffect evadeBuff = EffectListService.GetEffectOnTarget(this, eEffect.SavageBuff, eSpellType.SavageEvadeBuff);

			if (player != null)
			{
				if (player.HasAbility(Abilities.Advanced_Evade) ||
					player.HasAbility(Abilities.Enhanced_Evade) ||
					player.EffectList.GetOfType<CombatAwarenessEffect>() != null ||
					player.EffectList.GetOfType<RuneOfUtterAgilityEffect>() != null)
					evadeChance = GetModified(eProperty.EvadeChance);
				else if (IsObjectInFront(ad.Attacker, 180) && (evadeBuff != null || player.HasAbility(Abilities.Evade)))
					evadeChance = Math.Max(GetModified(eProperty.EvadeChance), 0);
			}
			else if (this is GameNPC && IsObjectInFront(ad.Attacker, 180))
				evadeChance = GetModified(eProperty.EvadeChance);

			if (evadeChance > 0 && !ad.Target.IsStunned && !ad.Target.IsSitting)
			{
				if (attackerCount > 1)
					evadeChance -= (attackerCount - 1) * 0.03;

				evadeChance *= 0.001;
				evadeChance += 0.01 * attackerConLevel;

				// Kelgor's Claw 15% evade.
				if (lastAD != null && lastAD.Style != null && lastAD.Style.ID == 380)
					evadeChance += 15 * 0.01;

				// Reduce chance by attacker's defense penetration.
				evadeChance *= 1 - GetAttackerDefensePenetration(ad.Attacker, ad.Weapon) / 100.0;

				if (ad.AttackType == eAttackType.Ranged)
					evadeChance /= 5.0;

				if (evadeChance < 0.01)
					evadeChance = 0.01;
				else if (evadeChance > Properties.EVADE_CAP && ad.Attacker is GamePlayer && ad.Target is GamePlayer)
					evadeChance = Properties.EVADE_CAP; // 50% evade cap RvR only. http://www.camelotherald.com/more/664.shtml

				if (evadeChance > 0.995)
					evadeChance = 0.995;
				
				if (ad.AttackType == eAttackType.MeleeDualWield)
					evadeChance = Math.Max(evadeChance * 0.5, 0.01);
			
				if (IsObjectInFront(ad.Attacker, 180) &&
					(evadeBuff != null || (player != null && player.HasAbility(Abilities.Evade))) &&
					evadeChance < 0.05 &&
					ad.AttackType != eAttackType.Ranged)
				{
					// If player has a hard evade source, 5% minimum evade chance.
					evadeChance = 0.05;
				}
			}

			// Infiltrator RR5.
			if (ad.Attacker is GamePlayer playerAttacker)
			{
				OverwhelmEffect Overwhelm = playerAttacker.EffectList.GetOfType<OverwhelmEffect>();

				if (Overwhelm != null)
					evadeChance = Math.Max(evadeChance - OverwhelmAbility.BONUS, 0);
			}

			return evadeChance;
		}

		public virtual double TryParry(AttackData ad, AttackData lastAD, int attackerConLevel, int attackerCount)
		{
			//1.  Dual wielding does not grant more chances to parry than a single weapon.  Grab Bag 9/12/03
			//2.  There is no hard cap on ability to Parry.  Grab Bag 8/13/02
			//3.  Your chances of doing so are best when you are solo, trying to block or parry a style from someone who is also solo. The chances of doing so decrease with grouped, simultaneous attackers.  Grab Bag 7/19/02
			//4.  The parry chance is divided up amongst the attackers, such that if you had a 50% chance to parry normally, and were under attack by two targets, you would get a 25% chance to parry one, and a 25% chance to parry the other. So, the more people or monsters attacking you, the lower your chances to parry any one attacker. -   Grab Bag 11/05/04
			//Your chance to parry is affected by the number of attackers, the size of the weapon youre using, and your spec in parry.

			//Parry % = (5% + 0.5% * Parry) / # of Attackers
			//Parry: (((Dex*2)-100)/40)+(Parry/2)+(Mastery of P*3)+5. < Possible relation to buffs
			//So, if you have parry of 20 you will have a chance of parrying 15% if there is one attacker. If you have parry of 20 you will have a chance of parrying 7.5%, if there are two attackers.
			//From Grab Bag: "Dual wielders throw an extra wrinkle in. You have half the chance of shield blocking a dual wielder as you do a player using only one weapon. Your chance to parry is halved if you are facing a two handed weapon, as opposed to a one handed weapon."
			//So, when facing a 2H weapon, you may see a penalty to your evade.

			//http://www.camelotherald.com/more/453.php

			//Also, before this comparison happens, the game looks to see if your opponent is in your forward arc  to determine that arc, make a 120 degree angle, and put yourself at the point.

			double parryChance = 0;

			if (ad.IsMeleeAttack)
			{
				BladeBarrierEffect BladeBarrier = null;
				ECSGameEffect parryBuff = EffectListService.GetEffectOnTarget(this, eEffect.SavageBuff, eSpellType.SavageParryBuff);

				if (this is GamePlayer player)
				{
					// BladeBarrier overwrites all parrying, 90% chance to parry any attack, does not consider other bonuses to parry.
					// They still need an active weapon to parry with BladeBarrier
					BladeBarrier = player.EffectList.GetOfType<BladeBarrierEffect>();
					
					if (BladeBarrier != null && ActiveWeapon != null)
						parryChance = 0.90;
					else if (IsObjectInFront(ad.Attacker, 120))
					{
						if ((player.HasSpecialization(Specs.Parry) || parryBuff != null) && ActiveWeapon != null &&
							ActiveWeapon.Object_Type != (int)eObjectType.RecurvedBow &&
							ActiveWeapon.Object_Type != (int)eObjectType.Longbow &&
							ActiveWeapon.Object_Type != (int)eObjectType.CompositeBow &&
							ActiveWeapon.Object_Type != (int)eObjectType.Crossbow &&
							ActiveWeapon.Object_Type != (int)eObjectType.Fired)
						{
							parryChance = GetModified( eProperty.ParryChance );
						}
					}
				}
				else if (this is GameNPC && IsObjectInFront(ad.Attacker, 120))
					parryChance = GetModified(eProperty.ParryChance);

				if (BladeBarrier != null && !ad.Target.IsStunned && !ad.Target.IsSitting)
					return parryChance;

				if (parryChance > 0 && !ad.Target.IsStunned && !ad.Target.IsSitting)
				{
					if (attackerCount > 1)
						parryChance /= attackerCount / 2;

					parryChance *= 0.001;
					parryChance += 0.05 * attackerConLevel;

					// Tribal Wrath 25% evade.
					if (lastAD != null && lastAD.Style != null && lastAD.Style.ID == 381)
						parryChance += 25 * 0.01;

					// Reduce chance by attacker's defense penetration.
					parryChance *= 1 - GetAttackerDefensePenetration(ad.Attacker, ad.Weapon) / 100.0;

					if (parryChance < 0.01)
						parryChance = 0.01;
					else if (parryChance > Properties.PARRY_CAP && ad.Attacker is GamePlayer && ad.Target is GamePlayer)
						parryChance = Properties.PARRY_CAP;

					if (parryChance > 0.995)
						parryChance = 0.995;
				}
			}

			if (ad.AttackType == eAttackType.MeleeTwoHand)
				parryChance = Math.Max(parryChance * 0.5, 0);

			// Infiltrator RR5.
			if (ad.Attacker is GamePlayer attackerPlayer)
			{
				OverwhelmEffect Overwhelm = attackerPlayer.EffectList.GetOfType<OverwhelmEffect>();

				if (Overwhelm != null)
					parryChance = Math.Max(parryChance - OverwhelmAbility.BONUS, 0);
			}

			return parryChance;
		}

		public virtual double TryBlock(AttackData ad, int attackerConLevel, int attackerCount)
		{
			//1.Quality does not affect the chance to block at this time.  Grab Bag 3/7/03
			//2.Condition and enchantment increases the chance to block  Grab Bag 2/27/03
			//3.There is currently no hard cap on chance to block  Grab Bag 2/27/03 and 8/16/02
			//4.Dual Wielders (enemy) decrease the chance to block  Grab Bag 10/18/02
			//5.Block formula: Shield = base 5% + .5% per spec point. Then modified by dex (.1% per point of dex above 60 and below 300?). Further modified by condition, bonus and shield level
			//8.The shields size only makes a difference when multiple things are attacking you  a small shield can block one attacker, a medium shield can block two at once, and a large shield can block three.  Grab Bag 4/4/03
			//Your chance to block is affected by the number of attackers, the size of the shield youre using, and your spec in block.
			//Shield% = (5% + 0.5% * Shield)
			//Small Shield = 1 attacker
			//Medium Shield = 2 attacker
			//Large Shield = 3 attacker
			//Each attacker above these numbers will reduce your chance to block.
			//From Grab Bag: "Dual wielders throw an extra wrinkle in. You have half the chance of shield blocking a dual wielder as you do a player using only one weapon. Your chance to parry is halved if you are facing a two handed weapon, as opposed to a one handed weapon."
			//Block: (((Dex*2)-100)/40)+(Shield/2)+(Mastery of B*3)+5. < Possible relation to buffs

			//http://www.camelotherald.com/more/453.php

			//Also, before this comparison happens, the game looks to see if your opponent is in your forward arc  to determine that arc, make a 120 degree angle, and put yourself at the point.
			//your friend is most likely using a player crafted shield. The quality of the player crafted item will make a significant difference  try it and see.

			double blockChance = 0;
			DbInventoryItem leftHand = Inventory?.GetItem(eInventorySlot.LeftHandWeapon);

			if (leftHand != null && leftHand.Object_Type != (int) eObjectType.Shield)
				leftHand = null;

			GamePlayer player = this as GamePlayer;

			if (IsObjectInFront(ad.Attacker, 120) && !ad.Target.IsStunned && !ad.Target.IsSitting)
			{
				if (player != null)
				{
					if (player.HasAbility(Abilities.Shield) && leftHand != null && (player.ActiveWeapon == null || player.ActiveWeapon.Item_Type == Slot.RIGHTHAND || player.ActiveWeapon.Item_Type == Slot.LEFTHAND))
						blockChance = GetModified(eProperty.BlockChance) * (leftHand.Quality * 0.01) * (leftHand.Condition / (double) leftHand.MaxCondition);
				}
				else
					blockChance = GetModified(eProperty.BlockChance);
			}

			if (blockChance > 0)
			{
				// Reduce block chance if the shield used is too small.
				int shieldSize = 1;

				if (leftHand != null)
				{
					shieldSize = Math.Max(leftHand.Type_Damage, 1);

					if (attackerCount > shieldSize)
						blockChance *= shieldSize / (double) attackerCount;
				}

				blockChance *= 0.001;
				blockChance += attackerConLevel * 0.05;

				// Reduce chance by attacker's defense penetration.
				blockChance *= 1 - GetAttackerDefensePenetration(ad.Attacker, ad.Weapon) / 100;

				if (blockChance < 0.01)
					blockChance = 0.01;
				else if (blockChance > Properties.BLOCK_CAP && ad.Attacker is GamePlayer && ad.Target is GamePlayer)
					blockChance = Properties.BLOCK_CAP;

				// Possibly intended to be applied in RvR only.
				if (shieldSize == 1 && blockChance > 0.8)
					blockChance = 0.8;
				else if (shieldSize == 2 && blockChance > 0.9)
					blockChance = 0.9;
				else if (shieldSize == 3 && blockChance > 0.99)
					blockChance = 0.99;

				if (IsEngaging)
				{
					EngageECSGameEffect engage = (EngageECSGameEffect) EffectListService.GetEffectOnTarget(this, eEffect.Engage);

					if (engage != null && attackComponent.AttackState && engage.EngageTarget == ad.Attacker)
					{
						if (engage.EngageTarget.LastAttackedByEnemyTick > GameLoop.GameLoopTime - EngageAbilityHandler.ENGAGE_ATTACK_DELAY_TICK)
							player?.Out.SendMessage(engage.EngageTarget.GetName(0, true) + " has been attacked recently and you are unable to engage.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						else if (Endurance < EngageAbilityHandler.ENGAGE_ENDURANCE_COST)
							engage.Cancel(false, true);
						else
						{
							Endurance -= EngageAbilityHandler.ENGAGE_ENDURANCE_COST;
							player?.Out.SendMessage("You concentrate on blocking the blow!", eChatType.CT_Skill, eChatLoc.CL_SystemWindow);

							if (blockChance < 0.95)
								blockChance = 0.95;
						}
					}
				}

				if (ad.AttackType == eAttackType.MeleeDualWield)
					blockChance *= 0.5;
			}

			// Infiltrator RR5.
			if (player != null)
			{
				OverwhelmEffect Overwhelm = player.EffectList.GetOfType<OverwhelmEffect>();

				if (Overwhelm != null)
					blockChance = Math.Max(blockChance - OverwhelmAbility.BONUS, 0);
			}

			return blockChance;
		}

		public double GetAttackerDefensePenetration(GameLiving living, DbInventoryItem weapon)
		{
			double totalReduction = 0.0;

			if (living is GamePlayer p)
            {
	            double skillBasedReduction = living.WeaponSpecLevel(weapon) * 0.15;
	            double statBasedReduction = living.GetWeaponStat(weapon) * .05;
				//p.CharacterClass.WeaponSkillBase returns unscaled damage table value
				//divide by 200 to change to scaling factor. example: warrior's 460 WeaponSkillBase / 200 = 2.3 Damage Table
				//divide by final 2.1 to use the 2.1 damage table as our anchor. classes below 2.1 damage table will have slightly reduced penetration, above 2.1 will have increased penetration
				double tableMod = p.CharacterClass.WeaponSkillBase / 200.0 / 2.1;
				totalReduction = (skillBasedReduction + statBasedReduction) * tableMod;
            }
			else
			{
				double NPCReduction = 15 * (living.Level / 50.0); //10% penetration at level 50
				totalReduction = NPCReduction;
				if(totalReduction < 0) totalReduction = 0;
			}
				
			return totalReduction;
		}

		/// <summary>
		/// Modify the attack done to this living.
		/// This method offers us a chance to modify the attack data prior to the living taking damage.
		/// </summary>
		/// <param name="attackData">The attack data for this attack</param>
		public virtual void ModifyAttack(AttackData attackData)
		{
		}


		/// <summary>
		/// This method is called whenever this living
		/// should take damage from some source
		/// </summary>
		/// <param name="source">the damage source</param>
		/// <param name="damageType">the damage type</param>
		/// <param name="damageAmount">the amount of damage</param>
		/// <param name="criticalAmount">the amount of critical damage</param>
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			base.TakeDamage(source, damageType, damageAmount, criticalAmount);

			double damageDealt = damageAmount + criticalAmount;

			#region PVP DAMAGE

			// Is this a GamePlayer behind the source?
			if (source is GamePlayer || (source is GameNPC && (source as GameNPC).Brain is IControlledBrain && ((source as GameNPC).Brain as IControlledBrain).GetPlayerOwner() != null) || source is GameSiegeWeapon)
			{
				// Only apply to necropet.
				if (this is NecromancerPet)
				{
					//And if a GamePlayer is behind
					GamePlayer this_necro_pl = null;

					if (this is GameNPC && (this as GameNPC).Brain is IControlledBrain)
						this_necro_pl = ((this as GameNPC).Brain as IControlledBrain).GetPlayerOwner();

					if (this_necro_pl != null && this_necro_pl.Realm != source.Realm && source.Realm != 0)
						DamageRvRMemory += (long)damageDealt + (long)criticalAmount;
				}
			}

			#endregion PVP DAMAGE

			if (source != null && source is GameNPC)
			{
				IControlledBrain brain = ((GameNPC)source).Brain as IControlledBrain;
				if (brain != null)
					source = brain.GetLivingOwner();
			}

			GamePlayer attackerPlayer = source as GamePlayer;
			if (attackerPlayer != null && attackerPlayer != this)
			{
				// Apply Mauler RA5L
				GiftOfPerizorEffect GiftOfPerizor = EffectList.GetOfType<GiftOfPerizorEffect>();
				if (GiftOfPerizor != null)
				{
					int difference = (int)(0.25 * damageDealt); // RA absorb 25% damage
					damageDealt -= difference;
					GamePlayer TheMauler = this.TempProperties.GetProperty<GamePlayer>("GiftOfPerizorOwner", null);
					if (TheMauler != null && TheMauler.IsAlive)
					{
						// Calculate mana using %. % is calculated with target maxhealth and damage difference, apply this % to mauler maxmana
						double manareturned = (difference / this.MaxHealth * TheMauler.MaxMana);
						TheMauler.ChangeMana(source, eManaChangeType.Spell, (int)manareturned);
					}
				}

				Group attackerGroup = attackerPlayer.Group;
				if (attackerGroup != null)
				{
					List<GameLiving> xpGainers = new List<GameLiving>(8);
					// collect "helping" group players in range
					foreach (GameLiving living in attackerGroup.GetMembersInTheGroup())
					{
						if (this.IsWithinRadius(living, WorldMgr.MAX_EXPFORKILL_DISTANCE) && living.IsAlive && living.ObjectState == eObjectState.Active)
							xpGainers.Add(living);
					}

					foreach (GameLiving living in xpGainers)
						this.AddXPGainer(living, (float)(damageDealt / xpGainers.Count));
				}
				else
				{
					this.AddXPGainer(source, (float)damageDealt);
				}
				//DealDamage needs to be called after addxpgainer!
			}
			else if (source != null && source != this)
			{
				AddXPGainer(source, (float)damageAmount + criticalAmount);
			}

			/*
			//[Freya] Nidel: Use2's Flask
			if(this is GamePlayer)
			{
				bool isFatalBlow = (damageAmount + criticalAmount) >= Health;

				if (isFatalBlow)
				{
					GameSpellEffect deadFlask = SpellHandler.FindEffectOnTarget(this, "DeadFlask");
					if(deadFlask != null)
					{
						if(Util.Chance((int)deadFlask.Spell.Value))
						{
							if (IsLowHealth)
								Notify(GameLivingEvent.LowHealth, this, null);
							return;
						}
					}
				}
			}*/

			bool wasAlive = IsAlive;

			Health -= damageAmount + criticalAmount;

			if (!IsAlive && wasAlive && isDeadOrDying == false)
            {
					if (Monitor.TryEnter(deadLock))
					{
						try
						{
						isDeadOrDying = true;
						Die(source);
						}
						finally
						{
							Monitor.Exit(deadLock);
						}
					}
					else
					{
					return;
					}
			}
			else
			{
				if (IsLowHealth)
					Notify(GameLivingEvent.LowHealth, this, null);
			}
		}
		object deadLock = new object();
		/// <summary>
		/// Called on the attacker when attacking an enemy.
		/// </summary>
		public virtual void OnAttackEnemy(AttackData ad)
        {
			//Console.WriteLine(string.Format("OnAttack called on {0}", this.Name));

			// Note that this function is called whenever an attack is made, regardless of whether that attack was successful.
			// i.e. missed melee swings and resisted spells still trigger this.
			
			if (effectListComponent is null)
                return;

			if (this is GamePlayer player)
				player.Stealth(false);

			//Cancel SpeedOfTheRealm (Hastener Speed) 
			if (effectListComponent.Effects.ContainsKey(eEffect.MovementSpeedBuff))
			{
				var effects = effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff);

				for (int i = 0; i < effects.Count; i++)
				{
					if (effects[i] is null)
						continue;

					var spellEffect = effects[i] as ECSGameSpellEffect;
					if (spellEffect != null && spellEffect.Name.ToLower().Equals("speed of the realm"))
					{
						EffectService.RequestImmediateCancelEffect(effects[i]);
					}
				}
            }

			if(ad != null && ad.Damage > 0)
				TryCancelMovementSpeedBuffs(true);

			var oProcEffects = effectListComponent.GetSpellEffects(eEffect.OffensiveProc);
            //OffensiveProcs
            if (ad != null && ad.Attacker == this && oProcEffects != null && ad.AttackType != AttackData.eAttackType.Spell && ad.AttackResult != eAttackResult.Missed)
            {
                for (int i = 0; i < oProcEffects.Count; i++)
                {
                    var oProcEffect = oProcEffects[i];

                    (oProcEffect.SpellHandler as OffensiveProcSpellHandler).EventHandler(ad);
                }
            }
			DirtyTricksECSGameEffect dt = (DirtyTricksECSGameEffect)EffectListService.GetAbilityEffectOnTarget(this, eEffect.DirtyTricks);
			if (dt != null)
            {
				dt.EventHandler(ad);
            }
			TripleWieldECSGameEffect tw = (TripleWieldECSGameEffect)EffectListService.GetAbilityEffectOnTarget(this, eEffect.TripleWield);
			if (tw != null)
            {
				tw.EventHandler(ad);
            }

			if (ad.Target is GamePlayer && ad.Target != this)
			{
				LastAttackTickPvP = GameLoop.GameLoopTime;
			}
			else
			{
				LastAttackTickPvE = GameLoop.GameLoopTime;
			}

			if (this is GameNPC npc)
			{
				var brain = npc.Brain as ControlledMobBrain;

                if (ad.Target is GamePlayer)
				{
					LastAttackTickPvP = GameLoop.GameLoopTime;
					if (brain != null)
						brain.Owner.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
				}
				else
				{
					LastAttackTickPvE = GameLoop.GameLoopTime;
					if (brain != null)
						brain.Owner.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
				}
			}

			// Don't cancel offensive focus spell
			if (ad.AttackType != eAttackType.Spell)
				CancelFocusSpell();
        }

        public void CancelFocusSpell(bool moving = false)
        {
            foreach (var pulseSpell in effectListComponent.GetSpellEffects(eEffect.Pulse))
            {
				if (pulseSpell.SpellHandler.Spell.IsFocus)
                {
					((SpellHandler)pulseSpell.SpellHandler).FocusSpellAction(moving);
					EffectService.RequestImmediateCancelEffect(pulseSpell);
					if (((SpellHandler)pulseSpell.SpellHandler).Target.effectListComponent.Effects.TryGetValue(eEffect.FocusShield, out var petEffect))
                    {
						if (petEffect is not null)
                        {
							//verify the effect is a focus shield and not a timer based damage shield
							if (petEffect.FirstOrDefault().SpellHandler.Spell.IsFocus)
								EffectService.RequestImmediateCancelEffect(petEffect.FirstOrDefault());
						}
					}
				}
            }
        }

		/// <summary>
		/// This method is called at the end of the attack sequence to
		/// notify objects if they have been attacked/hit by an attack
		/// </summary>
		/// <param name="ad">information about the attack</param>
		public virtual void OnAttackedByEnemy(AttackData ad)
		{
			//Console.WriteLine(string.Format("OnAttackedByEnemy called on {0}", this.Name));

			// Note that this function is called whenever an attack is received, regardless of whether that attack was successful.
			// i.e. missed melee swings and resisted spells still trigger this.

			if (ad == null)
				return;

			// Must be above the IsHit/Combat check below since things like subsequent DoT ticks don't cause combat but should still break CC.
			HandleCrowdControlOnAttacked(ad);

			if (ad.IsHit && ad.CausesCombat)
			{
				//Notify(GameLivingEvent.AttackedByEnemy, this, new AttackedByEnemyEventArgs(ad));               
				HandleMovementSpeedEffectsOnAttacked(ad);

				if (this is GameNPC gameNpc && ActiveWeaponSlot == eActiveWeaponSlot.Distance && IsWithinRadius(ad.Attacker, 150))
					gameNpc.SwitchToMelee(ad.Attacker);

				attackComponent.AddAttacker(ad.Attacker);

				if (ad.SpellHandler == null || (ad.SpellHandler != null && ad.SpellHandler is not DoTSpellHandler))
				{
					if (ad.Attacker.Realm == 0 || Realm == 0)
					{
						LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
						ad.Attacker.LastAttackTickPvE = GameLoop.GameLoopTime;
					}
					else if (ad.Attacker != this) //Check if the attacker is not this living (some things like Res Sickness have attacker/target the same)
					{
						LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
						ad.Attacker.LastAttackTickPvP = GameLoop.GameLoopTime;
					}
				}

				// Melee Attack that actually caused damage.
				if (ad.IsMeleeAttack && ad.Damage > 0)
				{
					// Handle Ablatives.
					List<ECSGameSpellEffect> effects = effectListComponent.GetSpellEffects(eEffect.AblativeArmor);

					for (int i = 0; i < effects.Count; i++)
					{
						AblativeArmorECSGameEffect effect = effects[i] as AblativeArmorECSGameEffect;

						if (effect == null)
							continue;

						AblativeArmorSpellHandler ablativeArmorSpellHandler = effect.SpellHandler as AblativeArmorSpellHandler;

						if (!ablativeArmorSpellHandler.MatchingDamageType(ref ad))
							continue;

						int ablativeHp = effect.RemainingValue;
						double absorbPercent = AblativeArmorSpellHandler.ValidateSpellDamage((int)effect.SpellHandler.Spell.Damage);
						int damageAbsorbed = (int)(0.01 * absorbPercent * (ad.Damage + ad.CriticalDamage));

						if (damageAbsorbed > ablativeHp)
							damageAbsorbed = ablativeHp;

						ablativeHp -= damageAbsorbed;
						ad.Damage -= damageAbsorbed;

						(effect.SpellHandler as AblativeArmorSpellHandler).OnDamageAbsorbed(ad, damageAbsorbed);

						if (ad.Target is GamePlayer)
							(ad.Target as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((ad.Target as GamePlayer).Client, "AblativeArmor.Target", damageAbsorbed), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

						if (ad.Attacker is GamePlayer)
							(ad.Attacker as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((ad.Attacker as GamePlayer).Client, "AblativeArmor.Attacker", damageAbsorbed), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

						if (ablativeHp <= 0)
							EffectService.RequestImmediateCancelEffect(effect);
						else
							effect.RemainingValue = ablativeHp;
					}
				}

				// Handle DefensiveProcs.
				List<ECSGameSpellEffect> dProcEffects = effectListComponent.GetSpellEffects(eEffect.DefensiveProc);

				if (ad != null && ad.Target == this && dProcEffects != null && ad.AttackType != eAttackType.Spell)
				{
					for (int i = 0; i < dProcEffects.Count; i++)
						(dProcEffects[i].SpellHandler as DefensiveProcSpellHandler).EventHandler(ad);
				}
			}
			else if (ad.IsSpellResisted && ad.Target is GameNPC npc)
				npc.CancelReturnToSpawnPoint();
		}

		public void HandleDamageShields(AttackData ad)
		{
			var dSEffects = effectListComponent.GetSpellEffects(eEffect.FocusShield);
			// Handle DamageShield damage
			if (dSEffects != null)
			{
				for (int i = 0; i < dSEffects.Count; i++)
				{
					if (dSEffects[i].IsBuffActive)
					{
						var dSEffect = dSEffects[i];

						((DamageShieldSpellHandler)dSEffect.SpellHandler).EventHandler(null, this, new AttackedByEnemyEventArgs(ad));
					}
				}
			}
		}

		/// <summary>
		/// Attempt to break/remove CC spells on this living. Returns true if any CC spells were removed.
		/// </summary>
		public virtual bool HandleCrowdControlOnAttacked(AttackData ad)
		{
			if (effectListComponent == null || ad == null || !ad.IsHit)
				return false;

			bool removeMez = false;
			bool removeSnare = false; // Immunity-triggering snare/root spells
			bool removeMovementSpeedDebuff = false; // Non-immunity snares like focus snare, melee snares, DD+Snare spells, etc.

			// Attack was Melee
			if (ad.AttackType != AttackData.eAttackType.Spell)
			{
				switch (ad.AttackResult)
				{
					case eAttackResult.HitStyle:
					case eAttackResult.HitUnstyled:
						removeSnare = true;
						removeMez = true;
						removeMovementSpeedDebuff = true;
						break;
					case eAttackResult.Blocked:
					case eAttackResult.Evaded:
					case eAttackResult.Fumbled:
					case eAttackResult.Missed:
					case eAttackResult.Parried:
						// Missed melee swings still break mez.
						removeMez = true;
						break;
				}
			}
			// Attack was a Spell. Note that a spell being resisted does not mean it does not break mez.
			else
			{
				// Any damage breaks mez and snare/root.
				if (ad.Damage > 0)
				{
					removeMez = true;
					removeSnare = true;
					removeMovementSpeedDebuff = true;
				}
				// Most resisted harmful spells break mez. There are some exceptions which can be added here.
				else if (ad.IsSpellResisted)
                {
					removeMez = true;
                }
				// Non-Damaging, non-resisted spells that break mez.
				else if (ad.SpellHandler is NearsightSpellHandler || ad.SpellHandler is AmnesiaSpellHandler || ad.SpellHandler is DiseaseSpellHandler
						 || ad.SpellHandler is SpeedDecreaseSpellHandler || ad.SpellHandler is StunSpellHandler || ad.SpellHandler is ConfusionSpellHandler
						 || ad.SpellHandler is AbstractResistDebuff) 
				{
					removeMez = true;
				}
				
				if (this is GameNPC && ad.SpellHandler is not MesmerizeSpellHandler)
					removeMez = true;
			}

			// Remove Mez
            if (removeMez && effectListComponent.Effects.ContainsKey(eEffect.Mez))
			{
				var effect = EffectListService.GetEffectOnTarget(this, eEffect.Mez);

				if (effect != null)
					EffectService.RequestImmediateCancelEffect(effect);
			}

			// Remove Snare/Root
			if (removeSnare && effectListComponent.Effects.ContainsKey(eEffect.Snare))
			{
				var effect = EffectListService.GetEffectOnTarget(this, eEffect.Snare);

				if (effect != null)
					EffectService.RequestImmediateCancelEffect(effect);
			}

            // Remove MovementSpeedDebuff
            if (removeMovementSpeedDebuff)
            {
				var effect = EffectListService.GetEffectOnTarget(this, eEffect.MovementSpeedDebuff);

				if (effect != null && effect is ECSGameSpellEffect spellEffect && spellEffect.SpellHandler.Spell.SpellType != eSpellType.UnbreakableSpeedDecrease)
					EffectService.RequestImmediateCancelEffect(effect);

				var ichor_effect = EffectListService.GetEffectOnTarget(this, eEffect.Ichor);
				if (ichor_effect != null)
					EffectService.RequestImmediateCancelEffect(ichor_effect);
            }

            return removeMez || removeSnare || removeMovementSpeedDebuff;
		}

        public virtual void HandleMovementSpeedEffectsOnAttacked(AttackData ad)
        {
            if (effectListComponent == null || ad == null)
                return;

			//Cancel SpeedOfTheRealm (Hastener Speed) 
			if (effectListComponent.Effects.ContainsKey(eEffect.MovementSpeedBuff))
			{
				var effects = effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff);

				for (int i = 0; i < effects.Count; i++)
				{
					if (effects[i] is null)
						continue;

					var spellEffect = effects[i];
					if (spellEffect != null && spellEffect.SpellHandler.Spell.ID is 2430) // Speed of the Realm
						EffectService.RequestImmediateCancelEffect(effects[i]);
				}
            }

            // Cancel movement speed buffs when attacked only if damaged
			if (ad != null & ad.Damage > 0)
				TryCancelMovementSpeedBuffs(false);
		}

        public virtual void TryCancelMovementSpeedBuffs(bool isAttacker)
        {
            if (effectListComponent == null)
                return;

            if (effectListComponent.Effects.ContainsKey(eEffect.MovementSpeedBuff))
			{
				var effects = effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff);

				for (int i = 0; i < effects.Count; i++)
				{
					if (effects[i] is null)
						continue;

					var spellEffect = effects[i];
					if (spellEffect != null && spellEffect.SpellHandler.Spell.Target == eSpellTarget.PET)
					{
						if (spellEffect.SpellHandler.Spell.ID is 305 // Whip of Encouragement
							or (>= 895 and <= 897)) // Tracker, Chaser, Pursuer Enhancement
							continue;
					}
					
					EffectService.RequestImmediateCancelEffect(effects[i]);
				}
            }

			if (this is GameNPC npc && npc.Brain is ControlledMobBrain || this is GameSummonedPet)
            {
				List<ECSGameSpellEffect> ownerEffects;
				ControlledMobBrain pBrain = (this as GameNPC).Brain as ControlledMobBrain;
				GameSummonedPet pet = this as GameSummonedPet;

				if (pBrain != null)
					ownerEffects = pBrain.Owner.effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff);
				else
					ownerEffects = pet.Owner.effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff);

				for (int i = 0; i < ownerEffects.Count; i++)
				{
					if (isAttacker || ownerEffects[i] is not ECSGameSpellEffect spellEffect || spellEffect.SpellHandler.Spell.Target != eSpellTarget.SELF)
						EffectService.RequestImmediateCancelEffect(ownerEffects[i]);
				}				
            }
        }

        /// <summary>
        /// This method is called whenever this living is dealing
        /// damage to some object
        /// </summary>
        /// <param name="ad">AttackData</param>
        public virtual void DealDamage(AttackData ad)
		{
			ad.Target.TakeDamage(ad);
		}

		/// <summary>
		/// Adds a object to the list of objects that will gain xp
		/// after this living dies
		/// </summary>
		/// <param name="xpGainer">the xp gaining object</param>
		/// <param name="damageAmount">the amount of damage, float because for groups it can be split</param>
		public virtual void AddXPGainer(GameObject xpGainer, float damageAmount)
		{
			lock (m_xpGainers.SyncRoot)
			{
				if( m_xpGainers.Contains( xpGainer ) == false )
				{
					m_xpGainers.Add( xpGainer, 0.0f );
				}
				m_xpGainers[xpGainer] = (float)m_xpGainers[xpGainer] + damageAmount;
			}
		}

		/// <summary>
		/// Changes the health
		/// </summary>
		/// <param name="changeSource">the source that inited the changes</param>
		/// <param name="healthChangeType">the change type</param>
		/// <param name="changeAmount">the change amount</param>
		/// <returns>the amount really changed</returns>
		public virtual int ChangeHealth(GameObject changeSource, eHealthChangeType healthChangeType, int changeAmount)
		{
			//TODO fire event that might increase or reduce the amount
			int oldHealth = Health;
			Health += changeAmount;
			int healthChanged = Health - oldHealth;

			//Notify our enemies that we were healed by other means than
			//natural regeneration, this allows for aggro on healers!
			if (healthChanged > 0 && healthChangeType != eHealthChangeType.Regenerate)
			{
				EnemyHealedEventArgs args = new(this, changeSource, healthChangeType, healthChanged);

				foreach (GameLiving attacker in attackComponent.Attackers.Keys)
				{
					if (attacker is not GameLiving attackerLiving)
						continue;

					attackerLiving.Notify(GameLivingEvent.EnemyHealed, attacker, args);
				}
			}

			return healthChanged;
		}

		/// <summary>
		/// Changes the mana
		/// </summary>
		/// <param name="changeSource">the source that inited the changes</param>
		/// <param name="manaChangeType">the change type</param>
		/// <param name="changeAmount">the change amount</param>
		/// <returns>the amount really changed</returns>
		public virtual int ChangeMana(GameObject changeSource, eManaChangeType manaChangeType, int changeAmount)
		{
			//TODO fire event that might increase or reduce the amount
			int oldMana = Mana;
			Mana += changeAmount;
			return Mana - oldMana;
		}

		/// <summary>
		/// Changes the endurance
		/// </summary>
		/// <param name="changeSource">the source that inited the changes</param>
		/// <param name="enduranceChangeType">the change type</param>
		/// <param name="changeAmount">the change amount</param>
		/// <returns>the amount really changed</returns>
		public virtual int ChangeEndurance(GameObject changeSource, eEnduranceChangeType enduranceChangeType, int changeAmount)
		{
			//TODO fire event that might increase or reduce the amount
			int oldEndurance = Endurance;
			Endurance += changeAmount;
			return Endurance - oldEndurance;
		}

		/// <summary>
		/// Called when an enemy of ours is healed during combat
		/// </summary>
		/// <param name="enemy">the enemy</param>
		/// <param name="healSource">the healer</param>
		/// <param name="changeType">the healtype</param>
		/// <param name="healAmount">the healamount</param>
		public virtual void EnemyHealed(GameLiving enemy, GameObject healSource, eHealthChangeType changeType, int healAmount)
		{
			Notify(GameLivingEvent.EnemyHealed, this, new EnemyHealedEventArgs(enemy, healSource, changeType, healAmount));
		}

		/// <summary>
		/// Called when this living dies
		/// </summary>
		public virtual void Die(GameObject killer)
		{
			isDeadOrDying = true;
			//Console.WriteLine($"Dead or Dying set to {this.isDeadOrDying} for {this.Name} in living");
			ReaperService.KillLiving(this, killer);
		}

		public virtual void ProcessDeath(GameObject killer)
		{
			try
			{
				if (this is not GameNPC and not GamePlayer)
				{
					// deal out exp and realm points based on server rules
					GameServer.ServerRules.OnLivingKilled(this, killer);
				}

				attackComponent.StopAttack();

				if (killer is GameLiving livingKiller)
					attackComponent.Attackers.TryAdd(livingKiller, long.MaxValue);

				List<GamePlayer> playerAttackers = new();

				foreach (GameObject attacker in attackComponent.Attackers.Keys)
				{
					if (attacker is not GameLiving livingAttacker)
						continue;

					GamePlayer player = attacker as GamePlayer;

					if (attacker is GameNPC npcAttacker && npcAttacker.Brain is IControlledBrain npcAttackerBrain)
					{
						// Ok, we're a pet - if our Player owner isn't in the attacker list, let's make them a 'virtual' attacker
						player = npcAttackerBrain.GetPlayerOwner();

						if (player != null)
						{
							if (!attackComponent.Attackers.ContainsKey(player))
							{
								if (!playerAttackers.Contains(player))
									playerAttackers.Add(player);
							}

							// Pet gets the killed message as well
							livingAttacker.EnemyKilled(this);
						}
					}

					if (player != null)
					{
						if (!playerAttackers.Contains(player))
							playerAttackers.Add(player);

						if (player.Group != null)
						{
							foreach (GamePlayer groupPlayer in player.Group.GetPlayersInTheGroup())
							{
								if (groupPlayer.IsWithinRadius(this, WorldMgr.MAX_EXPFORKILL_DISTANCE) && playerAttackers.Contains(groupPlayer) == false)
									playerAttackers.Add(groupPlayer);
							}
						}
					}
					else
						livingAttacker.EnemyKilled(this);
				}

				foreach (GamePlayer player in playerAttackers)
					player.EnemyKilled(this);

				foreach (Quests.DataQuest q in DataQuestList)
					q.Notify(GameLivingEvent.Dying, this, new DyingEventArgs(killer, playerAttackers));

				attackComponent.Attackers.Clear();

				// clear all of our targets
				rangeAttackComponent.AutoFireTarget = null;
				TargetObject = null;

				// cancel all left effects
				EffectList.CancelAll();
				effectListComponent.CancelAll();

				// Stop the regeneration timers
				StopHealthRegeneration();
				StopPowerRegeneration();
				StopEnduranceRegeneration();

				//Reduce health to zero
				Health = 0;

				// Remove all last attacked times
				LastAttackedByEnemyTickPvE = 0;
				LastAttackedByEnemyTickPvP = 0;

				//Let's send the notification at the end
				Notify(GameLivingEvent.Dying, this, new DyingEventArgs(killer));
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
			}
			finally
			{
				//isDying flag is ALWAYS set to false even if exception happens so it can get remove from the list
				isDeadOrDying = false;
			}
		}

		/// <summary>
		/// Called when the living is gaining experience
		/// </summary>
		/// <param name="expTotal">total amount of xp to gain</param>
		/// <param name="expCampBonus">camp bonus to display</param>
		/// <param name="expGroupBonus">group bonus to display</param>
		/// <param name="expOutpostBonus">outpost bonux to display</param>
		/// <param name="sendMessage">should exp gain message be sent</param>
		/// <param name="allowMultiply">should the xp amount be multiplied</param>
		public virtual void GainExperience(eXPSource xpSource, long expTotal, long expCampBonus, long expGroupBonus, long expOutpostBonus, bool sendMessage, bool allowMultiply, bool notify)
		{
			if (expTotal > 0 && notify)
				Notify(GameLivingEvent.GainedExperience, this, new GainedExperienceEventArgs(expTotal, expCampBonus, expGroupBonus, expOutpostBonus, sendMessage, allowMultiply, xpSource));
		}
		/// <summary>
		/// Called when this living gains realm points
		/// </summary>
		/// <param name="amount">amount of realm points gained</param>
		public virtual void GainRealmPoints(long amount)
		{
			Notify(GameLivingEvent.GainedRealmPoints, this, new GainedRealmPointsEventArgs(amount));
		}
		/// <summary>
		/// Called when this living gains bounty points
		/// </summary>
		/// <param name="amount"></param>
		public virtual void GainBountyPoints(long amount)
		{
			Notify(GameLivingEvent.GainedBountyPoints, this, new GainedBountyPointsEventArgs(amount));
		}
		/// <summary>
		/// Called when the living is gaining experience
		/// </summary>
		/// <param name="exp">base amount of xp to gain</param>
		public void GainExperience(eXPSource xpSource, long exp)
		{
			GainExperience(xpSource, exp, 0, 0, 0, true, false, true);
		}

		/// <summary>
		/// Called when the living is gaining experience
		/// </summary>
		/// <param name="exp">base amount of xp to gain</param>
		/// <param name="allowMultiply">Do we allow the xp to be multiplied</param>
		public void GainExperience(eXPSource xpSource, long exp, bool allowMultiply)
		{
			GainExperience(xpSource, exp, 0, 0, 0, true, allowMultiply, true);
		}

		/// <summary>
		/// Called when an enemy of this living is killed
		/// </summary>
		/// <param name="enemy">enemy killed</param>
		public virtual void EnemyKilled(GameLiving enemy)
		{
			Notify(GameLivingEvent.EnemyKilled, this, new EnemyKilledEventArgs(enemy));
		}

		/// <summary>
		/// Holds visible active weapon slots
		/// </summary>
		protected byte m_visibleActiveWeaponSlots = 0xFF; // none by default

		/// <summary>
		/// Gets visible active weapon slots
		/// </summary>
		public byte VisibleActiveWeaponSlots
		{
			get { return m_visibleActiveWeaponSlots; }
			set { m_visibleActiveWeaponSlots=value; }
		}

		/// <summary>
		/// Holds the living's cloak hood state
		/// </summary>
		protected bool m_isCloakHoodUp;

		/// <summary>
		/// Sets/gets the living's cloak hood state
		/// </summary>
		public virtual bool IsCloakHoodUp
		{
			get { return m_isCloakHoodUp; }
			set { m_isCloakHoodUp = value; }
		}

		/// <summary>
		/// Holds the living's cloak hood state
		/// </summary>
		protected bool m_IsCloakInvisible = false;

		/// <summary>
		/// Sets/gets the living's cloak visible state
		/// </summary>
		public virtual bool IsCloakInvisible
		{
			get { return m_IsCloakInvisible; }
			set { m_IsCloakInvisible = value; }
		}

		/// <summary>
		/// Holds the living's helm visible state
		/// </summary>
		protected bool m_IsHelmInvisible = false;

		/// <summary>
		/// Sets/gets the living's cloak hood state
		/// </summary>
		public virtual bool IsHelmInvisible
		{
			get { return m_IsHelmInvisible; }
			set { m_IsHelmInvisible = value; }
		}

		/// <summary>
		/// Switches the active weapon to another one
		/// </summary>
		/// <param name="slot">the new eActiveWeaponSlot</param>
		public virtual void SwitchWeapon(eActiveWeaponSlot slot)
		{
			if (Inventory == null)
				return;

			rangeAttackComponent.RangedAttackState = eRangedAttackState.None;
			rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;

			DbInventoryItem rightHandSlot = Inventory.GetItem(eInventorySlot.RightHandWeapon);
			DbInventoryItem leftHandSlot = Inventory.GetItem(eInventorySlot.LeftHandWeapon);
			DbInventoryItem twoHandSlot = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
			DbInventoryItem distanceSlot = Inventory.GetItem(eInventorySlot.DistanceWeapon);

			// simple active slot logic:
			// 0=right hand, 1=left hand, 2=two-hand, 3=range, F=none
			int rightHand = VisibleActiveWeaponSlots & 0x0F;
			int leftHand = (VisibleActiveWeaponSlots & 0xF0) >> 4;

			// set new active weapon slot
			switch (slot)
			{
				case eActiveWeaponSlot.Standard:
				{
					if (rightHandSlot == null)
						rightHand = 0xFF;
					else
						rightHand = 0x00;

					if (leftHandSlot == null)
						leftHand = 0xFF;
					else
						leftHand = 0x01;

					break;
				}

				case eActiveWeaponSlot.TwoHanded:
				{
					if (twoHandSlot != null && (twoHandSlot.Hand == 1 || this is GameNPC)) // 2h
					{
						rightHand = leftHand = 0x02;
						break;
					}

					// 1h weapon in 2h slot
					if (twoHandSlot == null)
						rightHand = 0xFF;
					else
						rightHand = 0x02;

					if (leftHandSlot == null)
						leftHand = 0xFF;
					else
						leftHand = 0x01;

					break;
				}

				case eActiveWeaponSlot.Distance:
				{
					leftHand = 0xFF; // cannot use left-handed weapons if ranged slot active

					if (distanceSlot == null)
						rightHand = 0xFF;
					else if (distanceSlot.Hand == 1 || this is GameNPC) // NPC equipment does not have hand so always assume 2 handed bow
						rightHand = leftHand = 0x03; // bows use 2 hands, throwing axes 1h
					else
						rightHand = 0x03;

					break;
				}
			}

			m_activeWeaponSlot = slot;

			// pack active weapon slots value back
			m_visibleActiveWeaponSlots = (byte)(((leftHand & 0x0F) << 4) | (rightHand & 0x0F));
		}

		#endregion

		#region Property/Bonus/Buff/PropertyCalculator fields
		/// <summary>
		/// Array for property boni for abilities
		/// </summary>
		protected IPropertyIndexer m_abilityBonus = new PropertyIndexer();
		/// <summary>
		/// Ability bonus property
		/// </summary>
		public virtual IPropertyIndexer AbilityBonus
		{
			get { return m_abilityBonus; }
		}

		/// <summary>
		/// Array for property boni by items
		/// </summary>
		protected IPropertyIndexer m_itemBonus = new PropertyIndexer();
		/// <summary>
		/// Property Item Bonus field
		/// </summary>
		public virtual IPropertyIndexer ItemBonus
		{
			get { return m_itemBonus; }
		}


		/// <summary>
		/// Array for buff boni
		/// </summary>
		protected IPropertyIndexer m_buff1Bonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer BaseBuffBonusCategory
		{
			get { return m_buff1Bonus; }
		}

		/// <summary>
		/// Array for second buff boni
		/// </summary>
		protected IPropertyIndexer m_buff2Bonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer SpecBuffBonusCategory
		{
			get { return m_buff2Bonus; }
		}

		/// <summary>
		/// Array for third debuff boni
		/// </summary>
		protected IPropertyIndexer m_debuffBonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer DebuffCategory
		{
			get { return m_debuffBonus; }
		}

		/// <summary>
		/// Array for forth buff boni
		/// </summary>
		protected IPropertyIndexer m_buff4Bonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer BuffBonusCategory4
		{
			get { return m_buff4Bonus; }
		}

		/// <summary>
		/// Array for first multiplicative buff boni
		/// </summary>
		protected IMultiplicativeProperties m_buffMult1Bonus = new MultiplicativePropertiesHybrid();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IMultiplicativeProperties BuffBonusMultCategory1
		{
			get { return m_buffMult1Bonus; }
		}

		/// <summary>
		/// Array for spec debuff boni
		/// </summary>
		protected IPropertyIndexer m_specDebuffBonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer SpecDebuffCategory
		{
			get { return m_specDebuffBonus; }
		}
		
		/// <summary>
		/// property calculators for each property
		/// look at PropertyCalculator class for more description
		/// </summary>
		internal static readonly IPropertyCalculator[] m_propertyCalc = new IPropertyCalculator[(int)eProperty.MaxProperty+1];

		/// <summary>
		/// retrieve a property value of that living
		/// this value is modified/capped and ready to use
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int GetModified(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValue(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		//Eden : secondary resists, such AoM, vampiir magic resistance etc, should not apply in CC duration, disease, debuff etc, using a new function
		public virtual int GetModifiedBase(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValueBase(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find base property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		/// <summary>
		/// Retrieve a property value of this living's buff bonuses only;
		/// caps and cap increases apply.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int GetModifiedFromBuffs(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValueFromBuffs(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find buff property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		/// <summary>
		/// Retrieve a property value of this living's item bonuses only;
		/// caps and cap increases apply.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int GetModifiedFromItems(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValueFromItems(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find item property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		/// <summary>
		/// has to be called after properties were changed and updates are needed
		/// TODO: not sure about property change detection, has to be reviewed
		/// </summary>
		public virtual void PropertiesChanged()
		{
			//			// take last changes as old ones now
			//			for (int i=0; i<m_oldTempProps.Length; i++)
			//			{
			//				m_oldTempProps[i] = m_newTempProps[i];
			//			}
			//
			//			// recalc new array to detect changes later
			//			for (int i=0; i<m_propertyCalc.Length; i++)
			//			{
			//				if (m_propertyCalc[i]!=null)
			//				{
			//					m_newTempProps[i] = m_propertyCalc[i].CalcValue(this, (eProperty)i);
			//				}
			//				else
			//				{
			//					m_newTempProps[i] = 0;
			//				}
			//			}
		}

		#endregion
		#region Stats, Resists
		/// <summary>
		/// The name of the states
		/// </summary>
		public static readonly string[] STAT_NAMES = new string[]{"Unknown Stat","Strength", "Dexterity", "Constitution", "Quickness", "Intelligence",
			"Piety", "Empathy", "Charisma"};

		/// <summary>
		/// base values for char stats
		/// </summary>
		protected readonly short[] m_charStat = new short[8];
		/// <summary>
		/// get a unmodified char stat value
		/// </summary>
		/// <param name="stat"></param>
		/// <returns></returns>
		public virtual int GetBaseStat(eStat stat)
		{
			return m_charStat[stat - eStat._First];
		}
		/// <summary>
		/// changes a base stat value
		/// </summary>
		/// <param name="stat"></param>
		/// <param name="amount"></param>
		public virtual void ChangeBaseStat(eStat stat, short amount)
		{
			m_charStat[stat - eStat._First] += amount;
		}

		/// <summary>
		/// this field is just for convinience and speed purposes
		/// converts the damage types to resist fields
		/// </summary>
		protected static readonly eProperty[] m_damageTypeToResistBonusConversion = new eProperty[] {
			eProperty.Resist_Natural, //0,
			eProperty.Resist_Crush,
			eProperty.Resist_Slash,
			eProperty.Resist_Thrust,
			0, 0, 0, 0, 0, 0,
			eProperty.Resist_Body,
			eProperty.Resist_Cold,
			eProperty.Resist_Energy,
			eProperty.Resist_Heat,
			eProperty.Resist_Matter,
			eProperty.Resist_Spirit
		};
		/// <summary>
		/// gets the resistance value by damage type, refer to eDamageType for constants
		/// </summary>
		/// <param name="damageType"></param>
		/// <returns></returns>
		public virtual eProperty GetResistTypeForDamage(eDamageType damageType)
		{
			if ((int)damageType < m_damageTypeToResistBonusConversion.Length)
			{
				return m_damageTypeToResistBonusConversion[(int)damageType];
			}
			else
			{
				log.ErrorFormat("No resist found for damage type {0} on living {1}!", (int)damageType, Name);
				return 0;
			}
		}
		/// <summary>
		/// gets the resistance value by damage types
		/// </summary>
		/// <param name="damageType">the damag etype</param>
		/// <returns>the resist value</returns>
		public virtual int GetResist(eDamageType damageType)
		{
			return GetModified(GetResistTypeForDamage(damageType));
		}

		public virtual int GetResistBase(eDamageType damageType)
		{
			return GetModifiedBase(GetResistTypeForDamage(damageType));
		}

		/// <summary>
		/// Stores temporary properties on this living.
		/// Beware to use unique keys so they do not interfere
		/// </summary>
		public PropertyCollection TempProperties { get; } = new();

		/// <summary>
		/// Gets or Sets the effective level of the Object
		/// </summary>
		public override int EffectiveLevel
		{
			get { return GetModified(eProperty.LivingEffectiveLevel); }
		}

		/// <summary>
		/// returns the level of a specialization
		/// if 0 is returned, the spec is non existent on living
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public virtual int GetBaseSpecLevel(string keyName)
		{
			return Level;
		}

		/// <summary>
		/// returns the level of a specialization + bonuses from RR and Items
		/// if 0 is returned, the spec is non existent on the living
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public virtual int GetModifiedSpecLevel(string keyName)
		{
			return Level;
		}

		#endregion
		#region Regeneration
		/// <summary>
		/// GameTimer used for restoring hp
		/// </summary>
		protected ECSGameTimer m_healthRegenerationTimer;
		/// <summary>
		/// GameTimer used for restoring mana
		/// </summary>
		protected ECSGameTimer m_powerRegenerationTimer;
		/// <summary>
		/// GameTimer used for restoring endurance
		/// </summary>
		protected ECSGameTimer m_enduRegenerationTimer;

		/// <summary>
		/// The default frequency of regenerating health in milliseconds
		/// </summary>
		protected const ushort m_healthRegenerationPeriod = 6000;

		/// <summary>
		/// Interval for health regeneration tics
		/// </summary>
		protected virtual ushort HealthRegenerationPeriod
		{
			get { return m_healthRegenerationPeriod; }
		}

		/// <summary>
		/// The default frequency of regenerating power in milliseconds
		/// </summary>
		protected const ushort m_powerRegenerationPeriod = 6000;

		/// <summary>
		/// Interval for power regeneration tics
		/// </summary>
		protected virtual ushort PowerRegenerationPeriod
		{
			get { return m_powerRegenerationPeriod; }
		}

		/// <summary>
		/// The default frequency of regenerating endurance in milliseconds
		/// </summary>
		protected const ushort m_enduranceRegenerationPeriod = 1500;

		/// <summary>
		/// Interval for endurance regeneration tics
		/// </summary>
		protected virtual ushort EnduranceRegenerationPeriod
		{
			get { return m_enduranceRegenerationPeriod; }
		}

		/// <summary>
		/// The lock object for lazy regen timers initialization
		/// </summary>
		protected readonly object m_regenTimerLock = new object();

		/// <summary>
		/// Starts the health regeneration
		/// </summary>
		public virtual void StartHealthRegeneration()
		{
			if (!IsAlive || ObjectState != eObjectState.Active)
				return;
			lock (m_regenTimerLock)
			{
				if (m_healthRegenerationTimer == null)
				{
					m_healthRegenerationTimer = new ECSGameTimer(this);
					m_healthRegenerationTimer.Callback = new ECSGameTimer.ECSTimerCallback(HealthRegenerationTimerCallback);
				}
				else if (m_healthRegenerationTimer.IsAlive)
				{
					return;
				}

				m_healthRegenerationTimer.Start(HealthRegenerationPeriod);
			}
		}
		/// <summary>
		/// Starts the power regeneration
		/// </summary>
		public virtual void StartPowerRegeneration()
		{
			
			if (ObjectState != eObjectState.Active)
				return;
			lock (m_regenTimerLock)
			{
				if (m_powerRegenerationTimer == null)
				{
					m_powerRegenerationTimer = new ECSGameTimer(this);
					m_powerRegenerationTimer.Callback = new ECSGameTimer.ECSTimerCallback(PowerRegenerationTimerCallback);
				}
				else if (m_powerRegenerationTimer.IsAlive)
				{
					return;
				}

				m_powerRegenerationTimer.Start(PowerRegenerationPeriod);
			}
		}
		/// <summary>
		/// Starts the endurance regeneration
		/// </summary>
		public virtual void StartEnduranceRegeneration()
		{
			if (ObjectState != eObjectState.Active)
				return;
			lock (m_regenTimerLock)
			{
				if (m_enduRegenerationTimer == null)
				{
					m_enduRegenerationTimer = new ECSGameTimer(this);
					m_enduRegenerationTimer.Callback = new ECSGameTimer.ECSTimerCallback(EnduranceRegenerationTimerCallback);
				}
				else if (m_enduRegenerationTimer.IsAlive)
				{
					return;
				}
				m_enduRegenerationTimer.Start(EnduranceRegenerationPeriod);
			}
		}
		/// <summary>
		/// Stop the health regeneration
		/// </summary>
		public virtual void StopHealthRegeneration()
		{
			lock (m_regenTimerLock)
			{
				if (m_healthRegenerationTimer == null)
					return;
				m_healthRegenerationTimer.Stop();
				m_healthRegenerationTimer = null;
			}
		}
		/// <summary>
		/// Stop the power regeneration
		/// </summary>
		public virtual void StopPowerRegeneration()
		{
			lock (m_regenTimerLock)
			{
				if (m_powerRegenerationTimer == null)
					return;
				m_powerRegenerationTimer.Stop();
				m_powerRegenerationTimer = null;
			}
		}
		/// <summary>
		/// Stop the endurance regeneration
		/// </summary>
		public virtual void StopEnduranceRegeneration()
		{
			lock (m_regenTimerLock)
			{
				if (m_enduRegenerationTimer == null)
					return;
				m_enduRegenerationTimer.Stop();
				m_enduRegenerationTimer = null;
			}
		}

		protected virtual int HealthRegenerationTimerCallback(ECSGameTimer callingTimer)
		{
			if (Health < MaxHealth)
				ChangeHealth(this, eHealthChangeType.Regenerate, GetModified(eProperty.HealthRegenerationRate));

			bool atMaxHealth = Health >= MaxHealth;

			if (this is NecromancerPet necroPet && necroPet.Brain is IControlledBrain necroBrain)
			{
				GamePlayer player = necroBrain.GetPlayerOwner();

				if (player != null && DamageRvRMemory > 0)
				{
					if (atMaxHealth)
						DamageRvRMemory = 0;
					else
						DamageRvRMemory -= Math.Max(GetModified(eProperty.HealthRegenerationRate), 0);
				}
			}

			if (atMaxHealth)
			{
				lock (m_xpGainers.SyncRoot)
				{
					m_xpGainers.Clear();
				}

				return 0;
			}

			if (InCombat)
				return HealthRegenerationPeriod * 5;

			return HealthRegenerationPeriod;
		}

		protected virtual int PowerRegenerationTimerCallback(ECSGameTimer selfRegenerationTimer)
		{
			if (IsVampiirOrMauler())
			{
				double onePercMana = Math.Ceiling(MaxMana * 0.01);

				if (!InCombat)
				{
					ChangeMana(this, eManaChangeType.Regenerate, (int) -onePercMana);
					return 1000;
				}
			}
			else
			{
				int stackingBonus = 0;

				if (this is GamePlayer p)
					stackingBonus = p.PowerRegenStackingBonus;

				if (Mana < MaxMana)
					ChangeMana(this, eManaChangeType.Regenerate, GetModified(eProperty.PowerRegenerationRate) + stackingBonus);

				if (Mana >= MaxMana)
					return 0;
			}

			int totalRegenPeriod = PowerRegenerationPeriod;

			if (InCombat)
				totalRegenPeriod *= 2;

			if (IsSitting)
				totalRegenPeriod /= 2;

			AtlasOF_SerenityAbility raSerenity = GetAbility<AtlasOF_SerenityAbility>();

			if (raSerenity != null && raSerenity.Level > 0)
				totalRegenPeriod -= raSerenity.GetAmountForLevel(raSerenity.Level);

			return totalRegenPeriod;

			bool IsVampiirOrMauler()
			{
				if (this is not GamePlayer player)
					return false;

				eCharacterClass characterClass = (eCharacterClass) player.CharacterClass.ID;
				return characterClass is eCharacterClass.Vampiir || (characterClass >= eCharacterClass.MaulerAlb && characterClass <= eCharacterClass.MaulerHib);
			}
		}

		protected virtual int EnduranceRegenerationTimerCallback(ECSGameTimer selfRegenerationTimer)
		{
			if (Endurance < MaxEndurance)
			{
				int regen = GetModified(eProperty.EnduranceRegenerationRate);

				if (regen > 0)
					ChangeEndurance(this, eEnduranceChangeType.Regenerate, regen);
			}

			if (Endurance >= MaxEndurance)
				return 0;

			return EnduranceRegenerationPeriod;
		}

        #endregion

		#region Components

		public AttackComponent attackComponent;
		public RangeAttackComponent rangeAttackComponent;
		public StyleComponent styleComponent;
		public CastingComponent castingComponent;
		public EffectListComponent effectListComponent;
		public MovementComponent movementComponent;
		public HealthComponent healthComponent;
		public CraftComponent craftComponent;
		//public DamageComponent damageComponent;
		//public StatsComponent statsComponent;
		//public SingleStatBuffComponent buffComponent;

		#endregion

        #region Mana/Health/Endurance/Concentration/Delete
        /// <summary>
        /// Amount of mana
        /// </summary>
        protected int m_mana;
		/// <summary>
		/// Amount of endurance
		/// </summary>
		protected int m_endurance;
		/// <summary>
		/// Maximum value that can be in m_endurance
		/// </summary>
		protected int m_maxEndurance;

		/// <summary>
		/// Gets/sets the object health
		/// </summary>
		public override int Health
		{
			get => m_health;
			set
			{
				int maxHealth = MaxHealth;

				if (value >= maxHealth)
				{
					m_health = maxHealth;

					// We clean all damage dealers if we are fully healed, no special XP calculations need to be done.
					// May prevent players from gaining RPs after this living was healed to full?
					lock (m_xpGainers.SyncRoot)
					{
						m_xpGainers.Clear();
					}
				}
				else
					m_health = Math.Max(0, value);

				if (IsAlive && m_health < maxHealth)
					StartHealthRegeneration();
			}
		}

		public override int MaxHealth
		{
			get {	return GetModified(eProperty.MaxHealth); }
		}

		public virtual int Mana
		{
			get
			{
				return m_mana;
			}
			set
			{
				int maxmana = MaxMana;
				m_mana = Math.Min(value, maxmana);
				m_mana = Math.Max(m_mana, 0);
				if (IsAlive && (m_mana < maxmana || (this is GamePlayer && ((GamePlayer)this).CharacterClass.ID == (int)eCharacterClass.Vampiir)
				                || (this is GamePlayer && ((GamePlayer)this).CharacterClass.ID > 59 && ((GamePlayer)this).CharacterClass.ID < 63)))
				{
					StartPowerRegeneration();
				}
			}
		}

		public virtual int MaxMana
		{
			get
			{
				return GetModified(eProperty.MaxMana);
			}
		}

		public virtual byte ManaPercent
		{
			get
			{
				return (byte)(MaxMana <= 0 ? 0 : ((Mana * 100) / MaxMana));
			}
		}

		/// <summary>
		/// Gets/sets the object endurance
		/// </summary>
		public virtual int Endurance
		{
			get { return m_endurance; }
			set
			{
				m_endurance = Math.Min(value, m_maxEndurance);
				m_endurance = Math.Max(m_endurance, 0);
				if (IsAlive && m_endurance < m_maxEndurance)
				{
					StartEnduranceRegeneration();
				}
			}
		}

		/// <summary>
		/// Gets or sets the maximum endurance of this living
		/// </summary>
		public virtual int MaxEndurance
		{
			get { return m_maxEndurance; }
			set
			{
				m_maxEndurance = value;
				Endurance = Endurance; //cut extra end points if there are any or start regeneration
			}
		}

		/// <summary>
		/// Gets the endurance in percent of maximum
		/// </summary>
		public virtual byte EndurancePercent
		{
			get
			{
				return (byte)(MaxEndurance <= 0 ? 0 : ((Endurance * 100) / MaxEndurance));
			}
		}

		/// <summary>
		/// Gets/sets the object concentration
		/// </summary>
		public virtual int Concentration
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets/sets the object maxconcentration
		/// </summary>
		public virtual int MaxConcentration
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets the concentration in percent of maximum
		/// </summary>
		public virtual byte ConcentrationPercent
		{
			get
			{
				return (byte)(MaxConcentration <= 0 ? 0 : ((Concentration * 100) / MaxConcentration));
			}
		}

		

		/// <summary>
		/// Cancels all concentration effects by this living and on this living
		/// </summary>
		public void CancelAllConcentrationEffects(bool updateplayer = true)
		{
			CancelAllConcentrationEffects(false, updateplayer);
		}

		/// <summary>
		/// Cancels all concentration effects by this living and on this living
		/// </summary>
		public void CancelAllConcentrationEffects(bool leaveSelf, bool updateplayer)
		{
			// cancel conc spells
			for (int i = 0; i < effectListComponent.ConcentrationEffects.Count; i++)
            {
				EffectService.RequestCancelConcEffect(effectListComponent.ConcentrationEffects[i]);
            }

			//cancel all active conc spell effects from other casters
			if (effectListComponent != null)
			{
				foreach (var effect in effectListComponent.GetSpellEffects().Where(e => e.IsConcentrationEffect()))
				{
					if (!leaveSelf || (leaveSelf && effect.SpellHandler.Caster != this))
						EffectService.RequestCancelConcEffect((IConcentrationEffect)effect, false);
				}
			}
        }

        // 			ArrayList concEffects = new ArrayList();
        // 			lock (EffectList)
        // 			{
        // 				foreach (IGameEffect effect in EffectList)
        // 				{
        // 					if (effect is GameSpellEffect && ((GameSpellEffect)effect).Spell.Concentration > 0)
        // 					{
        // 						if (!leaveSelf || leaveSelf && ((GameSpellEffect)effect).SpellHandler.Caster != this)
        // 							concEffects.Add(effect);
        // 					}
        // 				}
        // 			}
        // 			foreach (GameSpellEffect effect in concEffects)
        // 			{
        // 				effect.Cancel(false);
        // 			}
        // 		}

        #endregion
        #region Speed/Heading/Target/GroundTarget/GuildName/SitState/Level
        /// <summary>
        /// The targetobject of this living
        /// This is a weak reference to a GameObject, which
        /// means that the gameobject can be cleaned up even
        /// when this living has a reference on it ...
        /// </summary>
        protected readonly WeakReference m_targetObjectWeakReference;

		/// <summary>
		/// Holds the Living's Coordinate inside the current Region
		/// </summary>
		protected Point3D m_groundTarget;

		/// <summary>
		/// Gets or sets the target of this living
		/// </summary>
		public virtual GameObject TargetObject
		{
			get
			{
				return (m_targetObjectWeakReference.Target as GameObject);
			}
			set
			{
				m_targetObjectWeakReference.Target = value;
			}
		}
		public virtual bool IsSitting
		{
			get { return false; }
			set { }
		}
		/// <summary>
		/// Gets the Living's ground-target Coordinate inside the current Region
		/// </summary>
		public virtual Point3D GroundTarget
		{
			get { return m_groundTarget; }
		}

		/// <summary>
		/// Sets the Living's ground-target Coordinates inside the current Region
		/// </summary>
		public virtual void SetGroundTarget(int groundX, int groundY, int groundZ)
		{
			m_groundTarget.X = groundX;
			m_groundTarget.Y = groundY;
			m_groundTarget.Z = groundZ;
		}

		/// <summary>
		/// Gets or Sets the current level of the Object
		/// </summary>
		public override byte Level
		{
			get { return base.Level; }
			set
			{
				base.Level = value;
				if (ObjectState == eObjectState.Active)
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (player == null)
							continue;
						player.Out.SendLivingDataUpdate(this, false);
					}
				}
			}
		}

		/// <summary>
		/// What is the base, unmodified level of this living
		/// </summary>
		public virtual byte BaseLevel
		{
			get { return Level; }
		}

		/// <summary>
		/// Calculates the level of a skill on this living.  Generally this is simply the level of the skill.
		/// </summary>
		public virtual int CalculateSkillLevel(Skill skill)
		{
			return skill.Level;
		}


		#endregion
		#region Movement
		public virtual void UpdateHealthManaEndu()
		{
			if (IsAlive)
			{
				if (Health < MaxHealth) StartHealthRegeneration();
				else if (Health > MaxHealth) Health = MaxHealth;

				if (Mana < MaxMana) StartPowerRegeneration();
				else if (Mana > MaxMana) Mana = MaxMana;

				if (Endurance < MaxEndurance) StartEnduranceRegeneration();
				else if (Endurance > MaxEndurance) Endurance = MaxEndurance;
			}
		}

		public virtual short CurrentSpeed
		{
			get => movementComponent.CurrentSpeed;
			set => movementComponent.CurrentSpeed = value;
		}

		public virtual bool IsTurningDisabled => movementComponent.IsTurningDisabled;

		public virtual short MaxSpeed => movementComponent.MaxSpeed;

		public virtual short MaxSpeedBase
		{
			get => movementComponent.MaxSpeedBase;
			set => movementComponent.MaxSpeedBase = value;
		}

		public virtual bool IsMoving => movementComponent.IsMoving;

		public virtual void DisableTurning(bool add)
		{
			movementComponent.DisableTurning(add);
		}

		public virtual bool IsStealthed => false;

		public virtual void Stealth(bool goStealth)
		{
			// Not implemented.
		}

		public virtual void OnMaxSpeedChange() { }

		#endregion
		#region Say/Yell/Whisper/Emote/Messages

		private bool m_isSilent = false;

		/// <summary>
		/// Can this living say anything?
		/// </summary>
		public virtual bool IsSilent
		{
			get { return m_isSilent; }
			set { m_isSilent = value; }
		}


		/// <summary>
		/// This function is called when this object receives a Say
		/// </summary>
		/// <param name="source">Source of say</param>
		/// <param name="str">Text that was spoken</param>
		/// <returns>true if the text should be processed further, false if it should be discarded</returns>
		public virtual bool SayReceive(GameLiving source, string str)
		{
			if (source == null || str == null)
			{
				return false;
			}
			
			Notify(GameLivingEvent.SayReceive, this, new SayReceiveEventArgs(source, this, str));
			
			return true;
		}

		/// <summary>
		/// Broadcasts a message to all living beings around this object
		/// </summary>
		/// <param name="str">string to broadcast (without any "xxx says:" in front!!!)</param>
		/// <returns>true if text was said successfully</returns>
		public virtual bool Say(string str)
		{
			if (str == null || IsSilent)
			{
				return false;
			}
			
			Notify(GameLivingEvent.Say, this, new SayEventArgs(str));
			
			foreach (GameNPC npc in GetNPCsInRadius(WorldMgr.SAY_DISTANCE))
			{
				GameNPC receiver = npc;
				// don't send say to the target, it will be whispered...
				if (receiver != this && receiver != TargetObject)
				{
					receiver.SayReceive(this, str);
				}
			}

			foreach (GameDoorBase door in GetDoorsInRadius(150))
			{
				if (door is GameKeepDoor && (str.Contains("enter") || str.Contains("exit")))
				{
					GameKeepDoor receiver = door as GameKeepDoor;
					if (this is GamePlayer)
					{
						receiver.SayReceive(this, str);
						break; //only want to Say to one door
					}
				}
			}
			
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
			{
				GamePlayer receiver = player;
				if (receiver != this)
				{
					receiver.SayReceive(this, str);
				}
			}
			
			// whisper to Targeted NPC.
			if (TargetObject != null && TargetObject is GameNPC)
			{
				GameNPC targetNPC = (GameNPC)TargetObject;
				targetNPC.WhisperReceive(this, str);
			}
			
			return true;
		}

		/// <summary>
		/// This function is called when the living receives a yell
		/// </summary>
		/// <param name="source">GameLiving that was yelling</param>
		/// <param name="str">string that was yelled</param>
		/// <returns>true if the string should be processed further, false if it should be discarded</returns>
		public virtual bool YellReceive(GameLiving source, string str)
		{
			if (source == null || str == null)
			{
				return false;
			}
			
			Notify(GameLivingEvent.YellReceive, this, new YellReceiveEventArgs(source, this, str));
			
			return true;
		}

		/// <summary>
		/// Broadcasts a message to all living beings around this object
		/// </summary>
		/// <param name="str">string to broadcast (without any "xxx yells:" in front!!!)</param>
		/// <returns>true if text was yelled successfully</returns>
		public virtual bool Yell(string str)
		{
			if (str == null || IsSilent)
			{
				return false;
			}
			
			Notify(GameLivingEvent.Yell, this, new YellEventArgs(str));
			
			foreach (GameNPC npc in GetNPCsInRadius(WorldMgr.YELL_DISTANCE))
			{
				GameNPC receiver = npc;
				if (receiver != this)
				{
					receiver.YellReceive(this, str);
				}
			}
			
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.YELL_DISTANCE))
			{
				GamePlayer receiver = player;
				if (receiver != this)
				{
					receiver.YellReceive(this, str);
				}
			}
			
			return true;
		}

		/// <summary>
		/// This function is called when the Living receives a whispered text
		/// </summary>
		/// <param name="source">GameLiving that was whispering</param>
		/// <param name="str">string that was whispered</param>
		/// <returns>true if the string should be processed further, false if it should be discarded</returns>
		public virtual bool WhisperReceive(GameLiving source, string str)
		{
			if (source == null || str == null)
			{
				return false;
			}

			GamePlayer player = null;
			if (source != null && source is GamePlayer)
			{
				player = source as GamePlayer;
				long whisperdelay = player.TempProperties.GetProperty<long>("WHISPERDELAY");
				if (whisperdelay > 0 && (GameLoop.GameLoopTime - 1500) < whisperdelay && player.Client.Account.PrivLevel == 1)
				{
					//player.Out.SendMessage("Speak slower!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					return false;
				}
				
				player.TempProperties.SetProperty("WHISPERDELAY", GameLoop.GameLoopTime);

				foreach (DOL.GS.Quests.DataQuest q in DataQuestList)
				{
					q.Notify(GamePlayerEvent.WhisperReceive, this, new WhisperReceiveEventArgs(player, this, str));
				}
			}

			Notify(GameLivingEvent.WhisperReceive, this, new WhisperReceiveEventArgs(source, this, str));

			return true;
		}

		/// <summary>
		/// Sends a whisper to a target
		/// </summary>
		/// <param name="target">The target of the whisper</param>
		/// <param name="str">text to whisper (without any "xxx whispers:" in front!!!)</param>
		/// <returns>true if text was whispered successfully</returns>
		public virtual bool Whisper(GameObject target, string str)
		{
			if (target == null || str == null || IsSilent)
			{
				return false;
			}
			
			if (!this.IsWithinRadius(target, WorldMgr.WHISPER_DISTANCE))
			{
				return false;
			}
			
			Notify(GameLivingEvent.Whisper, this, new WhisperEventArgs(target, str));
			
			if (target is GameLiving)
			{
				return ((GameLiving)target).WhisperReceive(this, str);
			}
			
			return false;
		}
		/// <summary>
		/// Makes this living do an emote-animation
		/// </summary>
		/// <param name="emote">the emote animation to show</param>
		public virtual void Emote(eEmote emote)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendEmoteAnimation(this, emote);
			}
		}

		/// <summary>
		/// A message to this living
		/// </summary>
		/// <param name="message"></param>
		/// <param name="type"></param>
		public virtual void MessageToSelf(string message, eChatType chatType)
		{
			// livings can't talk to themselves
		}

		#endregion
		#region Item/Money

		/// <summary>
		/// Called when the living is about to get an item from someone
		/// else
		/// </summary>
		/// <param name="source">Source from where to get the item</param>
		/// <param name="item">Item to get</param>
		/// <returns>true if the item was successfully received</returns>
		public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
		{
			if (source == null || item == null) return false;

			Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));

			//If the item has been removed by the event handlers : return
			if (item == null || item.OwnerID == null)
			{
				return true;
			}

			if (base.ReceiveItem(source, item) == false)
			{
				if (source is GamePlayer)
				{
					((GamePlayer)source).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)source).Client.Account.Language, "GameLiving.ReceiveItem", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}

				return false;
			}

			return true;
		}

		/// <summary>
		/// Called when the living is about to get money from someone
		/// else
		/// </summary>
		/// <param name="source">Source from where to get the money</param>
		/// <param name="money">array of money to get</param>
		/// <returns>true if the money was successfully received</returns>
		public override bool ReceiveMoney(GameLiving source, long money)
		{
			if (source == null || money <= 0) return false;

			Notify(GameLivingEvent.ReceiveMoney, this, new ReceiveMoneyEventArgs(source, this, money));

			if (source is GamePlayer)
				((GamePlayer)source).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)source).Client.Account.Language, "GameLiving.ReceiveMoney", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			//call base
			return base.ReceiveMoney(source, money);
		}
		#endregion
		#region Inventory
		/// <summary>
		/// Represent the inventory of all living
		/// </summary>
		protected IGameInventory m_inventory;

		/// <summary>
		/// Get/Set inventory
		/// </summary>
		public IGameInventory Inventory
		{
			get
			{
				return m_inventory;
			}
			set
			{
				m_inventory = value;
			}
		}
		#endregion
		#region Effects
		/// <summary>
		/// currently applied effects
		/// </summary>
		protected readonly GameEffectList m_effects;

		/// <summary>
		/// gets a list of active effects
		/// </summary>
		/// <returns></returns>
		public GameEffectList EffectList
		{
			get { return m_effects; }
		}

		/// <summary>
		/// Creates new effects list for this living.
		/// </summary>
		/// <returns>New effects list instance</returns>
		protected virtual GameEffectList CreateEffectsList()
		{
			return new GameEffectList(this);
		}

        #endregion
        #region Abilities

        /// <summary>
        /// Holds all abilities of the living (KeyName -> Ability)
        /// </summary>
        protected readonly Dictionary<string, Ability> m_abilities = new Dictionary<string, Ability>();

		protected readonly Object m_lockAbilities = new Object();

		/// <summary>
		/// Asks for existence of specific ability
		/// </summary>
		/// <param name="keyName">KeyName of ability</param>
		/// <returns>Does living have this ability</returns>
		public virtual bool HasAbility(string keyName)
		{
			bool hasit = false;
			
			lock (m_lockAbilities)
			{
				hasit = m_abilities.ContainsKey(keyName);
			}
			
			return hasit;
		}

		public bool HasAbilityType(Type type)
		{
			bool hasit = false;
			
			lock (m_lockAbilities)
			{
				hasit = (m_abilities.Values.Count(x => x.GetType() == type) > 0 ? true : false);
			}
			
			return hasit;
		}

		/// <summary>
		/// Add a new ability to a living
		/// </summary>
		/// <param name="ability"></param>
		public virtual void AddAbility(Ability ability)
		{
			AddAbility(ability, true);
		}

		/// <summary>
		/// Add or update an ability for this living
		/// </summary>
		/// <param name="ability"></param>
		/// <param name="sendUpdates"></param>
		public virtual void AddAbility(Ability ability, bool sendUpdates)
		{
			bool isNewAbility = false;
			lock (m_lockAbilities)
			{
				Ability oldAbility = null;
				m_abilities.TryGetValue(ability.KeyName, out oldAbility);
				
				if (oldAbility == null)
				{
					isNewAbility = true;
					m_abilities.Add(ability.KeyName, ability);
					ability.Activate(this, sendUpdates);
				}
				else
				{
					int oldLevel = oldAbility.Level;
					oldAbility.Level = ability.Level;
					
					isNewAbility |= oldAbility.Level > oldLevel;
				}
				
				if (sendUpdates && (isNewAbility && (this is GamePlayer)))
				{
					(this as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((this as GamePlayer).Client.Account.Language, "GamePlayer.AddAbility.YouLearn", ability.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
		}

		/// <summary>
		/// Remove an ability from this living
		/// </summary>
		/// <param name="abilityKeyName"></param>
		/// <returns></returns>
		public virtual bool RemoveAbility(string abilityKeyName)
		{
			Ability ability = null;
			lock (m_lockAbilities)
			{
				m_abilities.TryGetValue(abilityKeyName, out ability);
				
				if (ability == null)
					return false;
				
				ability.Deactivate(this, true);
				m_abilities.Remove(ability.KeyName);
			}
			
			if (this is GamePlayer)
				(this as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((this as GamePlayer).Client.Account.Language, "GamePlayer.RemoveAbility.YouLose", ability.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			return true;
		}

		/// <summary>
		/// returns ability of living or null if non existent
		/// </summary>
		/// <param name="abilityKey"></param>
		/// <returns></returns>
		public Ability GetAbility(string abilityKey)
		{
			Ability ab = null;
			lock (m_lockAbilities)
			{
				m_abilities.TryGetValue(abilityKey, out ab);
			}
			
			return ab;
		}

		/// <summary>
		/// returns ability of living or null if no existant
		/// </summary>
		/// <returns></returns>
		public T GetAbility<T>() where T : Ability
		{
			T tmp;
			lock (m_lockAbilities)
			{
				tmp = (T)m_abilities.Values.FirstOrDefault(a => a.GetType().Equals(typeof(T)));
			}
			
			return tmp;
		}

		/// <summary>
		/// returns ability of living or null if no existant
		/// </summary>
		/// <param name="abilityType"></param>
		/// <returns></returns>
		[Obsolete("Use GetAbility<T>() instead")]
		public Ability GetAbility(Type abilityType)
		{
			lock (m_lockAbilities)
			{
				foreach (Ability ab in m_abilities.Values)
				{
					if (ab.GetType().Equals(abilityType))
						return ab;
				}
			}
			return null;
		}

		/// <summary>
		/// returns the level of ability
		/// if 0 is returned, the ability is non existent on living
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public int GetAbilityLevel(string keyName)
		{
			Ability ab = null;
			
			lock (m_lockAbilities)
			{
				m_abilities.TryGetValue(keyName, out ab);
			}
			
			if (ab == null)
				return 0;

			return Math.Max(1, ab.Level);
		}

		/// <summary>
		/// returns all abilities in a copied list
		/// </summary>
		/// <returns></returns>
		public IList GetAllAbilities()
		{
			List<Ability> list = new List<Ability>();
			lock (m_lockAbilities)
			{
				list = new List<Ability>(m_abilities.Values);
			}
			
			return list;
		}

		#endregion Abilities

		/// <summary>
		/// Checks if living has ability to use items of this type
		/// </summary>
		/// <param name="item"></param>
		/// <returns>true if living has ability to use item</returns>
		public virtual bool HasAbilityToUseItem(DbItemTemplate item)
		{
			return GameServer.ServerRules.CheckAbilityToUseItem(this, item);
		}

		/// <summary>
		/// Table of skills currently disabled
		/// skill => disabletimeout (ticks) or 0 when endless
		/// </summary>
		private readonly Dictionary<KeyValuePair<int, Type>, KeyValuePair<long, Skill>> m_disabledSkills = new Dictionary<KeyValuePair<int, Type>, KeyValuePair<long, Skill>>();

		/// <summary>
		/// Gets the time left for disabling this skill in milliseconds
		/// </summary>
		/// <param name="skill"></param>
		/// <returns>milliseconds left for disable</returns>
		public virtual int GetSkillDisabledDuration(Skill skill)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				KeyValuePair<int, Type> key = new(skill.ID, skill.GetType());

				if (m_disabledSkills.TryGetValue(key, out KeyValuePair<long, Skill> value))
				{
					long timeout = value.Key;
					long left = timeout - GameLoop.GameLoopTime;

					if (left <= 0)
					{
						left = 0;
						m_disabledSkills.Remove(key);
					}

					return (int) left;
				}
			}

			return 0;
		}

		/// <summary>
		/// Gets a copy of all disabled skills
		/// </summary>
		/// <returns></returns>
		public virtual ICollection<Skill> GetAllDisabledSkills()
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				List<Skill> skillList = new List<Skill>();
				
				foreach(KeyValuePair<long, Skill> disabled in m_disabledSkills.Values)
					skillList.Add(disabled.Value);
				
				return skillList;
			}
		}

		/// <summary>
		/// Grey out some skills on client for specified duration
		/// </summary>
		/// <param name="skill">the skill to disable</param>
		/// <param name="duration">duration of disable in milliseconds</param>
		public virtual void DisableSkill(Skill skill, int duration)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				KeyValuePair<int, Type> key = new(skill.ID, skill.GetType());

				if (duration > 0)
					m_disabledSkills[key] = new KeyValuePair<long, Skill>(GameLoop.GameLoopTime + duration, skill);
				else
					m_disabledSkills.Remove(key);
			}
		}

		/// <summary>
		/// Grey out collection of skills on client for specified duration
		/// </summary>
		/// <param name="skill">the skill to disable</param>
		/// <param name="duration">duration of disable in milliseconds</param>
		public virtual void DisableSkills(ICollection<Tuple<Skill, int>> skills)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				foreach (Tuple<Skill, int> tuple in skills)
				{
					Skill skill = tuple.Item1;
					int duration = tuple.Item2;
					
					KeyValuePair<int, Type> key = new(skill.ID, skill.GetType());

					if (duration > 0)
						m_disabledSkills[key] = new KeyValuePair<long, Skill>(GameLoop.GameLoopTime + duration, skill);
					else
						m_disabledSkills.Remove(key);
				}
			}
		}

		/// <summary>
		/// Removes Greyed out skills
		/// </summary>
		/// <param name="skill">the skill to remove</param>
		public virtual void RemoveDisabledSkill(Skill skill)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				m_disabledSkills.Remove(new(skill.ID, skill.GetType()));
			}
		}

		#region Broadcasting utils

		/// <summary>
		/// Broadcasts the living equipment to all players around
		/// </summary>
		public virtual void BroadcastLivingEquipmentUpdate()
		{
			if (ObjectState != eObjectState.Active)
				return;
			
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null)
					continue;
				
				player.Out.SendLivingEquipmentUpdate(this);
			}
		}
		
		#endregion
		
		#region Region

		/// <summary>
		/// Removes the item from the world
		/// </summary>
		public override bool RemoveFromWorld()
		{
			if (!base.RemoveFromWorld())
				return false;

			attackComponent.StopAttack();

			foreach (GameObject attacker in attackComponent.Attackers.Keys)
			{
				if (attacker is not GameLiving attackerLiving)
					continue;

				attackerLiving.EnemyKilled(this);
			}

			attackComponent.Attackers.Clear();
			StopHealthRegeneration();
			StopPowerRegeneration();
			StopEnduranceRegeneration();
			attackComponent.attackAction.CleanUp();
			m_healthRegenerationTimer?.Stop();
			m_powerRegenerationTimer?.Stop();
			m_enduRegenerationTimer?.Stop();
			m_healthRegenerationTimer = null;
			m_powerRegenerationTimer = null;
			m_enduRegenerationTimer = null;

			return true;
		}

		#endregion
		#region Spell Cast
		/// <summary>
		/// Multiplier for melee and magic.
		/// </summary>
		public virtual double Effectiveness
		{
			get { return 1.0; }
			set { }
		}

		public virtual bool IsCasting => castingComponent.IsCasting;

		/// <summary>
		/// Returns true if the living has the spell effect, else false.
		/// </summary>
		/// <param name="spell"></param>
		/// <returns></returns>
		public override bool HasEffect(Spell spell)
		{
			lock (EffectList)
			{
				foreach (IGameEffect effect in EffectList)
				{
					if (effect is GameSpellEffect)
					{
						GameSpellEffect spellEffect = effect as GameSpellEffect;

						if (spellEffect.Spell.SpellType == spell.SpellType &&
						    spellEffect.Spell.EffectGroup == spell.EffectGroup)
							return true;
					}
				}
			}

			return base.HasEffect(spell);
		}

		/// <summary>
		/// Checks if the target has a type of effect
		/// </summary>
		/// <param name="target"></param>
		/// <param name="spell"></param>
		/// <returns></returns>
		public override bool HasEffect(Type effectType)
		{
			lock (EffectList)
			{
				foreach (IGameEffect effect in EffectList)
					if (effect.GetType() == effectType)
						return true;
			}

			return base.HasEffect(effectType);
		}

		/// <summary>
		/// Active spellhandler or null
		/// </summary>
		public ISpellHandler CurrentSpellHandler => castingComponent.SpellHandler;

		/// <summary>
		/// Immediately stops currently casting spell
		/// </summary>
		public virtual void StopCurrentSpellcast()
		{
			castingComponent.InterruptCasting();
		}

		public virtual bool CastSpell(Spell spell, SpellLine line, ISpellCastingAbilityHandler spellCastingAbilityHandler = null)
		{
			return castingComponent.RequestStartCastSpell(spell, line, spellCastingAbilityHandler, TargetObject as GameLiving);
		}

		// Should only be used when the target of the spell is different than the currently selected one.
		// Which can happen during LoS checks, since we're not waiting for the check to complete to perform other actions.
		protected bool CastSpell(Spell spell, SpellLine line, GameLiving target, ISpellCastingAbilityHandler spellCastingAbilityHandle = null)
		{
			return castingComponent.RequestStartCastSpell(spell, line, spellCastingAbilityHandle, target);
		}

		public virtual bool CastSpell(ISpellCastingAbilityHandler ab)
		{
			ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(this, ab.Spell, ab.SpellLine);

			if (spellhandler != null)
			{
				spellhandler.Ability = ab;
				return spellhandler.StartSpell(this);
			}

			return false;
		}

		#endregion
		#region LoadCalculators
		/// <summary>
		/// Load the property calculations
		/// </summary>
		/// <returns></returns>
		public static bool LoadCalculators()
		{
			try
			{
				foreach (Assembly asm in ScriptMgr.GameServerScripts)
				{
					foreach (Type t in asm.GetTypes())
					{
						try
						{
							if (!t.IsClass || t.IsAbstract) continue;
							if (!typeof(IPropertyCalculator).IsAssignableFrom(t)) continue;
							IPropertyCalculator calc = (IPropertyCalculator)Activator.CreateInstance(t);
							foreach (PropertyCalculatorAttribute attr in t.GetCustomAttributes(typeof(PropertyCalculatorAttribute), false))
							{
								for (int i = (int)attr.Min; i <= (int)attr.Max; i++)
								{
									m_propertyCalc[i] = calc;
								}
							}
						}
						catch (Exception e)
						{
							if (log.IsErrorEnabled)
								log.Error("Error while working with type " + t.FullName, e);
						}
					}
				}
				return true;
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("GameLiving.LoadCalculators()", e);
				return false;
			}
		}
		#endregion
		#region ControlledNpc

		private int m_petCount;
		public int PetCount => m_petCount;

		public void UpdatePetCount(bool add)
		{
			if (add)
				Interlocked.Increment(ref m_petCount);
			else
				Interlocked.Decrement(ref m_petCount);
		}

		/// <summary>
		/// Holds the controlled object
		/// </summary>
		protected IControlledBrain[] m_controlledBrain = null;

		/// <summary>
		/// Initializes the ControlledNpcs for the GameLiving class
		/// </summary>
		/// <param name="num">Number of places to allocate.  If negative, sets to null.</param>
		public virtual void InitControlledBrainArray(int num)
		{
			if (num > 0)
			{
				m_controlledBrain = new IControlledBrain[num];
			}
			else
			{
				m_controlledBrain = null;
			}
		}

		/// <summary>
		/// Get or set the ControlledBrain.  Set always uses m_controlledBrain[0]
		/// </summary>
		public virtual IControlledBrain ControlledBrain
		{
			get
			{
				if (m_controlledBrain == null)
					return null;

				return m_controlledBrain[0];
			}
			set
			{
				m_controlledBrain[0] = value;
			}
		}

		public virtual bool IsControlledNPC(GameNPC npc)
		{
			if (npc == null)
			{
				return false;
			}
			IControlledBrain brain = npc.Brain as IControlledBrain;
			if (brain == null)
			{
				return false;
			}
			return brain.GetLivingOwner() == this;
		}

		/// <summary>
		/// Sets the controlled object for this player
		/// </summary>
		/// <param name="controlledNpc"></param>
		public virtual void SetControlledBrain(IControlledBrain controlledBrain)
		{
		}

		#endregion
		#region Group
		/// <summary>
		/// Holds the group of this living
		/// </summary>
		protected Group m_group;
		/// <summary>
		/// Holds the index of this living inside of the group
		/// </summary>
		protected byte m_groupIndex;

		/// <summary>
		/// Gets or sets the living's group
		/// </summary>
		public Group Group
		{
			get { return m_group; }
			set { m_group = value; }
		}

		/// <summary>
		/// Gets or sets the index of this living inside of the group
		/// </summary>
		public byte GroupIndex
		{
			get { return m_groupIndex; }
			set { m_groupIndex = value; }
		}
		#endregion
		
		/// <summary>
		/// Handle event notifications.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			if (e == GameLivingEvent.Interrupted && args != null)
			{
				if (CurrentSpellHandler != null)
					CurrentSpellHandler.CasterIsAttacked((args as InterruptedEventArgs).Attacker);

				return;
			}

			base.Notify(e, sender, args);
		}
		
		/// <summary>
		/// Constructor to create a new GameLiving
		/// </summary>
		public GameLiving() : base()
		{
			attackComponent = new AttackComponent(this);
			rangeAttackComponent = new RangeAttackComponent(this);
			styleComponent = new StyleComponent(this);
			castingComponent = CastingComponent.Create(this);
			effectListComponent = new EffectListComponent(this);
			movementComponent = MovementComponent.Create(this);
			healthComponent = new HealthComponent(this);

			m_guildName = string.Empty;
			m_targetObjectWeakReference = new WeakRef(null);
			m_groundTarget = new Point3D(0, 0, 0);

			//Set all combat properties
			m_activeWeaponSlot = eActiveWeaponSlot.Standard;
			rangeAttackComponent.ActiveQuiverSlot = eActiveQuiverSlot.None;
			rangeAttackComponent.RangedAttackState = eRangedAttackState.None;
			rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
			m_xpGainers = new HybridDictionary();
			m_effects = CreateEffectsList();

			m_health = 1;
			m_mana = 1;
			m_endurance = 1;
			m_maxEndurance = 1;
		}
	}
}
