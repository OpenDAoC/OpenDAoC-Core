using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class BuffBot : GameNPC
    {
        #region Attributes & Config
        private Queue m_buffs = new Queue();
        private const int BUFFS_SPELL_DURATION = 7200; // Duration in seconds (2 hours)
        private const bool BUFFS_PLAYER_PET = true; // Should pets also receive buffs?

        public BuffBot() : base()
        {
            Flags |= eFlags.PEACE;
        }

        public override int Concentration => 10000;
        public override int Mana => 10000;

        public override bool AddToWorld()
        {
            Level = 50;
            return base.AddToWorld();
        }
        #endregion

        #region Interact Logic

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            if (GetDistanceTo(player) > WorldMgr.INTERACT_DISTANCE)
            {
                return false;
            }

            TurnTo(player, 2000);

            // Level 1 logic: Only Water Breathing is granted
            // if (player.Level == 1)
            // {
            //    BuffPlayer(player, MerchWaterbreathingBuff, MerchSpecSpellLine);
            //}
            //else
            //{
                // Level 2-50: Full buff routine with scaling
                ExecuteFullBuffRoutine(player);
            //}

            return true;
        }

        private void ExecuteFullBuffRoutine(GamePlayer player)
        {
            bool isCaster = player.CharacterClass.ClassType == eClassType.ListCaster;

            // Distribute class-specific buffs
            if (isCaster)
            { 
                BuffPlayer(player, casterMerchStrBuff, MerchBaseSpellLine);
                BuffPlayer(player, casterMerchDexBuff, MerchBaseSpellLine);
                BuffPlayer(player, casterMerchConBuff, MerchBaseSpellLine);
                BuffPlayer(player, casterMerchSpecAFBuff, MerchSpecSpellLine);
                BuffPlayer(player, casterMerchStrConBuff, MerchSpecSpellLine);
                BuffPlayer(player, casterMerchDexQuiBuff, MerchSpecSpellLine);
                BuffPlayer(player, casterMerchAcuityBuff, MerchSpecSpellLine);
            }
            else
            {
                BuffPlayer(player, MerchBaseAFBuff, MerchBaseSpellLine);
                BuffPlayer(player, MerchStrBuff, MerchBaseSpellLine);
                BuffPlayer(player, MerchDexBuff, MerchBaseSpellLine);
                BuffPlayer(player, MerchConBuff, MerchBaseSpellLine);
                BuffPlayer(player, MerchSpecAFBuff, MerchSpecSpellLine);
                BuffPlayer(player, MerchStrConBuff, MerchSpecSpellLine);
                BuffPlayer(player, MerchDexQuiBuff, MerchSpecSpellLine);
                BuffPlayer(player, MerchAcuityBuff, MerchSpecSpellLine);
            }

            // Common buffs for everyone
            BuffPlayer(player, MerchHasteBuff, MerchSpecSpellLine);
            //BuffPlayer(player, MerchWaterbreathingBuff, MerchSpecSpellLine); TODO not implemented in DB
        }
        #endregion

        #region Buff Processing & Scaling Engine

        /// <summary>
        /// Calculates and casts a buff based on the player's level.
        /// </summary>
        public void BuffPlayer(GamePlayer player, Spell spell, SpellLine spellLine)
        {
            if (spell == null || spellLine == null) return;
            if (m_buffs == null) m_buffs = new Queue();

            // Scaling calculation:
            // Level 50 = 1.0 (100% spell value)
            // Level 5  = 0.1 (10% spell value)
            double scaleFactor = (double)player.Level / 50.0;

            // Create a new temporary DbSpell object to avoid modifying the global spell
            DbSpell scaledDb = new DbSpell();
            
            scaledDb.Name = spell.Name;
            scaledDb.Icon = spell.Icon;
            scaledDb.ClientEffect = spell.ClientEffect;
            scaledDb.Type = spell.SpellType.ToString(); // Converted to string for DbSpell
            scaledDb.Duration = BUFFS_SPELL_DURATION;
            scaledDb.CastTime = 0;
            scaledDb.Target = "Realm";
            scaledDb.Range = WorldMgr.VISIBILITY_DISTANCE;
            scaledDb.EffectGroup = spell.EffectGroup;

            // Apply scaling logic
            // Haste and Water Breathing should not be scaled down
            if (/*spell.SpellType == eSpellType.WaterBreathing || */spell.SpellType == eSpellType.CombatSpeedBuff)
            {
                scaledDb.Value = spell.Value;
            }
            else
            {
                // Scale values for stats and AF
                scaledDb.Value = (int)(spell.Value * scaleFactor);

                // Ensure value is at least 1
                if (scaledDb.Value < 1) scaledDb.Value = 1;
            }

            Spell scaledSpell = new Spell(scaledDb, spell.Level);

            // Add player to queue
            m_buffs.Enqueue(new Container(scaledSpell, spellLine, player));

            // Add pet to queue if enabled
            if (BUFFS_PLAYER_PET && player.ControlledBrain?.Body is GameLiving pet)
            {
                m_buffs.Enqueue(new Container(scaledSpell, spellLine, pet));
            }

            CastBuffs();
        }

        private void CastBuffs()
        {
            while (m_buffs.Count > 0)
            {
                Container con = (Container)m_buffs.Dequeue();
                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(this, con.Spell, con.SpellLine);
                if (spellHandler != null)
                {
                    spellHandler.StartSpell(con.Target);
                }
            }
        }
        #endregion

        #region Spell Definitions

        private static SpellLine m_MerchBaseSpellLine;
        public static SpellLine MerchBaseSpellLine => m_MerchBaseSpellLine ?? (m_MerchBaseSpellLine = new SpellLine("MerchBaseSpellLine", "BuffBot Spells", "unknown", true));

        private static SpellLine m_MerchSpecSpellLine;
        public static SpellLine MerchSpecSpellLine => m_MerchSpecSpellLine ?? (m_MerchSpecSpellLine = new SpellLine("MerchSpecSpellLine", "BuffBot Spells", "unknown", false));

        // Base Buffs
        public static Spell MerchBaseAFBuff => CreateSpell(88001, 801, eSpellType.BaseArmorFactorBuff, 30, "Base Armor", 5017, 2000);
        public static Spell MerchStrBuff => CreateSpell(88002, 802, eSpellType.StrengthBuff, 32, "Base Strength", 5005, 2001);
        public static Spell casterMerchStrBuff => CreateSpell(89002, 802, eSpellType.StrengthBuff, 32, "Base Strength", 5005, 2001);
        public static Spell MerchConBuff => CreateSpell(88003, 803, eSpellType.ConstitutionBuff, 32, "Base Constitution", 5034, 2002);
        public static Spell casterMerchConBuff => CreateSpell(89003, 803, eSpellType.ConstitutionBuff, 32, "Base Constitution", 5034, 2002);
        public static Spell MerchDexBuff => CreateSpell(88004, 804, eSpellType.DexterityBuff, 32, "Base Dexterity", 5024, 2003);
        public static Spell casterMerchDexBuff => CreateSpell(89004, 804, eSpellType.DexterityBuff, 32, "Base Dexterity", 5024, 2003);

        // Spec/Composite Buffs
        public static Spell MerchStrConBuff => CreateSpell(88005, 805, eSpellType.StrengthConstitutionBuff, 47, "Strength/Constitution", 5065, 2004);
        public static Spell casterMerchStrConBuff => CreateSpell(89005, 805, eSpellType.StrengthConstitutionBuff, 47, "Strength/Constitution", 5065, 2004);
        public static Spell MerchDexQuiBuff => CreateSpell(88006, 806, eSpellType.DexterityQuicknessBuff, 47, "Dexterity/Quickness", 5074, 2005);
        public static Spell casterMerchDexQuiBuff => CreateSpell(89006, 806, eSpellType.DexterityQuicknessBuff, 47, "Dexterity/Quickness", 5074, 2005);
        public static Spell MerchAcuityBuff => CreateSpell(88007, 807, eSpellType.AcuityBuff, 32, "Acuity", 5078, 2006);
        public static Spell casterMerchAcuityBuff => CreateSpell(89007, 807, eSpellType.AcuityBuff, 32, "Acuity", 5078, 2006);
        public static Spell MerchSpecAFBuff => CreateSpell(88014, 808, eSpellType.SpecArmorFactorBuff, 43, "Spec Armor", 1504, 2007);
        public static Spell casterMerchSpecAFBuff => CreateSpell(89014, 808, eSpellType.SpecArmorFactorBuff, 43, "Spec Armor", 1504, 2007);

        // Utility Buffs
        public static Spell MerchHasteBuff => CreateSpell(88010, 809, eSpellType.CombatSpeedBuff, 12, "Combat Haste", 5054, 2008);
        //public static Spell MerchWaterbreathingBuff => CreateSpell(88015, 810, eSpellType.WaterBreathing, 100, "Water Breathing", 2009, 2009); TODO not implemented in DB

        private static Spell CreateSpell(int id, int group, eSpellType type, int value, string name, int icon, int effect)
        {
            DbSpell dbSpell = new DbSpell();
            dbSpell.SpellID = id;
            dbSpell.Name = name;
            dbSpell.Icon = icon;
            dbSpell.ClientEffect = effect;
            dbSpell.Value = value;
            dbSpell.Duration = BUFFS_SPELL_DURATION;
            dbSpell.Type = type.ToString();
            dbSpell.Target = "Realm";
            dbSpell.EffectGroup = group;
            dbSpell.CastTime = 0;
            dbSpell.Range = WorldMgr.VISIBILITY_DISTANCE;
            dbSpell.AllowAdd = false;
            return new Spell(dbSpell, 50);
        }
        #endregion

        #region Helper Class
        public class Container
        {
            public Spell Spell { get; set; }
            public SpellLine SpellLine { get; set; }
            public GameLiving Target { get; set; }

            public Container(Spell spell, SpellLine spellLine, GameLiving target)
            {
                Spell = spell;
                SpellLine = spellLine;
                Target = target;
            }
        }
        #endregion

        // Disabled Quest Symbol on BuffBot
        // public override eQuestIndicator GetQuestIndicator(GamePlayer player) => eQuestIndicator.Lore;
    }
}