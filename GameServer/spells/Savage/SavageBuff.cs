using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    public abstract class AbstractSavageBuff : PropertyChangingSpell
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;

        public AbstractSavageBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new SavageBuffECSGameEffect(initParams);
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster.Health < PowerCost(Caster))
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SavageEnduranceHeal.CheckBeginCast.InsufficientHealth"), eChatType.CT_SpellResisted);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override int PowerCost(GameLiving target)
        {
            if (m_spell.Power < 0)
                return (int) (m_caster.MaxHealth * Math.Abs(m_spell.Power) * 0.01);
            else
                return m_spell.Power;
        }

        public override int CalculateEnduranceCost()
        {
            return 0;
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>(16);
                //list.Add("Function: " + (Spell.SpellType == string.Empty ? "(not implemented)" : Spell.SpellType));
                //list.Add(" "); //empty line
                list.Add(Spell.Description);
                list.Add(" "); //empty line
                if (Spell.InstrumentRequirement != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.InstrumentRequire", GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement)));
                if (Spell.Damage != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")));
                if (Spell.LifeDrainReturn != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.HealthReturned", Spell.LifeDrainReturn));
                else if (Spell.Value != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Value", Spell.Value.ToString("0.###;0.###'%'")));
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Target", Spell.Target));
                if (Spell.Range != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Range", Spell.Range));
                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + " Permanent.");
                else if (Spell.Duration > 60000)
                    list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min"));
                else if (Spell.Duration != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
                if (Spell.Frequency != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
                if (Spell.Power != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.HealthCost", Spell.Power.ToString("0;0'%'")));
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
                if (Spell.RecastDelay > 60000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
                else if (Spell.RecastDelay > 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay / 1000).ToString() + " sec");
                if (Spell.Concentration != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.ConcentrationCost", Spell.Concentration));
                if (Spell.Radius != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Radius", Spell.Radius));
                if (Spell.DamageType != eDamageType.Natural)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));
                if (Spell.IsFocus)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Focus"));

                return list;
            }
        }
    }

    public abstract class AbstractSavageStatBuff : AbstractSavageBuff
    {
        public AbstractSavageStatBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        protected override void SendUpdates(GameLiving target)
        {
            if (target is GamePlayer player)
            {
                player.Out.SendCharStatsUpdate();
                player.Out.SendUpdateWeaponAndArmorStats();
                player.UpdateEncumbrance();
                player.UpdatePlayerStatus();
            }
        }
    }

    public abstract class AbstractSavageResistBuff : AbstractSavageBuff
    {
        public AbstractSavageResistBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        protected override void SendUpdates(GameLiving target)
        {
            if (target is GamePlayer player)
            {
                player.Out.SendCharResistsUpdate();
                player.UpdatePlayerStatus();
            }
        }
    }
 
    [SpellHandler(eSpellType.SavageParryBuff)]
    public class SavageParryBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.ParryChance;

        public SavageParryBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler(eSpellType.SavageEvadeBuff)]
    public class SavageEvadeBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.EvadeChance;

        public SavageEvadeBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler(eSpellType.SavageCombatSpeedBuff)]
    public class SavageCombatSpeedBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.MeleeSpeed;

        public SavageCombatSpeedBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
    }

    [SpellHandler(eSpellType.SavageDPSBuff)]
    public class SavageDPSBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.MeleeDamage;

        public SavageDPSBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler(eSpellType.SavageSlashResistanceBuff)]
    public class SavageSlashResistanceBuff : AbstractSavageResistBuff
    {
        public override eProperty Property1 => eProperty.Resist_Slash;

        public SavageSlashResistanceBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler(eSpellType.SavageThrustResistanceBuff)]
    public class SavageThrustResistanceBuff : AbstractSavageResistBuff
    {
        public override eProperty Property1 => eProperty.Resist_Thrust;

        public SavageThrustResistanceBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler(eSpellType.SavageCrushResistanceBuff)]
    public class SavageCrushResistanceBuff : AbstractSavageResistBuff
    {
        public override eProperty Property1 => eProperty.Resist_Crush;

        public SavageCrushResistanceBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
