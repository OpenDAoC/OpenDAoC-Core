﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS.Scripts
{
    public class BuffMerchantEvent : GameMerchant
    {
#region BuffMerchant attrib/spells/casting
        public BuffMerchantEvent()
            : base()
        {
            Flags |= GameNPC.eFlags.PEACE;
        }

        public override int Concentration
        {
            get
            {
                return 10000;
            }
        }

        public override int Mana
        {
            get
            {
                return 10000;
            }
        }

        private Queue m_buffs = new Queue();
        private const int BUFFS_SPELL_DURATION = 1800;
        private const bool BUFFS_PLAYER_PET = true;

        public override bool AddToWorld()
        {
            Level = 50;
            return base.AddToWorld();
        }
        public void BuffPlayer(GamePlayer player, Spell spell, SpellLine spellLine)
        {
            if (m_buffs == null) m_buffs = new Queue();

            m_buffs.Enqueue(new Container(spell, spellLine, player));

            //don't forget his pet !
            if (BUFFS_PLAYER_PET && player.ControlledBrain != null)
            {
                if (player.ControlledBrain.Body != null)
                {
                    m_buffs.Enqueue(new Container(spell, spellLine, player.ControlledBrain.Body));
                }
            }

            CastBuffs();

        }
        public void CastBuffs()
        {
            Container con = null;
            while (m_buffs.Count > 0)
            {
                con = (Container)m_buffs.Dequeue();

                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(this, con.Spell, con.SpellLine);

                if (spellHandler != null)
                {
                    spellHandler.StartSpell(con.Target);
                }
            }
        }

        #region SpellCasting

        private static SpellLine m_MerchBaseSpellLine;
        private static SpellLine m_MerchSpecSpellLine;
        private static SpellLine m_MerchOtherSpellLine;

        /// <summary>
        /// Spell line used by Merchs
        /// </summary>
        public static SpellLine MerchBaseSpellLine
        {
            get
            {
                if (m_MerchBaseSpellLine == null)
                    m_MerchBaseSpellLine = new SpellLine("MerchBaseSpellLine", "BuffMerch Spells", "unknown", true);

                return m_MerchBaseSpellLine;
            }
        }
        public static SpellLine MerchSpecSpellLine
        {
            get
            {
                if (m_MerchSpecSpellLine == null)
                    m_MerchSpecSpellLine = new SpellLine("MerchSpecSpellLine", "BuffMerch Spells", "unknown", false);

                return m_MerchSpecSpellLine;
            }
        }
        public static SpellLine MerchOtherSpellLine
        {
            get
            {
                if (m_MerchOtherSpellLine == null)
                    m_MerchOtherSpellLine = new SpellLine("MerchOtherSpellLine", "BuffMerch Spells", "unknown", true);

                return m_MerchOtherSpellLine;
            }
        }

        private static Spell m_baseaf;
        private static Spell m_basestr;
        private static Spell m_basecon;
        private static Spell m_basedex;
        private static Spell m_strcon;
        private static Spell m_dexqui;
        private static Spell m_acuity;
        private static Spell m_specaf;
        private static Spell m_casterbaseaf;
        private static Spell m_casterbasestr;
        private static Spell m_casterbasecon;
        private static Spell m_casterbasedex;
        private static Spell m_casterstrcon;
        private static Spell m_casterdexqui;
        private static Spell m_casteracuity;
        private static Spell m_casterspecaf;
        private static Spell m_haste;
        #region Non-live (commented out)
        //private static Spell m_powereg;
        //private static Spell m_dmgadd;
        //private static Spell m_hpRegen;
        //private static Spell m_heal;
        #endregion None-live (commented out)

        #region Spells

        /// <summary>
        /// Merch Base AF buff (VERIFIED)
        /// </summary>
        public static Spell MerchBaseAFBuff
        {
            get
            {
                if (m_baseaf == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 1467;
                    spell.Icon = 1467;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //Effective buff 58
                    spell.Name = "Armor of the Realm";
                    spell.Description = "Adds to the recipient's Armor Factor (AF) resulting in better protection against some forms of attack. It acts in addition to any armor the target is wearing.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88001;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Base Armor Factor by 20.";
                    spell.Type = eSpellType.BaseArmorFactorBuff.ToString();
                    spell.EffectGroup = 1;

                    m_baseaf = new Spell(spell, 39);
                }
                return m_baseaf;
            }
        }
        /// <summary>
        /// Merch Caster Base AF buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchBaseAFBuff
        {
            get
            {
                if (m_casterbaseaf == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5017;
                    spell.Icon = 5017;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //Effective buff 58
                    spell.Name = "Armor of the Realm";
                    spell.Description = "Adds to the recipient's Armor Factor (AF) resulting in better protection against some forms of attack. It acts in addition to any armor the target is wearing.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89001;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Base Armor Factor by 20.";
                    spell.Type = eSpellType.BaseArmorFactorBuff.ToString();
                    spell.EffectGroup = 1;

                    m_casterbaseaf = new Spell(spell, 39);
                }
                return m_casterbaseaf;
            }
        }
        /// <summary>
        /// Merch Base Str buff (VERIFIED)
        /// </summary>
        public static Spell MerchStrBuff
        {
            get
            {
                if (m_basestr == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5004;
                    spell.Icon = 5004;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //effective buff 55
                    spell.Name = "Strength of the Realm";
                    spell.Description = "Increases target's Strength.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88002;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Strength by 20.";
                    spell.Type = eSpellType.StrengthBuff.ToString();
                    spell.EffectGroup = 4;

                    m_basestr = new Spell(spell, 39);
                }
                return m_basestr;
            }
        }
        /// <summary>
        /// Merch Caster Base Str buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchStrBuff
        {
            get
            {
                if (m_casterbasestr == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5004;
                    spell.Icon = 5004;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //effective buff 55
                    spell.Name = "Strength of the Realm";
                    spell.Description = "Increases target's Strength.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89002;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Strength by 20.";
                    spell.Type = eSpellType.StrengthBuff.ToString();
                    spell.EffectGroup = 4;

                    m_casterbasestr = new Spell(spell, 39);
                }
                return m_casterbasestr;
            }
        }
        /// <summary>
        /// Merch Base Con buff (VERIFIED)
        /// </summary>
        public static Spell MerchConBuff
        {
            get
            {
                if (m_basecon == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5034;
                    spell.Icon = 5034;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //effective buff 55
                    spell.Name = "Fortitude of the Realm";
                    spell.Description = "Increases target's Constitution.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88003;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Constitution by 20.";
                    spell.Type = eSpellType.ConstitutionBuff.ToString();
                    spell.EffectGroup = 201;

                    m_basecon = new Spell(spell, 39);
                }
                return m_basecon;
            }
        }
        /// <summary>
        /// Merch Caster Base Con buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchConBuff
        {
            get
            {
                if (m_casterbasecon == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5034;
                    spell.Icon = 5034;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //effective buff 55
                    spell.Name = "Fortitude of the Realm";
                    spell.Description = "Increases target's Constitution.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89003;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Constitution by 20.";
                    spell.Type = eSpellType.ConstitutionBuff.ToString();
                    spell.EffectGroup = 201;

                    m_casterbasecon = new Spell(spell, 39);
                }
                return m_casterbasecon;
            }
        }
        /// <summary>
        /// Merch Base Dex buff (VERIFIED)
        /// </summary>
        public static Spell MerchDexBuff
        {
            get
            {
                if (m_basedex == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5024;
                    spell.Icon = 5024;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //effective buff 55
                    spell.Name = "Dexterity of the Realm";
                    spell.Description = "Increases Dexterity for a character.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88004;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Dexterity by 20.";
                    spell.Type = eSpellType.DexterityBuff.ToString();
                    spell.EffectGroup = 202;

                    m_basedex = new Spell(spell, 39);
                }
                return m_basedex;
            }
        }
        /// <summary>
        /// Merch Caster Base Dex buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchDexBuff
        {
            get
            {
                if (m_casterbasedex == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5024;
                    spell.Icon = 5024;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 20; //effective buff 55
                    spell.Name = "Dexterity of the Realm";
                    spell.Description = "Increases Dexterity for a character.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89004;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Dexterity by 20.";
                    spell.Type = eSpellType.DexterityBuff.ToString();
                    spell.EffectGroup = 202;

                    m_casterbasedex = new Spell(spell, 39);
                }
                return m_casterbasedex;
            }
        }
        /// <summary>
        /// Merch Spec Str/Con buff (VERIFIED)
        /// </summary>
        public static Spell MerchStrConBuff
        {
            get
            {
                if (m_strcon == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5065;
                    spell.Icon = 5065;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 85
                    spell.Name = "Might of the Realm";
                    spell.Description = "Increases Str/Con for a character";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88005;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Str/Con by 35.";
                    spell.Type = eSpellType.StrengthConstitutionBuff.ToString();
                    spell.EffectGroup = 204;

                    m_strcon = new Spell(spell, 39);
                }
                return m_strcon;
            }
        }
        /// <summary>
        /// Merch Caster Spec Str/Con buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchStrConBuff
        {
            get
            {
                if (m_casterstrcon == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5065;
                    spell.Icon = 5065;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 85
                    spell.Name = "Might of the Realm";
                    spell.Description = "Increases Str/Con for a character";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89005;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Str/Con by 35.";
                    spell.Type = eSpellType.StrengthConstitutionBuff.ToString();
                    spell.EffectGroup = 204;

                    m_casterstrcon = new Spell(spell, 39);
                }
                return m_casterstrcon;
            }
        }
        /// <summary>
        /// Merch Spec Dex/Qui buff (VERIFIED)
        /// </summary>
        public static Spell MerchDexQuiBuff
        {
            get
            {
                if (m_dexqui == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5074;
                    spell.Icon = 5074;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 85
                    spell.Name = "Deftness of the Realm";
                    spell.Description = "Increases Dexterity and Quickness for a character.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88006;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Dex/Qui by 35.";
                    spell.Type = eSpellType.DexterityQuicknessBuff.ToString();
                    spell.EffectGroup = 203;

                    m_dexqui = new Spell(spell, 39);
                }
                return m_dexqui;
            }
        }
        /// <summary>
        /// Merch Caster Spec Dex/Qui buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchDexQuiBuff
        {
            get
            {
                if (m_casterdexqui == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5074;
                    spell.Icon = 5074;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 85
                    spell.Name = "Deftness of the Realm";
                    spell.Description = "Increases Dexterity and Quickness for a character.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89006;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Dex/Qui by 35.";
                    spell.Type = eSpellType.DexterityQuicknessBuff.ToString();
                    spell.EffectGroup = 203;

                    m_casterdexqui = new Spell(spell, 39);
                }
                return m_casterdexqui;
            }
        }
        /// <summary>
        /// Merch Spec Acuity buff (VERIFIED)
        /// </summary>
        public static Spell MerchAcuityBuff
        {
            get
            {
                if (m_acuity == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5078;
                    spell.Icon = 5078;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 72;
                    spell.Name = "Acuity of the Realm";
                    spell.Description = "Increases Acuity (casting attribute) for a character.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88007;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Acuity by 35.";
                    spell.Type = eSpellType.AcuityBuff.ToString();
                    spell.EffectGroup = 200;

                    m_acuity = new Spell(spell, 39);
                }
                return m_acuity;
            }
        }
        /// <summary>
        /// Merch Caster Spec Acuity buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchAcuityBuff
        {
            get
            {
                if (m_casteracuity == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5078;
                    spell.Icon = 5078;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 72;
                    spell.Name = "Acuity of the Realm";
                    spell.Description = "Increases Acuity (casting attribute) for a character.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89007;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Acuity by 35.";
                    spell.Type = eSpellType.AcuityBuff.ToString();
                    spell.EffectGroup = 200;

                    m_casteracuity = new Spell(spell, 39);
                }
                return m_casteracuity;
            }
        }
        /// <summary>
        /// Merch Spec Af buff (VERIFIED)
        /// </summary>
        public static Spell MerchSpecAFBuff
        {
            get
            {
                if (m_specaf == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 3155;
                    spell.Icon = 3155;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 67
                    spell.Name = "Armor of the Realm";
                    spell.Description = "Adds 35 to the recipient's Armor Factor (AF), resulting in better protection against some forms of attack. It acts in addition to any armor the target is wearing.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88014;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Armor Factor by 35.";
                    spell.Type = eSpellType.SpecArmorFactorBuff.ToString();
                    spell.EffectGroup = 2;

                    m_specaf = new Spell(spell, 39);
                }
                return m_specaf;
            }
        }
        /// <summary>
        /// Merch Caster Spec Af buff (VERIFIED)
        /// </summary>
        public static Spell casterMerchSpecAFBuff
        {
            get
            {
                if (m_casterspecaf == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 3155;
                    spell.Icon = 3155;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 35; //effective buff 67
                    spell.Name = "Armor of the Realm";
                    spell.Description = "Adds to the recipient's Armor Factor (AF), resulting in better protection against some forms of attack. It acts in addition to any armor the target is wearing.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 89014;
                    spell.Target = "Realm";
                    spell.Message1 = "Increases target's Armor Factor by 35.";
                    spell.Type = eSpellType.SpecArmorFactorBuff.ToString();
                    spell.EffectGroup = 2;

                    m_casterspecaf = new Spell(spell, 39);
                }
                return m_casterspecaf;
            }
        }
        /// <summary>
        /// Merch Haste buff (VERIFIED)
        /// </summary>
        public static Spell MerchHasteBuff
        {
            get
            {
                if (m_haste == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Concentration = 1;
                    spell.ClientEffect = 5054;
                    spell.Icon = 5054;
                    spell.Duration = BUFFS_SPELL_DURATION;
                    spell.Value = 15;
                    spell.Name = "Haste of the Realm";
                    spell.Message1 = "Increases the target's combat speed by 15.";
                    spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                    spell.SpellID = 88010;
                    spell.Target = "Realm";
                    spell.Type = eSpellType.CombatSpeedBuff.ToString();
                    spell.EffectGroup = 100;

                    m_haste = new Spell(spell, 39);
                }
                return m_haste;
            }
        }
        #region Non-live (commented out)
        /*
		/// <summary>
		/// Merch Power Reg buff
		/// </summary>
		public static Spell MerchPoweregBuff
		{
			get
			{
				if (m_powereg == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Concentration = 1;
					spell.ClientEffect = 980;
					spell.Icon = 980;
					spell.Duration = BUFFS_SPELL_DURATION;
					spell.Value = 30;
					spell.Name = "Power of the Realm";
					spell.Description = "Target regenerates power regeneration during the duration of the spell";
					spell.Range = WorldMgr.VISIBILITY_DISTANCE;
					spell.SpellID = 88008;
					spell.Target = "Realm";
					spell.Type = "PowerRegenBuff";
					m_powereg = new Spell(spell, 50);
				}
				return m_powereg;
			}
		}
	   
		/// <summary>
		/// Merch Damage Add buff
		/// </summary>
		public static Spell MerchDmgaddBuff
		{
			get
			{
				if (m_dmgadd == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Concentration = 1;
					spell.ClientEffect = 18;
					spell.Icon = 18;
					spell.Duration = BUFFS_SPELL_DURATION;
					spell.Damage = 5.0;
					spell.DamageType = 15;
					spell.Name = "Damage of the Realm";
					spell.Description = "Target's melee attacks do additional damage.";
					spell.Range = WorldMgr.VISIBILITY_DISTANCE;
					spell.SpellID = 88009;
					spell.Target = "Realm";
					spell.Type = "DamageAdd";
					m_dmgadd = new Spell(spell, 50);
				}
				return m_dmgadd;
			}
		}
		
		/// <summary>
		/// Merch HP Regen buff
		/// </summary>
		public static Spell MerchHPRegenBuff
		{
			get
			{
				if (m_hpRegen == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Concentration = 1;
					spell.ClientEffect = 1534;
					spell.Icon = 1534;
					spell.Duration = BUFFS_SPELL_DURATION;
					spell.Value = 7;
					spell.Name = "Health of the Realm";
					spell.Description = "Target regenerates the given amount of health every tick";
					spell.Range = WorldMgr.VISIBILITY_DISTANCE;
					spell.SpellID = 88011;
					spell.Target = "Realm";
					spell.Type = "HealthRegenBuff";
					m_hpRegen = new Spell(spell, 50);
				}
				return m_hpRegen;
			}
		}
		
		/// <summary>
		/// Merch Heal buff
		/// </summary>
		public static Spell MerchHealBuff
		{
			get
			{
				if (m_heal == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Concentration = 1;
					spell.ClientEffect = 1424;
					spell.Value = 3000;
					spell.Name = "Blessed Health of the Realm";
					spell.Description = "Heals the target.";
					spell.Range = WorldMgr.VISIBILITY_DISTANCE;
					spell.SpellID = 88013;
					spell.Target = "Realm";
					spell.Type = "Heal";
					m_heal = new Spell(spell, 50);
				}
				return m_heal;
			}
		}
		 */
        #endregion Non-live (commented out)

        #endregion Spells

        #endregion SpellCasting

        private void SendReply(GamePlayer target, string msg)
        {
            target.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }

        public class Container
        {
            private Spell m_spell;
            public Spell Spell
            {
                get { return m_spell; }
            }

            private SpellLine m_spellLine;
            public SpellLine SpellLine
            {
                get { return m_spellLine; }
            }

            private GameLiving m_target;
            public GameLiving Target
            {
                get { return m_target; }
                set { m_target = value; }
            }
            public Container(Spell spell, SpellLine spellLine, GameLiving target)
            {
                m_spell = spell;
                m_spellLine = spellLine;
                m_target = target;
            }
        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            return eQuestIndicator.Lore ;
        }
        #endregion

        public override bool Interact(GamePlayer player)
        {
            TradeItems = new MerchantTradeItems("BuffTokens");
            if (!base.Interact(player)) return false;
            TurnTo(player, 10000);
            player.Out.SendMessage("Greetings, " + player.Name + ".\n I've been instructed to strengthen you so that you may defend the lands with valor. Simply hand me the token for the enhancement you desire, and I will empower you accordingly.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            SendMerchantWindow(player);
            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;
            GamePlayer player = source as GamePlayer;
            if (player == null) return false;

            TurnTo(player, 10000);
            TradeItems = new MerchantTradeItems("BuffTokens");
            SendMerchantWindow(player);

            return true;
        }

        public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
        {

            int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
            int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

            DbItemTemplate template = this.TradeItems.GetItem(pagenumber, (eMerchantWindowSlot)slotnumber);
            if (template == null) return;

            int amountToBuy = number;
            if (template.PackSize > 0)
                amountToBuy *= template.PackSize;

            if (amountToBuy <= 0) return;

            long totalValue = number * template.Price;

            lock (player.Inventory.Lock)
            {

                if (player.Wallet.GetMoney() < totalValue)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.YouNeed", WalletHelper.ToString(totalValue)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (!player.Inventory.AddTemplate(GameInventoryItem.Create(template), amountToBuy, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.NotInventorySpace"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
                InventoryLogging.LogInventoryAction(this, player, eInventoryActionType.Merchant, template, amountToBuy);

                string message;
                if (amountToBuy > 1)
                    message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.BoughtPieces", amountToBuy, template.GetName(1, false), WalletHelper.ToString(totalValue));
                else
                    message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.Bought", template.GetName(1, false), WalletHelper.ToString(totalValue));

                if (!player.Wallet.RemoveMoney(totalValue, message, eChatType.CT_Merchant, eChatLoc.CL_SystemWindow))
                {
                    throw new Exception("Money amount changed while adding items.");
                }
                InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, totalValue);

            }
            return;
        }

#region GiveTokens
        public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
        {
            GamePlayer t = source as GamePlayer;

            if (GetDistanceTo(t) > WorldMgr.INTERACT_DISTANCE)
            {
                ((GamePlayer)source).Out.SendMessage("You are too far away to give anything to " + GetName(0, false) + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (t != null && item != null)
            {
                if (item.Id_nb == "Full_Buffs_Token" || item.Id_nb == "BPFull_Buffs_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchBaseAFBuff, MerchBaseSpellLine);
                        BuffPlayer(t, casterMerchStrBuff, MerchBaseSpellLine);
                        BuffPlayer(t, casterMerchDexBuff, MerchBaseSpellLine);
                        BuffPlayer(t, casterMerchConBuff, MerchBaseSpellLine);
                        BuffPlayer(t, casterMerchSpecAFBuff, MerchSpecSpellLine);
                        BuffPlayer(t, casterMerchStrConBuff, MerchSpecSpellLine);
                        BuffPlayer(t, casterMerchDexQuiBuff, MerchSpecSpellLine);
                        BuffPlayer(t, casterMerchAcuityBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchHasteBuff, MerchSpecSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchBaseAFBuff, MerchBaseSpellLine);
                        BuffPlayer(t, MerchStrBuff, MerchBaseSpellLine);
                        BuffPlayer(t, MerchDexBuff, MerchBaseSpellLine);
                        BuffPlayer(t, MerchConBuff, MerchBaseSpellLine);
                        BuffPlayer(t, MerchSpecAFBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchStrConBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchDexQuiBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchAcuityBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchHasteBuff, MerchSpecSpellLine);
                    }
                    #region Non-live (commented out)
                    //BuffPlayer(t, MerchPoweregBuff, MerchSpecSpellLine);
                    //BuffPlayer(t, MerchDmgaddBuff, MerchSpecSpellLine);
                    //BuffPlayer(t, MerchHPRegenBuff, MerchSpecSpellLine);
                    //BuffPlayer(t, MerchEndRegenBuff, MerchSpecSpellLine);
                    //BuffPlayer(t, MerchHealBuff, MerchSpecSpellLine);
                    #endregion Non-live (commented out)
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "Specialization_Buffs_Token" || item.Id_nb == "BPSpecialization_Buffs_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchSpecAFBuff, MerchSpecSpellLine);
                        BuffPlayer(t, casterMerchStrConBuff, MerchSpecSpellLine);
                        BuffPlayer(t, casterMerchDexQuiBuff, MerchSpecSpellLine);
                        BuffPlayer(t, casterMerchAcuityBuff, MerchSpecSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchSpecAFBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchStrConBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchDexQuiBuff, MerchSpecSpellLine);
                        BuffPlayer(t, MerchAcuityBuff, MerchSpecSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;

                }
                if (item.Id_nb == "Baseline_Buffs_Token" || item.Id_nb == "BPBaseline_Buffs_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchBaseAFBuff, MerchBaseSpellLine);
                        BuffPlayer(t, casterMerchStrBuff, MerchBaseSpellLine);
                        BuffPlayer(t, casterMerchDexBuff, MerchBaseSpellLine);
                        BuffPlayer(t, casterMerchConBuff, MerchBaseSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchBaseAFBuff, MerchBaseSpellLine);
                        BuffPlayer(t, MerchStrBuff, MerchBaseSpellLine);
                        BuffPlayer(t, MerchDexBuff, MerchBaseSpellLine);
                        BuffPlayer(t, MerchConBuff, MerchBaseSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "Strength_Buff_Token" || item.Id_nb == "BPStrength_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchStrBuff, MerchBaseSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchStrBuff, MerchBaseSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "Fortification_Buff_Token" || item.Id_nb == "BPFortification_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchConBuff, MerchBaseSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchConBuff, MerchBaseSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "Dexterity_Buff_Token" || item.Id_nb == "BPDexterity_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchDexBuff, MerchBaseSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchDexBuff, MerchBaseSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "Armor_Buff_Token" || item.Id_nb == "BPArmor_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchBaseAFBuff, MerchBaseSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchBaseAFBuff, MerchBaseSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "StrCon_Buff_Token" || item.Id_nb == "BPStrCon_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchStrConBuff, MerchSpecSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchStrConBuff, MerchSpecSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "DexQui_Buff_Token" || item.Id_nb == "BPDexQui_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchDexQuiBuff, MerchSpecSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchDexQuiBuff, MerchSpecSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "Acu_Buff_Token" || item.Id_nb == "BPAcu_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchAcuityBuff, MerchSpecSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchAcuityBuff, MerchSpecSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "SpecAF_Buff_Token" || item.Id_nb == "BPSpecAF_Buff_Token")
                {
                    if (t.CharacterClass.ClassType == eClassType.ListCaster)
                    {
                        BuffPlayer(t, casterMerchSpecAFBuff, MerchSpecSpellLine);
                    }
                    else
                    {
                        BuffPlayer(t, MerchSpecAFBuff, MerchSpecSpellLine);
                    }
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                if (item.Id_nb == "Haste_Buff_Token" || item.Id_nb == "BPHaste_Buff_Token")
                {
                    BuffPlayer(t, MerchHasteBuff, MerchSpecSpellLine);
                    t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    t.Inventory.RemoveItem(item);
                    return true;
                }
                #region Non-live (commented out)
                /*
				if (item.Id_nb == "PowerReg_Buff_Token")
				{
					BuffPlayer(t, MerchPoweregBuff, MerchSpecSpellLine);
					t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					t.Inventory.RemoveItem(item);
					return true;
				}
				if (item.Id_nb == "DmgAdd_Buff_Token")
				{
					BuffPlayer(t, MerchDmgaddBuff, MerchSpecSpellLine);
					t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					t.Inventory.RemoveItem(item);
					return true;
				}
				if (item.Id_nb == "HPReg_Buff_Token")
				{
					BuffPlayer(t, MerchHPRegenBuff, MerchSpecSpellLine);
					t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					t.Inventory.RemoveItem(item);
					return true;
				}
				 */
                #endregion Non-live (commented out)
            }
            #region Non-live (commented out)
            /*if (item.Id_nb == "EnduReg_Buff_Token")
			{
				BuffPlayer(t, MerchEndRegenBuff, MerchSpecSpellLine);
				t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				t.Inventory.RemoveItem(item);
				return true;
			}
			if (item.Id_nb == "Heal_Buff_Token")
			{
				BuffPlayer(t, MerchHealBuff, MerchSpecSpellLine);
				t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				t.Inventory.RemoveItem(item);
				return true;
			}
			if (item.Id_nb == "Otherline_Buffs_Token")
			{
				BuffPlayer(t, MerchPoweregBuff, MerchSpecSpellLine);
				BuffPlayer(t, MerchDmgaddBuff, MerchSpecSpellLine);
				BuffPlayer(t, MerchHasteBuff, MerchSpecSpellLine);
				BuffPlayer(t, MerchHPRegenBuff, MerchSpecSpellLine);
				//BuffPlayer(t, MerchEndRegenBuff, MerchSpecSpellLine);
				BuffPlayer(t, MerchHealBuff, MerchSpecSpellLine);
				t.Out.SendMessage("Fight well, " + t.RaceName + ".", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				t.Inventory.RemoveItem(item);
				return true;
			}
			 */
            #endregion Non-live (commented out)
            return base.ReceiveItem(source, item);
        }
    }
}
#endregion

#region Tokens
namespace DOL.GS.Items
{
    public class BuffTokensEvent
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        [GameServerStartedEvent]
        public static void OnServerStartup(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_BUFF_TOKENS)
                return;

            DbItemTemplate item;

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Full_Buffs_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Full_Buffs_Token";
                item.Name = "Full Buffs Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 320000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Specialization_Buffs_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Specialization_Buffs_Token";
                item.Name = "Specialization Buffs Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 240000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Baseline_Buffs_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Baseline_Buffs_Token";
                item.Name = "Baseline Buffs Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 80000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Strength_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Strength_Buff_Token";
                item.Name = "Strength Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 20000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Fortification_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Fortification_Buff_Token";
                item.Name = "Fortification Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 20000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Dexterity_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Dexterity_Buff_Token";
                item.Name = "Dexertity Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 20000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Armor_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Armor_Buff_Token";
                item.Name = "Armor Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 20000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("StrCon_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "StrCon_Buff_Token";
                item.Name = "Might Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 40000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("DexQui_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "DexQui_Buff_Token";
                item.Name = "Deftness Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 40000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Acu_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Acu_Buff_Token";
                item.Name = "Enlightenment Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 40000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("SpecAF_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "SpecAF_Buff_Token";
                item.Name = "Barrier Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 60000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }

            item = (DbItemTemplate)GameServer.Database.FindObjectByKey<DbItemTemplate>("Haste_Buff_Token");
            if (item == null)
            {
                item = new DbItemTemplate();
                item.Id_nb = "Haste_Buff_Token";
                item.Name = "Haste Buff Token";
                item.Level = 1;
                item.Item_Type = 40;
                item.Model = 485;
                item.IsDropable = false;
                item.IsPickable = true;
                item.Price = 60000;
                item.Weight = 1;
                item.PackageID = "BuffTokens";
                GameServer.Database.AddObject(item);
                if (log.IsDebugEnabled)
                    log.Debug("Added " + item.Id_nb);
            }
        }
    }


    #region MerchantList
    public class BuffTokensListEvent
    {

        [GameServerStartedEvent]
        public static void OnServerStartup(DOLEvent e, object sender, EventArgs args)
        {
            DbItemTemplate[] buffMerchEvent = DOLDB<DbItemTemplate>.SelectObjects(DB.Column("PackageID").IsLike("BuffTokens")).OrderBy(it => it.Item_Type).ToArray();
            DbMerchantItem m_item = null;
            int pagenumber = 0;
            int slotposition = 0;
            m_item = DOLDB<DbMerchantItem>.SelectObject(DB.Column("ItemListID").IsEqualTo("BuffTokens"));
            if (m_item == null)
            {
                foreach (DbItemTemplate item in buffMerchEvent)
                {
                    m_item = new DbMerchantItem();
                    m_item.ItemListID = "BuffTokens";
                    m_item.ItemTemplateID = item.Id_nb;
                    m_item.PageNumber = pagenumber;
                    m_item.SlotPosition = slotposition;
                    m_item.AllowAdd = true;
                    GameServer.Database.AddObject(m_item);
                    if (slotposition == 29)
                    {
                        slotposition = 0;
                        pagenumber += 1;
                    }
                    else
                    {
                        slotposition += 1;
                    }
                }
            }
        }
    }
    #endregion MerchantList
}
#endregion Tokens