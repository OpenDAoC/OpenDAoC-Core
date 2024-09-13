using System;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Debuffs a single stat
    /// </summary>
    public abstract class SingleStatDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        // bonus category
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.Debuff;

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new StatDebuffECSEffect(initParams);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);

            if (target.Realm == 0 || Caster.Realm == 0)
            {
                target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
                Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
            }
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = Spell.Duration;
            duration *= (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);
            duration -= duration * target.GetResist(Spell.DamageType) * 0.01;

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = (Spell.Duration * 4);
            return (int)duration;
        }

        /// <summary>
        /// Calculates chance of spell getting resisted
        /// </summary>
        /// <param name="target">the target of the spell</param>
        /// <returns>chance that spell will be resisted for specific target</returns>
        public override double CalculateSpellResistChance(GameLiving target)
        {
            double chance =  base.CalculateSpellResistChance(target);

            /* GameSpellEffect rampage = SpellHandler.FindEffectOnTarget(target, "Rampage");

            if (rampage != null)
                chance += (int)rampage.Spell.Value;*/

            return Math.Min(100, chance);
        }
    }

    [SpellHandler(eSpellType.StrengthDebuff)]
    public class StrengthDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Strength;
    }

    [SpellHandler(eSpellType.DexterityDebuff)]
    public class DexterityDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Dexterity;
    }

    [SpellHandler(eSpellType.ConstitutionDebuff)]
    public class ConstitutionDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Constitution;
    }

    [SpellHandler(eSpellType.ArmorFactorDebuff)]
    public class ArmorFactorDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ArmorFactor;
    }

    [SpellHandler(eSpellType.ArmorAbsorptionDebuff)]
    public class ArmorAbsorptionDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ArmorAbsorption;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.CombatSpeedDebuff)]
    public class CombatSpeedDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MeleeSpeed;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.MeleeDamageDebuff)]
    public class MeleeDamageDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MeleeDamage;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.FatigueConsumptionDebuff)]
    public class FatigueConsumptionDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.FatigueConsumption;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.FumbleChanceDebuff)]
    public class FumbleChanceDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.FumbleChance;


        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.DPSDebuff)]
    public class DPSDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.DPS;
    }

    [SpellHandler(eSpellType.SkillsDebuff)]
    public class SkillsDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.AllSkills;
    }

    [SpellHandler(eSpellType.AcuityDebuff)]
    public class AcuityDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Acuity;
    }

    [SpellHandler(eSpellType.QuicknessDebuff)]
    public class QuicknessDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Quickness;
    }

    [SpellHandler(eSpellType.ToHitDebuff)]
    public class ToHitSkillDebuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ToHitBonus;
    }
}
