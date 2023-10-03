﻿using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;

namespace DOL.GS
{
	/// <summary>
	/// Holds all data for an Attack
	/// </summary>
	public class AttackData
	{
		private GameLiving m_attacker = null;
		private GameLiving m_target = null;
		private eArmorSlot m_hitArmorSlot = eArmorSlot.NOTSET;
		private int m_damage = 0;
		private int m_critdamage = 0;
		private int m_styledamage = 0;
		private int m_modifier = 0;
		private eDamageType m_damageType = 0;
		private Style m_style = null;
		private eAttackType m_attackType = eAttackType.Unknown;
		private eAttackResult m_attackResult = eAttackResult.Any;
		private ISpellHandler m_spellHandler;
		private List<ISpellHandler> m_styleEffects;
		private int m_animationId;
		private int m_weaponSpeed;
		private bool m_isOffHand;
		private DbInventoryItem m_weapon;
		private bool m_isSpellResisted = false;
		private bool m_causesCombat = true;
		private double m_missRate = 0;
		private double m_parryChance = 0;
		private double m_evadeChance = 0;
		private double m_blockChance = 0;

		/// <summary>
		/// Constructs new AttackData
		/// </summary>
		public AttackData()
		{
			m_styleEffects = new List<ISpellHandler>();
		}
		public double MissRate
        {
			get { return m_missRate; }
			set { m_missRate = value; }
        }
		public double ParryChance
		{
			get { return m_parryChance; }
			set { m_parryChance = value * 100; }
		}
		public double EvadeChance
		{
			get { return m_evadeChance; }
			set { m_evadeChance = value * 100; }
		}
		public double BlockChance
		{
			get { return m_blockChance; }
			set { m_blockChance = value * 100; }
		}
		/// <summary>
		/// Sets or gets the modifier (resisted damage)
		/// </summary>
		public int Modifier
		{
			get { return m_modifier; }
			set { m_modifier = value; }
		}

		/// <summary>
		/// Was the spell resisted
		/// </summary>
		public bool IsSpellResisted
		{
			get
			{
				if (SpellHandler == null)
					return false;
				return m_isSpellResisted;
			}
			set { m_isSpellResisted = value; }
		}

		/// <summary>
		/// Sets or gets the attacker
		/// </summary>
		public GameLiving Attacker
		{
			get { return m_attacker; }
			set { m_attacker = value; }
		}

		/// <summary>
		/// Sets or gets the attack target
		/// </summary>
		public GameLiving Target
		{
			get { return m_target; }
			set { m_target = value; }
		}

		/// <summary>
		/// Sets or gets the armor hit location
		/// </summary>
		public eArmorSlot ArmorHitLocation
		{
			get { return m_hitArmorSlot; }
			set { m_hitArmorSlot = value; }
		}

		/// <summary>
		/// Sets or gets the damage
		/// </summary>
		public int Damage
		{
			get { return m_damage; }
			set { m_damage = value; }
		}

		/// <summary>
		/// Sets or gets the critical damage
		/// </summary>
		public int CriticalDamage
		{
			get { return m_critdamage; }
			set { m_critdamage = value; }
		}

		/// <summary>
		/// Sets or gets the style damage
		/// </summary>
		public int StyleDamage
		{
			get { return m_styledamage; }
			set { m_styledamage = value; }
		}

		/// <summary>
		/// Sets or gets the damage type
		/// </summary>
		public eDamageType DamageType
		{
			get { return m_damageType; }
			set { m_damageType = value; }
		}

		/// <summary>
		/// Sets or gets the style used
		/// </summary>
		public Style Style
		{
			get { return m_style; }
			set { m_style = value; }
		}

		/// <summary>
		/// Sets or gets the attack result
		/// </summary>
		public eAttackResult AttackResult
		{
			get { return m_attackResult; }
			set { m_attackResult = value; }
		}

		///// <summary>
		///// Sets or gets the attack spellhandler
		///// </summary>
		public ISpellHandler SpellHandler
		{
			get { return m_spellHandler; }
			set { m_spellHandler = value; }
		}

		/// <summary>
		/// (procs) Gets the style effects
		/// </summary>
		public List<ISpellHandler> StyleEffects
		{
			get { return m_styleEffects; }
		}

		/// <summary>
		/// Sets or gets the weapon speed
		/// </summary>
		public int WeaponSpeed
		{
			get { return m_weaponSpeed; }
			set { m_weaponSpeed = value; }
		}

		/// <summary>
		/// Checks whether attack type is one of melee types
		/// </summary>
		public bool IsMeleeAttack
		{
			get
			{
				return m_attackType == eAttackType.MeleeOneHand
					|| m_attackType == eAttackType.MeleeTwoHand
					|| m_attackType == eAttackType.MeleeDualWield;
			}
		}

		public bool IsOffHand
		{
			get { return m_isOffHand; }
			set { m_isOffHand = value; }
		}

		public DbInventoryItem Weapon
		{
			get { return m_weapon; }
			set { m_weapon = value; }
		}

		/// <summary>
		/// The type of attack
		/// </summary>
		public enum eAttackType : int
		{
			/// <summary>
			/// Attack type has not been set yet
			/// </summary>
			Unknown = -1,
			/// <summary>
			/// Attack is done using a weapon in one hand
			/// </summary>
			MeleeOneHand = 1,
			/// <summary>
			/// Attack is done using one weapon in each hand
			/// </summary>
			MeleeDualWield = 2,
			/// <summary>
			/// Attack is done using one same weapon in both hands
			/// </summary>
			MeleeTwoHand = 3,
			/// <summary>
			/// Attack is done using a weapon in ranged slot
			/// </summary>
			Ranged = 4,
			/// <summary>
			/// Attack is done with a spell
			/// </summary>
			Spell = 5,
		}

		/// <summary>
		/// Sets or gets the attack type
		/// </summary>
		public eAttackType AttackType
		{
			get { return m_attackType; }
			set { m_attackType = value; }
		}

		/// <summary>
		/// Sets or gets the attack animation ID
		/// </summary>
		public int AnimationId
		{
			get { return m_animationId; }
			set { m_animationId = value; }
		}

		/// <summary>
		/// Method to determine if an attack result, resulted in a hit
		/// </summary>
		/// <returns>true if it was a hit</returns>
		public bool IsHit
		{
			get
			{
				switch (m_attackResult)
				{
					case eAttackResult.HitUnstyled:
					case eAttackResult.HitStyle:
					case eAttackResult.Missed:
					case eAttackResult.Blocked:
					case eAttackResult.Evaded:
					case eAttackResult.Fumbled:
					case eAttackResult.Parried: return true;
					default: return false;
				}
			}
		}

		public bool IsRandomFumble
		{
			get
			{
				GamePlayer playerAttacker = Attacker as GamePlayer;
				double fumbleChance = Attacker.ChanceToFumble;
				double fumbleRoll;

				if (!ServerProperties.Properties.OVERRIDE_DECK_RNG && playerAttacker != null)
					fumbleRoll = playerAttacker.RandomNumberDeck.GetPseudoDouble();
				else
					fumbleRoll = Util.CryptoNextDouble();

				if (playerAttacker?.UseDetailedCombatLog == true)
					playerAttacker.Out.SendMessage($"Your chance to fumble: {fumbleChance * 100:0.##}% rand: {fumbleRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

				return IsMeleeAttack && fumbleChance > fumbleRoll;
			}
		}

		/// <summary>
		/// Does this attack put the living in combat?
		/// </summary>
		public bool CausesCombat
		{
			get { return m_causesCombat; }
			set { m_causesCombat = value; }
		}
	}
}
