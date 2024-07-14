using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Buffs a single stat,
    /// considered as a baseline buff (regarding the bonuscategories on statproperties)
    /// </summary>
    public abstract class SingleStatBuff(GameLiving caster, Spell spell, SpellLine line) : PropertyChangingSpell(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;

        protected override void SendUpdates(GameLiving target)
        {
            target.UpdateHealthManaEndu();
        }

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new StatBuffECSEffect(initParams);
        }

        /// <summary>
        /// Determines wether this spell is compatible with given spell
        /// and therefore overwritable by better versions
        /// spells that are overwritable cannot stack
        /// </summary>
        public override bool IsOverwritable(ECSGameSpellEffect compare)
        {
            if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;

            if (!base.IsOverwritable(compare))
                return false;

            if (Spell.Duration > 0 && compare.SpellHandler.Spell.Concentration > 0)
                return compare.SpellHandler.Spell.Value >= Spell.Value;

            return compare.SpellHandler.SpellLine.IsBaseLine == SpellLine.IsBaseLine;
        }
    }

    [SpellHandlerAttribute("StrengthBuff")]
    public class StrengthBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Strength;

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirStrength))
            {
                MessageToCaster("Your target already has an effect of that type!", eChatType.CT_Spell);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }
    }

    [SpellHandlerAttribute("DexterityBuff")]
    public class DexterityBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Dexterity;

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirDexterity))
            {
                MessageToCaster("Your target already has an effect of that type!", eChatType.CT_Spell);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }
    }

    [SpellHandlerAttribute("ConstitutionBuff")]
    public class ConstitutionBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Constitution;

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirConstitution))
            {
                MessageToCaster("Your target already has an effect of that type!", eChatType.CT_Spell);
                return;
            }

            base.ApplyEffectOnTarget(target);
        }
    }

    [SpellHandlerAttribute("ArmorFactorBuff")]
    public class ArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1
        {
            get
            {
                if (Caster is GamePlayer c && (c.CharacterClass is ClassRanger || c.CharacterClass is ClassHunter) && (SpellLine.KeyName.ToLower().Equals("beastcraft") || SpellLine.KeyName.ToLower().Equals("pathfinding")))
                    return eBuffBonusCategory.BaseBuff;

                if (Spell.Target == eSpellTarget.SELF)
                    return eBuffBonusCategory.Other; // no caps for self buffs

                if (m_spellLine.IsBaseLine)
                    return eBuffBonusCategory.BaseBuff; // baseline cap

                return eBuffBonusCategory.Other; // no caps for spec line buffs
            }
        }

        public override eProperty Property1 => eProperty.ArmorFactor;
    }

    [SpellHandlerAttribute("ArmorAbsorptionBuff")]
    public class ArmorAbsorptionBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ArmorAbsorption;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandlerAttribute("CombatSpeedBuff")]
    public class CombatSpeedBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MeleeSpeed;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandlerAttribute("HasteBuff")]
    public class HasteBuff(GameLiving caster, Spell spell, SpellLine line) : CombatSpeedBuff(caster, spell, line) { }

    [SpellHandlerAttribute("CelerityBuff")]
    public class CelerityBuff(GameLiving caster, Spell spell, SpellLine line) : CombatSpeedBuff(caster, spell, line) { }

    [SpellHandlerAttribute("FatigueConsumptionBuff")]
    public class FatigueConsumptionBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.FatigueConsumption;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandlerAttribute("MeleeDamageBuff")]
    public class MeleeDamageBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MeleeDamage;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandlerAttribute("MesmerizeDurationBuff")]
    public class MesmerizeDurationBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MesmerizeDurationReduction;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandlerAttribute("AcuityBuff")]
    public class AcuityBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Acuity;
    }

    [SpellHandlerAttribute("QuicknessBuff")]
    public class QuicknessBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Quickness;
    }

    [SpellHandlerAttribute("DPSBuff")]
    public class DPSBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.DPS;
    }

    [SpellHandlerAttribute("EvadeBuff")]
    public class EvadeChanceBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.EvadeChance;
    }

    [SpellHandlerAttribute("ParryBuff")]
    public class ParryChanceBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ParryChance;
    }

    [SpellHandlerAttribute("WeaponSkillBuff")]
    public class WeaponSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.WeaponSkill;
    }

    [SpellHandlerAttribute("StealthSkillBuff")]
    public class StealthSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Skill_Stealth;
    }

    [SpellHandlerAttribute("ToHitBuff")]
    public class ToHitSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ToHitBonus;
    }

    [SpellHandlerAttribute("MagicResistsBuff")]
    public class MagicResistsBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MagicAbsorption;
    }

    [SpellHandlerAttribute("StyleAbsorbBuff")]
    public class StyleAbsorbBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.StyleAbsorb;
    }

    [SpellHandlerAttribute("ExtraHP")]
    public class ExtraHP(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ExtraHP;
    }

    [SpellHandlerAttribute("PaladinArmorFactorBuff")]
    public class PaladinArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1
        {
            get
            {
                if (Spell.Target == eSpellTarget.SELF)
                    return eBuffBonusCategory.Other; // no caps for self buffs

                if (m_spellLine.IsBaseLine)
                    return eBuffBonusCategory.BaseBuff; // baseline cap

                return eBuffBonusCategory.Other; // no caps for spec line buffs
            }
        }

        public override eProperty Property1 => eProperty.ArmorFactor;
    }

    [SpellHandler("FlexibleSkillBuff")]
    public class FlexibleSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Skill_Flexible_Weapon;
    }

    [SpellHandler("ResiPierceBuff")]
    public class ResiPierceBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ResistPierce;
    }
}
