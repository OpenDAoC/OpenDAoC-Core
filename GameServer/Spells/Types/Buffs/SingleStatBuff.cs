using System;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.PlayerClass;

namespace Core.GS.Spells
{
    /// <summary>
    /// Buffs a single stat,
    /// considered as a baseline buff (regarding the bonuscategories on statproperties)
    /// </summary>
    public abstract class SingleStatBuff : PropertyChangingSpell
    {
        public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }

        protected override void SendUpdates(GameLiving target)
        {
            target.UpdateHealthManaEndu();
        }

        public override void CreateECSEffect(EcsGameEffectInitParams initParams)
        {
            new StatBuffEcsSpellEffect(initParams);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            int specLevel = Caster.GetModifiedSpecLevel(m_spellLine.Spec);

            if (SpellLine.KeyName is GlobalSpellsLines.Potions_Effects or GlobalSpellsLines.Item_Effects)
                Effectiveness = 1.0;
            else if (Spell.Level <= 0)
                Effectiveness = 1.0;
            else if (Caster is GamePlayer playerCaster)
            {
                if (playerCaster.PlayerClass.ID != (int)EPlayerClass.Savage && Spell.Target != ESpellTarget.ENEMY)
                {
                    if (playerCaster.PlayerClass.ClassType != EPlayerClassType.ListCaster)
                    {
                        Effectiveness = 0.75; // This section is for self bulfs, cleric buffs etc.
                        Effectiveness += (specLevel - 1.0) * 0.5 / Spell.Level;
                        Effectiveness = Math.Max(0.75, Effectiveness);
                        Effectiveness = Math.Min(1.25, Effectiveness);
                    }
                }
                else if (Spell.Target == ESpellTarget.ENEMY)
                {
                    Effectiveness = 0.75; // This section is for list casters stat debuffs.
                    if (playerCaster.PlayerClass.ClassType == EPlayerClassType.ListCaster)
                    {
                        Effectiveness += (specLevel - 1.0) * 0.5 / Spell.Level;
                        Effectiveness = Math.Max(0.75, Effectiveness);
                        Effectiveness = Math.Min(1.25, Effectiveness);
                        Effectiveness *= 1.0 + m_caster.GetModified(EProperty.DebuffEffectivness) * 0.01;

                        if (playerCaster.UseDetailedCombatLog && m_caster.GetModified(EProperty.DebuffEffectivness) > 0)
                            playerCaster.Out.SendMessage($"debuff effectiveness: {m_caster.GetModified(EProperty.DebuffEffectivness)}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        Effectiveness = 1.0; // Non list casters debuffs. Reaver curses, Champ debuffs etc.
                        Effectiveness *= 1.0 + m_caster.GetModified(EProperty.DebuffEffectivness) * 0.01;
                    }
                }
            }
            else if (Caster is NecromancerPet necroPetCaster && necroPetCaster.Owner is GamePlayer playerOwner && Spell.Target == ESpellTarget.ENEMY)
            {
                specLevel = playerOwner.GetModifiedSpecLevel(m_spellLine.Spec);

                Effectiveness = 0.75; // This section is for list casters stat debuffs.
                Effectiveness += (specLevel - 1.0) * 0.5 / Spell.Level;
                Effectiveness = Math.Max(0.75, Effectiveness);
                Effectiveness = Math.Min(1.25, Effectiveness);
                Effectiveness *= 1.0 + playerOwner.GetModified(EProperty.DebuffEffectivness) * 0.01;                

                if (Spell.SpellType == ESpellType.ArmorFactorDebuff)
                    Effectiveness *= 1 + target.GetArmorAbsorb(EArmorSlot.TORSO);

                if (playerOwner.UseDetailedCombatLog && m_caster.GetModified(EProperty.DebuffEffectivness) > 0)
                    playerOwner.Out.SendMessage($"debuff effectiveness: {m_caster.GetModified(EProperty.DebuffEffectivness)}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);
            }
            else
                Effectiveness = 1.0;

            if (Spell.Target != ESpellTarget.ENEMY)
            {
                Effectiveness *= 1.0 + m_caster.GetModified(EProperty.BuffEffectiveness) * 0.01;

                if (Caster is GamePlayer gamePlayer && gamePlayer.UseDetailedCombatLog && m_caster.GetModified(EProperty.BuffEffectiveness) > 0 )
                    gamePlayer.Out.SendMessage($"buff effectiveness: {m_caster.GetModified(EProperty.BuffEffectiveness)}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);
            }
            else
                Effectiveness *= GetCritBonus();

            target.StartHealthRegeneration();
            base.ApplyEffectOnTarget(target);
        }

        /// <summary>
        /// Determines wether this spell is compatible with given spell
        /// and therefore overwritable by better versions
        /// spells that are overwritable cannot stack
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
                return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;

            if (!base.IsOverwritable(compare))
                return false;

            if (Spell.Duration > 0 && compare.SpellHandler.Spell.Concentration > 0)
                return compare.SpellHandler.Spell.Value >= Spell.Value;

            return compare.SpellHandler.SpellLine.IsBaseLine == SpellLine.IsBaseLine;
        }

        private double GetCritBonus()
        {
            double critMod = 1.0;
            int critChance = Caster.DotCriticalChance;

            if (critChance <= 0)
                return critMod;

            GamePlayer playerCaster = Caster as GamePlayer;

            if (playerCaster?.UseDetailedCombatLog == true && critChance > 0)
                playerCaster.Out.SendMessage($"Debuff crit chance: {Caster.DotCriticalChance}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);

            if (Util.Chance(critChance))
            {
                critMod *= 1 + Util.Random(1, 10) * 0.1;
                playerCaster?.Out.SendMessage($"Your {Spell.Name} critically debuffs the enemy for {Math.Round(critMod - 1,3) * 100}% additional effect!", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
            }

            return critMod;
        }

        // constructor
        protected SingleStatBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Str stat baseline buff
    /// </summary>
    [SpellHandler("StrengthBuff")]
    public class StrengthBuff : SingleStatBuff
    {
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirStrength))
            {
                MessageToCaster("Your target already has an effect of that type!", EChatType.CT_Spell);
                return;
            }
            base.ApplyEffectOnTarget(target);
        }
        public override EProperty Property1 { get { return EProperty.Strength; } }

        // constructor
        public StrengthBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Dex stat baseline buff
    /// </summary>
    [SpellHandler("DexterityBuff")]
    public class DexterityBuff : SingleStatBuff
    {
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirDexterity))
            {
                MessageToCaster("Your target already has an effect of that type!", EChatType.CT_Spell);
                return;
            }
            base.ApplyEffectOnTarget(target);
        }
        public override EProperty Property1 { get { return EProperty.Dexterity; } }

        // constructor
        public DexterityBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Con stat baseline buff
    /// </summary>
    [SpellHandler("ConstitutionBuff")]
    public class ConstitutionBuff : SingleStatBuff
    {
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.VampiirConstitution))
            {
                MessageToCaster("Your target already has an effect of that type!", EChatType.CT_Spell);
                return;
            }
            base.ApplyEffectOnTarget(target);
        }
        public override EProperty Property1 { get { return EProperty.Constitution; } }

        // constructor
        public ConstitutionBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Armor factor buff
    /// </summary>
    [SpellHandler("ArmorFactorBuff")]
    public class ArmorFactorBuff : SingleStatBuff
    {
        public override EBuffBonusCategory BonusCategory1
        {
            get
            {
                if (Caster is GamePlayer c && (c.PlayerClass is ClassRanger || c.PlayerClass is ClassHunter) && (SpellLine.KeyName.ToLower().Equals("beastcraft") || SpellLine.KeyName.ToLower().Equals("pathfinding")))
                    return EBuffBonusCategory.BaseBuff;

                if (Spell.Target == ESpellTarget.SELF)
                    return EBuffBonusCategory.Other; // no caps for self buffs

                if (m_spellLine.IsBaseLine)
                    return EBuffBonusCategory.BaseBuff; // baseline cap

                return EBuffBonusCategory.Other; // no caps for spec line buffs
            }
        }
        public override EProperty Property1 { get { return EProperty.ArmorFactor; } }

        // constructor
        public ArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Armor Absorption buff
    /// </summary>
    [SpellHandler("ArmorAbsorptionBuff")]
    public class ArmorAbsorptionBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.ArmorAbsorption; } }

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public ArmorAbsorptionBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Combat speed buff
    /// </summary>
    [SpellHandler("CombatSpeedBuff")]
    public class CombatSpeedBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.MeleeSpeed; } }

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public CombatSpeedBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    
    /// <summary>
    /// Haste Buff stacking with other Combat Speed Buff
    /// </summary>
    [SpellHandler("HasteBuff")]
    public class HasteBuff : CombatSpeedBuff
    {
        // constructor
        public HasteBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Celerity Buff stacking with other Combat Speed Buff
    /// </summary>
    [SpellHandler("CelerityBuff")]
    public class CelerityBuff : CombatSpeedBuff
    {
        // constructor
        public CelerityBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Fatigue reduction buff
    /// </summary>
    [SpellHandler("FatigueConsumptionBuff")]
    public class FatigueConsumptionBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.FatigueConsumption; } }

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public FatigueConsumptionBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Melee damage buff
    /// </summary>
    [SpellHandler("MeleeDamageBuff")]
    public class MeleeDamageBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.MeleeDamage; } }

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public MeleeDamageBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Mesmerize duration buff
    /// </summary>
    [SpellHandler("MesmerizeDurationBuff")]
    public class MesmerizeDurationBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.MesmerizeDurationReduction; } }

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public MesmerizeDurationBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }


    /// <summary>
    /// Acuity buff
    /// </summary>
    [SpellHandler("AcuityBuff")]
    public class AcuityBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.Acuity; } }

        // constructor
        public AcuityBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Quickness buff
    /// </summary>
    [SpellHandler("QuicknessBuff")]
    public class QuicknessBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.Quickness; } }

        // constructor
        public QuicknessBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// DPS buff
    /// </summary>
    [SpellHandler("DPSBuff")]
    public class DPSBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.DPS; } }

        // constructor
        public DPSBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Evade chance buff
    /// </summary>
    [SpellHandler("EvadeBuff")]
    public class EvadeChanceBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.EvadeChance; } }

        // constructor
        public EvadeChanceBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    /// <summary>
    /// Parry chance buff
    /// </summary>
    [SpellHandler("ParryBuff")]
    public class ParryChanceBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.ParryChance; } }

        // constructor
        public ParryChanceBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    /// <summary>
    /// WeaponSkill buff
    /// </summary>
    [SpellHandler("WeaponSkillBuff")]
    public class WeaponSkillBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.WeaponSkill; } }

        // constructor
        public WeaponSkillBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    /// <summary>
    /// Stealth Skill buff
    /// </summary>
    [SpellHandler("StealthSkillBuff")]
    public class StealthSkillBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.Skill_Stealth; } }

        // constructor
        public StealthSkillBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    /// <summary>
    /// To Hit buff
    /// </summary>
    [SpellHandler("ToHitBuff")]
    public class ToHitSkillBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.ToHitBonus; } }

        // constructor
        public ToHitSkillBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    /// <summary>
    /// Magic Resists Buff
    /// </summary>
    [SpellHandler("MagicResistsBuff")]
    public class MagicResistsBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.MagicAbsorption; } }

        // constructor
        public MagicResistsBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler("StyleAbsorbBuff")]
    public class StyleAbsorbBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.StyleAbsorb; } }
        public StyleAbsorbBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler("ExtraHP")]
    public class ExtraHP : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.ExtraHP; } }
        public ExtraHP(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Paladin Armor factor buff
    /// </summary>
    [SpellHandler("PaladinArmorFactorBuff")]
    public class PaladinArmorFactorBuff : SingleStatBuff
    {
        public override EBuffBonusCategory BonusCategory1
        {
            get
            {
                if (Spell.Target == ESpellTarget.SELF)
                    return EBuffBonusCategory.Other; // no caps for self buffs

                if (m_spellLine.IsBaseLine)
                    return EBuffBonusCategory.BaseBuff; // baseline cap

                return EBuffBonusCategory.Other; // no caps for spec line buffs
            }
        }
        public override EProperty Property1 { get { return EProperty.ArmorFactor; } }

        // constructor
        public PaladinArmorFactorBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler("FlexibleSkillBuff")]
    public class FlexibleSkillBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.Skill_Flexible_Weapon; } }
        public FlexibleSkillBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    [SpellHandler("ResiPierceBuff")]
    public class ResiPierceBuff : SingleStatBuff
    {
        public override EProperty Property1 { get { return EProperty.ResistPierce; } }
        public ResiPierceBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
