using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Buffs a single stat,
    /// considered as a baseline buff (regarding the bonuscategories on statproperties)
    /// </summary>
    public abstract class SingleStatBuff(GameLiving caster, Spell spell, SpellLine line) : PropertyChangingSpell(caster, spell, line)
    {
        public virtual bool BuffReceivesSpecBonus => false;
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;

        protected override void SendUpdates(GameLiving target)
        {
            target.UpdateHealthManaEndu();
        }

        protected override double CalculateBuffDebuffEffectiveness()
        {
            double effectiveness;
            GamePlayer playerCaster = Caster as GamePlayer;

            if (SpellLine.KeyName is GlobalSpellsLines.Potions_Effects or GlobalSpellsLines.Item_Effects or GlobalSpellsLines.Combat_Styles_Effect or GlobalSpellsLines.Realm_Spells || Spell.Level <= 0)
                effectiveness = 1.0;
            else if ((Spell.IsBuff && BuffReceivesSpecBonus) || Spell.IsDebuff)
            {
                if (Caster is NecromancerPet necromancerPet && necromancerPet.Owner is GamePlayer playerOwner)
                    playerCaster = playerOwner;

                if (playerCaster != null)
                    effectiveness = CalculateEffectivenessFromSpec(playerCaster);
                else
                    effectiveness = 1.0; // NPC (necromancer pet excluded).
            }
            else
                effectiveness = 1.0; // Neither a potion, item, buff, or debuff.

            if (playerCaster != null && playerCaster.UseDetailedCombatLog && effectiveness != 1)
                playerCaster.Out.SendMessage($"Effectiveness (spec): {effectiveness:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

            return base.CalculateBuffDebuffEffectiveness() * effectiveness;

            double CalculateEffectivenessFromSpec(GamePlayer player)
            {
                double effectiveness = 0.75 + (player.GetModifiedSpecLevel(m_spellLine.Spec) - 1.0) * 0.5 / Spell.Level;
                return Math.Clamp(effectiveness, 0.75, 1.25);
            }
        }

        public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
        {
            return new StatBuffECSEffect(initParams);
        }

        public override bool HasConflictingEffectWith(ISpellHandler compare)
        {
            if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.Spell.EffectGroup;

            if (!base.HasConflictingEffectWith(compare))
                return false;

            if (Spell.Duration > 0 && compare.Spell.Concentration > 0)
                return compare.Spell.Value >= Spell.Value;

            return compare.SpellLine.IsBaseLine == SpellLine.IsBaseLine;
        }
    }

    [SpellHandler(eSpellType.StrengthBuff)]
    public class StrengthBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override bool BuffReceivesSpecBonus => true;
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

    [SpellHandler(eSpellType.DexterityBuff)]
    public class DexterityBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override bool BuffReceivesSpecBonus => true;
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

    [SpellHandler(eSpellType.ConstitutionBuff)]
    public class ConstitutionBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override bool BuffReceivesSpecBonus => true;
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

    [SpellHandler(eSpellType.ArmorFactorBuff)]
    public abstract class ArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override bool BuffReceivesSpecBonus => true;
        public override eProperty Property1 => eProperty.ArmorFactor;
    }

    [SpellHandler(eSpellType.BaseArmorFactorBuff)]
    public class BaseArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : ArmorFactorBuff(caster, spell, line)
    {
        // List caster AF buffs use their delve value as is.
        public override bool BuffReceivesSpecBonus => Caster is GamePlayer player ? player.CharacterClass.ClassType is not eClassType.ListCaster : true;
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
    }

    [SpellHandler(eSpellType.SpecArmorFactorBuff)]
    public class SpecArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : ArmorFactorBuff(caster, spell, line)
    {
        // Spec AF chants (Paladin) are uncapped.
        public override eBuffBonusCategory BonusCategory1 => spell.IsChant ? eBuffBonusCategory.OtherBuff : eBuffBonusCategory.SpecBuff;
    }

    [SpellHandler(eSpellType.PaladinArmorFactorBuff)]
    public class PaladinArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : ArmorFactorBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.OtherBuff;
    }

    [SpellHandler(eSpellType.ArmorAbsorptionBuff)]
    public class ArmorAbsorptionBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ArmorAbsorption;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.CombatSpeedBuff)]
    public class CombatSpeedBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MeleeSpeed;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.HasteBuff)]
    public class HasteBuff(GameLiving caster, Spell spell, SpellLine line) : CombatSpeedBuff(caster, spell, line) { }

    [SpellHandler(eSpellType.CelerityBuff)]
    public class CelerityBuff(GameLiving caster, Spell spell, SpellLine line) : CombatSpeedBuff(caster, spell, line) { }

    [SpellHandler(eSpellType.FatigueConsumptionBuff)]
    public class FatigueConsumptionBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.FatigueConsumption;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.MeleeDamageBuff)]
    public class MeleeDamageBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MeleeDamage;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.MesmerizeDurationBuff)]
    public class MesmerizeDurationBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MesmerizeDurationReduction;

        protected override void SendUpdates(GameLiving target) { }
    }

    [SpellHandler(eSpellType.AcuityBuff)]
    public class AcuityBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override bool BuffReceivesSpecBonus => true;
        public override eProperty Property1 => eProperty.Acuity;
    }

    [SpellHandler(eSpellType.QuicknessBuff)]
    public class QuicknessBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override bool BuffReceivesSpecBonus => true;
        public override eProperty Property1 => eProperty.Quickness;
    }

    [SpellHandler(eSpellType.DPSBuff)]
    public class DPSBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.DPS;
    }

    [SpellHandler(eSpellType.EvadeBuff)]
    public class EvadeChanceBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.EvadeChance;
    }

    [SpellHandler(eSpellType.ParryBuff)]
    public class ParryChanceBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ParryChance;
    }

    [SpellHandler(eSpellType.WeaponSkillBuff)]
    public class WeaponSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.WeaponSkill;
    }

    [SpellHandler(eSpellType.StealthSkillBuff)]
    public class StealthSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Skill_Stealth;
    }

    [SpellHandler(eSpellType.ToHitBuff)]
    public class ToHitSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ToHitBonus;
    }

    [SpellHandler(eSpellType.MagicResistsBuff)]
    public class MagicResistsBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.MagicAbsorption;
    }

    [SpellHandler(eSpellType.StyleAbsorbBuff)]
    public class StyleAbsorbBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.StyleAbsorb;
    }

    [SpellHandler(eSpellType.ExtraHP)]
    public class ExtraHP(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ExtraHP;
    }

    [SpellHandler(eSpellType.FlexibleSkillBuff)]
    public class FlexibleSkillBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Skill_Flexible_Weapon;
    }

    [SpellHandler(eSpellType.ResiPierceBuff)]
    public class ResiPierceBuff(GameLiving caster, Spell spell, SpellLine line) : SingleStatBuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.ResistPierce;
    }
}
