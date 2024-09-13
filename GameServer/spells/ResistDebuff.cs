using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Base class for all resist debuffs, needed to set effectiveness and duration
    /// </summary>
    public abstract class AbstractResistDebuff(GameLiving caster, Spell spell, SpellLine line) : PropertyChangingSpell(caster, spell, line)
    {
        public abstract string DebuffTypeName { get; }
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.Debuff;

        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new StatDebuffECSEffect(initParams);
        }

        protected override int CalculateEffectDuration(GameLiving target)
        {
            double duration = Spell.Duration;

            duration *= 1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01;
            duration -= duration * target.GetResist(m_spell.DamageType) * 0.01;

            if (duration < 1)
                duration = 1;
            else if (duration > (Spell.Duration * 4))
                duration = Spell.Duration * 4;

            return (int) duration;
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

            if (target is GameNPC npc && npc.Brain is StandardMobBrain brain)
                brain.AddToAggroList(Caster, 1);

            if (Spell.CastTime > 0)
                target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
        }

        public override double CalculateSpellResistChance(GameLiving target)
        {
            double chance = base.CalculateSpellResistChance(target);

            /*
            GameSpellEffect rampage = SpellHandler.FindEffectOnTarget(target, "Rampage");

            if (rampage != null)
                chance += rampage.Spell.Value;
            */

            return Math.Min(100, chance);
        }

        protected override void SendUpdates(GameLiving target)
        {
            base.SendUpdates(target);

            if (target is GamePlayer player)
                player.Out.SendCharResistsUpdate();
        }

        public override IList<string> DelveInfo
        {
            get
            {
                /*
                Function: resistance decrease

                Decreases the target's resistance to the listed damage type.

                Resist decrease Energy: 15
                Target: Targetted
                Range: 1500
                Duration: 15 sec
                Power cost: 13
                Casting time: 2.0 sec
                Damage: Cold
                 */

                List<string> list =
                [
                    LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "ResistDebuff.DelveInfo.Function"),
                    " ",
                    Spell.Description,
                    " ",
                    string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "ResistDebuff.DelveInfo.Decrease", DebuffTypeName, m_spell.Value)),
                    LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Target", Spell.Target),
                ];

                if (Spell.Range != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Range", Spell.Range));

                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + " Permanent.");
                else if (Spell.Duration > 60000)
                    list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min"));
                else if (Spell.Duration != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));

                if (Spell.Power != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));

                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));

                if (Spell.RecastDelay > 60000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + Spell.RecastDelay / 60000 + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
                else if (Spell.RecastDelay > 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay / 1000).ToString() + " sec");

                if (Spell.Concentration != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.ConcentrationCost", Spell.Concentration));

                if (Spell.Radius != 0)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Radius", Spell.Radius));

                if (Spell.DamageType is not eDamageType.Natural)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));

                return list;
            }
        }
    }

    [SpellHandler(eSpellType.BodyResistDebuff)]
    public class BodyResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Body;
        public override string DebuffTypeName => "Body";
    }

    [SpellHandler(eSpellType.ColdResistDebuff)]
    public class ColdResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Cold;
        public override string DebuffTypeName => "Cold";
    }

    [SpellHandler(eSpellType.EnergyResistDebuff)]
    public class EnergyResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Energy;
        public override string DebuffTypeName => "Energy";
    }

    [SpellHandler(eSpellType.HeatResistDebuff)]
    public class HeatResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Heat;
        public override string DebuffTypeName => "Heat";
    }

    [SpellHandler(eSpellType.MatterResistDebuff)]
    public class MatterResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Matter;
        public override string DebuffTypeName => "Matter";
    }

    [SpellHandler(eSpellType.SpiritResistDebuff)]
    public class SpiritResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Spirit;
        public override string DebuffTypeName => "Spirit";
    }

    [SpellHandler(eSpellType.SlashResistDebuff)]
    public class SlashResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Slash;
        public override string DebuffTypeName => "Slash";
    }

    [SpellHandler(eSpellType.ThrustResistDebuff)]
    public class ThrustResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Thrust;
        public override string DebuffTypeName => "Thrust";
    }

    [SpellHandler(eSpellType.CrushResistDebuff)]
    public class CrushResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Crush;
        public override string DebuffTypeName => "Crush";
    }

    [SpellHandler(eSpellType.CrushSlashThrustDebuff)]
    public class CrushSlashThrustDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.Debuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.Debuff;
        public override eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.Debuff;

        public override eProperty Property1 => eProperty.Resist_Crush;
        public override eProperty Property2 => eProperty.Resist_Slash;
        public override eProperty Property3 => eProperty.Resist_Thrust;

        public override string DebuffTypeName => "Crush/Slash/Thrust";
    }

    [SpellHandler(eSpellType.EssenceSear)]
    public class EssenceResistDebuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistDebuff(caster, spell, line)
    {
        public override eProperty Property1 => eProperty.Resist_Natural;
        public override string DebuffTypeName => "Essence";
    }
}
