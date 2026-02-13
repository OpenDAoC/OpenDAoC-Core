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
        private const int BUFFS_SPELL_DURATION = 7200; 
        private const bool BUFFS_PLAYER_PET = true;

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
            if (GetDistanceTo(player) > WorldMgr.INTERACT_DISTANCE) return false;

            TurnTo(player, 2000);
            if (player.Level != 1)
            {
                ExecuteFullBuffRoutine(player);
                return true;
            }
            // Waterbuff needs to be implemented
            return true;
        }

        private void ExecuteFullBuffRoutine(GamePlayer player)
        {
            bool isCaster = player.CharacterClass.ClassType == eClassType.ListCaster;
            eRealm realm = player.Realm;


            // Spellids and Effectids are based of DB entries
            // Spec AF same for all realms, only effect is different
            // TODO class changes depending of Spec AF

            // SpellID, EffectGroup, Type, Value, Name, Icon, EffectID
            if (realm == eRealm.Midgard)
            {
                // Midgard
                if (!isCaster) BuffPlayer(player, CreateSpell(1, 801, eSpellType.BaseArmorFactorBuff, 30, "Greater Armor", 1465, 3155), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(2, 802, eSpellType.StrengthBuff, 32, "Greater Strength", 3166, 3166), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(3, 804, eSpellType.DexterityBuff, 32, "Greater Dexterity", 3174, 3174), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(4, 803, eSpellType.ConstitutionBuff, 32, "Greater Constitution", 3185, 3185), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(5, 808, eSpellType.SpecArmorFactorBuff, 43, "Specialist Armor", 1504, 1701), MerchSpecSpellLine);
                BuffPlayer(player, CreateSpell(6, 805, eSpellType.StrengthConstitutionBuff, 47, "Strength/Constitution", 3266, 3266), MerchSpecSpellLine);
                BuffPlayer(player, CreateSpell(7, 806, eSpellType.DexterityQuicknessBuff, 47, "Dexterity/Quickness", 3276, 3276), MerchSpecSpellLine);
                if (isCaster) BuffPlayer(player, CreateSpell(8, 807, eSpellType.AcuityBuff, 32, "Greater Acuity", 3282, 3282), MerchSpecSpellLine);
            }
            else if (realm == eRealm.Hibernia)
            {
                // Hibernia
                if (!isCaster) BuffPlayer(player, CreateSpell(1, 801, eSpellType.BaseArmorFactorBuff, 30, "Greater Armor", 5017, 5017), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(2, 802, eSpellType.StrengthBuff, 32, "Greater Strength", 5005, 5005), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(3, 804, eSpellType.DexterityBuff, 32, "Greater Dexterity", 5024, 5024), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(4, 803, eSpellType.ConstitutionBuff, 32, "Greater Constitution", 5034, 5034), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(5, 808, eSpellType.SpecArmorFactorBuff, 43, "Specialist Armor", 1504, 1504), MerchSpecSpellLine);
                BuffPlayer(player, CreateSpell(6, 805, eSpellType.StrengthConstitutionBuff, 47, "Strength/Constitution", 5065, 5065), MerchSpecSpellLine);
                BuffPlayer(player, CreateSpell(7, 806, eSpellType.DexterityQuicknessBuff, 47, "Dexterity/Quickness", 5074, 5074), MerchSpecSpellLine);
                if (isCaster) BuffPlayer(player, CreateSpell(8, 807, eSpellType.AcuityBuff, 32, "Greater Acuity", 5078, 5078), MerchSpecSpellLine);
            }
            else // Albion / Default
            {
                if (!isCaster) BuffPlayer(player, CreateSpell(1, 1461, eSpellType.BaseArmorFactorBuff, 30, "Greater Armor", 1465, 1465), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(2, 802, eSpellType.StrengthBuff, 32, "Greater Strength", 1455, 1455), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(3, 804, eSpellType.DexterityBuff, 32, "Greater Dexterity", 1474, 1474), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(4, 803, eSpellType.ConstitutionBuff, 32, "Greater Constitution", 1484, 1484), MerchBaseSpellLine);
                BuffPlayer(player, CreateSpell(5, 808, eSpellType.SpecArmorFactorBuff, 43, "Specialist Armor", 1504, 1031), MerchSpecSpellLine);
                BuffPlayer(player, CreateSpell(6, 805, eSpellType.StrengthConstitutionBuff, 47, "Strength/Constitution", 1515, 1515), MerchSpecSpellLine);
                BuffPlayer(player, CreateSpell(7, 806, eSpellType.DexterityQuicknessBuff, 47, "Dexterity/Quickness", 1524, 1524), MerchSpecSpellLine);
                if (isCaster) BuffPlayer(player, CreateSpell(7, 807, eSpellType.AcuityBuff, 32, "Greater Acuity", 1536, 1536), MerchSpecSpellLine);
            }

            // Buffs for all
            BuffPlayer(player, CreateSpell(9, 809, eSpellType.CombatSpeedBuff, 12, "Combat Haste", 5054, 164), MerchSpecSpellLine);
            BuffPlayer(player, CreateSpell(10, 810, eSpellType.WaterBreathing, 12, "Water Breathing", 8107, 8107), MerchSpecSpellLine);
        }
        #endregion

        #region Buff Processing & Scaling Engine
        public void BuffPlayer(GamePlayer player, Spell spell, SpellLine spellLine)
        {
            if (spell == null || spellLine == null) return;
            if (m_buffs == null) m_buffs = new Queue();

            double scaleFactor = (double)player.Level / 50.0;

            DbSpell scaledDb = new DbSpell();
            scaledDb.Name = spell.Name;
            scaledDb.Icon = spell.Icon;
            scaledDb.ClientEffect = spell.ClientEffect;
            scaledDb.Type = spell.SpellType.ToString(); 
            scaledDb.Duration = BUFFS_SPELL_DURATION;
            scaledDb.CastTime = 0;
            scaledDb.Target = "Realm";
            scaledDb.Range = WorldMgr.VISIBILITY_DISTANCE;
            scaledDb.EffectGroup = spell.EffectGroup;

            if (spell.SpellType == eSpellType.CombatSpeedBuff)
                scaledDb.Value = spell.Value;
            else
            {
                scaledDb.Value = (int)(spell.Value * scaleFactor);
                if (scaledDb.Value < 1) scaledDb.Value = 1;
            }

            Spell scaledSpell = new Spell(scaledDb, spell.Level);
            m_buffs.Enqueue(new Container(scaledSpell, spellLine, player));

            if (BUFFS_PLAYER_PET && player.ControlledBrain?.Body is GameLiving pet)
                m_buffs.Enqueue(new Container(scaledSpell, spellLine, pet));

            CastBuffs();
        }

        private void CastBuffs()
        {
            while (m_buffs.Count > 0)
            {
                Container con = (Container)m_buffs.Dequeue();
                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(this, con.Spell, con.SpellLine);
                if (spellHandler != null) spellHandler.StartSpell(con.Target);
            }
        }
        #endregion

        #region Spell Definitions
        private static SpellLine m_MerchBaseSpellLine;
        public static SpellLine MerchBaseSpellLine => m_MerchBaseSpellLine ?? (m_MerchBaseSpellLine = new SpellLine("MerchBaseSpellLine", "BuffBot", "unknown", true));

        private static SpellLine m_MerchSpecSpellLine;
        public static SpellLine MerchSpecSpellLine => m_MerchSpecSpellLine ?? (m_MerchSpecSpellLine = new SpellLine("MerchSpecSpellLine", "BuffBot", "unknown", false));

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
    }
}