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
        private Queue m_buffs = new Queue();
        private const int BUFFS_DURATION = 7200; // 2 Stunden
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

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            if (GetDistanceTo(player) > WorldMgr.INTERACT_DISTANCE) return false;

            TurnTo(player, 2000);
            ExecuteFullBuffRoutine(player);
            return true;
        }

        private void ExecuteFullBuffRoutine(GamePlayer player)
        {
            if (player.Level < 1) return;

            bool isCaster = player.CharacterClass.ClassType == eClassType.ListCaster;
            eRealm realm = player.Realm;

            // Target, SpellID, EffectGroup, eSpellType, BaseValue, Name, Icon, SpellLine, ScaleWithLevel (default: true)
            if (!isCaster)
                AddBuffToQueue(player, 1, 1, eSpellType.BaseArmorFactorBuff, 30, "Greater Armor", GetIconByRealm(realm, "AF"), MerchBaseSpellLine);

            AddBuffToQueue(player, 2, 4, eSpellType.StrengthBuff, 32, "Greater Strength", GetIconByRealm(realm, "STR"), MerchBaseSpellLine);
            AddBuffToQueue(player, 3, 202, eSpellType.DexterityBuff, 32, "Greater Dexterity", GetIconByRealm(realm, "DEX"), MerchBaseSpellLine);
            AddBuffToQueue(player, 4, 201, eSpellType.ConstitutionBuff, 32, "Greater Constitution", GetIconByRealm(realm, "CON"), MerchBaseSpellLine);

            // --- SPEC BUFFS ---
            AddBuffToQueue(player, 5, 2, eSpellType.SpecArmorFactorBuff, 43, "Specialist Armor", 1504, MerchSpecSpellLine);
            AddBuffToQueue(player, 6, 204, eSpellType.StrengthConstitutionBuff, 47, "Strength/Constitution", GetIconByRealm(realm, "STRCON"), MerchSpecSpellLine);
            AddBuffToQueue(player, 7, 203, eSpellType.DexterityQuicknessBuff, 47, "Dexterity/Quickness", GetIconByRealm(realm, "DEXQUI"), MerchSpecSpellLine);

            if (isCaster)
                AddBuffToQueue(player, 8, 200, eSpellType.AcuityBuff, 32, "Greater Acuity", GetIconByRealm(realm, "ACU"), MerchSpecSpellLine);

            // --- UTILITY ---
            if (!isCaster)
                AddBuffToQueue(player, 9, 100, eSpellType.CombatSpeedBuff, 12, "Combat Haste", 5054, MerchSpecSpellLine, false);

            AddBuffToQueue(player, 10, 7510, eSpellType.WaterBreathing, 100, "Water Breathing", 8107, MerchSpecSpellLine, false);

            // Start casting the queue
            CastBuffs();
        }

        /// <summary>
        /// Berechnet die Skalierung und fügt den Buff der Warteschlange hinzu.
        /// </summary>
        private void AddBuffToQueue(GamePlayer player, int id, int group, eSpellType type, int baseValue, string name, int icon, SpellLine line, bool scale = true)
        {
            double scaleFactor = scale ? (player.Level / 50.0) : 1.0;
            int finalValue = (int)(baseValue * scaleFactor);
            if (finalValue < 1) finalValue = 1;

            DbSpell dbSpell = new DbSpell
            {
                SpellID = 900000 + id,
                Name = name,
                Icon = icon,
                ClientEffect = icon,
                Value = finalValue,
                Duration = (type == eSpellType.WaterBreathing) ? 1800 : BUFFS_DURATION,
                Type = type.ToString(),
                Target = "Realm",
                EffectGroup = group,
                CastTime = 0,
                Range = WorldMgr.VISIBILITY_DISTANCE,
                AllowAdd = false
            };

            Spell spell = new Spell(dbSpell, player.Level);

            m_buffs.Enqueue(new Container(spell, line, player));

            if (BUFFS_PLAYER_PET && player.ControlledBrain?.Body is GameLiving pet)
            {
                m_buffs.Enqueue(new Container(spell, line, pet));
            }
        }

        private void CastBuffs()
        {
            while (m_buffs.Count > 0)
            {
                Container con = (Container)m_buffs.Dequeue();
                ISpellHandler handler = ScriptMgr.CreateSpellHandler(this, con.Spell, con.SpellLine);
                if (handler != null)
                {
                    handler.StartSpell(con.Target);
                }
            }
        }

        #region Helper: Icons & SpellLines
        private int GetIconByRealm(eRealm realm, string type)
        {
            switch (realm)
            {
                case eRealm.Midgard:
                    if (type == "AF") return 3155;
                    if (type == "STR") return 3166;
                    if (type == "DEX") return 3174;
                    if (type == "CON") return 3185;
                    if (type == "STRCON") return 3266;
                    if (type == "DEXQUI") return 3276;
                    if (type == "ACU") return 3282;
                    break;
                case eRealm.Hibernia:
                    if (type == "AF") return 5017;
                    if (type == "STR") return 5005;
                    if (type == "DEX") return 5024;
                    if (type == "CON") return 5034;
                    if (type == "STRCON") return 5065;
                    if (type == "DEXQUI") return 5074;
                    if (type == "ACU") return 5078;
                    break;
                default: // Albion
                    if (type == "AF") return 1465;
                    if (type == "STR") return 1455;
                    if (type == "DEX") return 1474;
                    if (type == "CON") return 1484;
                    if (type == "STRCON") return 1515;
                    if (type == "DEXQUI") return 1524;
                    if (type == "ACU") return 1536;
                    break;
            }
            return 1;
        }

        private static SpellLine m_baseLine;
        public static SpellLine MerchBaseSpellLine => m_baseLine ?? (m_baseLine = new SpellLine("MerchBase", "BuffBot Base", "unknown", true));

        private static SpellLine m_specLine;
        public static SpellLine MerchSpecSpellLine => m_specLine ?? (m_specLine = new SpellLine("MerchSpec", "BuffBot Spec", "unknown", false));

        public class Container
        {
            public Spell Spell { get; }
            public SpellLine SpellLine { get; }
            public GameLiving Target { get; }
            public Container(Spell s, SpellLine l, GameLiving t) { Spell = s; SpellLine = l; Target = t; }
        }
        #endregion
    }
}