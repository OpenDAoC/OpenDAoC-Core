namespace DOL.GS.Spells
{
    /// <summary>
    /// Base class for all resist buffs, needed to set effectiveness
    /// </summary>
    public abstract class AbstractResistBuff(GameLiving caster, Spell spell, SpellLine line) : PropertyChangingSpell(caster, spell, line)
    {
        public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            return new StatBuffECSEffect(initParams);
        }

        protected override void SendUpdates(GameLiving target)
        {
            base.SendUpdates(target);

            if (target is GamePlayer)
            {
                GamePlayer player = (GamePlayer)target;
                player.Out.SendCharResistsUpdate();
            }
        }
    }

    [SpellHandlerAttribute("BodyResistBuff")]
    public class BodyResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Body;
    }

    [SpellHandlerAttribute("ColdResistBuff")]
    public class ColdResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Cold;
    }

    [SpellHandlerAttribute("EnergyResistBuff")]
    public class EnergyResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Energy;
    }

    [SpellHandlerAttribute("HeatResistBuff")]
    public class HeatResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Heat;
    }

    [SpellHandlerAttribute("MatterResistBuff")]
    public class MatterResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Matter;
    }

    [SpellHandlerAttribute("SpiritResistBuff")]
    public class SpiritResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Spirit;
    }

    [SpellHandlerAttribute("BodySpiritEnergyBuff")]
    public class BodySpiritEnergyBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.BaseBuff;

        public override eProperty Property1 => eProperty.Resist_Body;
        public override eProperty Property2 => eProperty.Resist_Spirit;
        public override eProperty Property3 => eProperty.Resist_Energy;
    }

    [SpellHandlerAttribute("HeatColdMatterBuff")]
    public class HeatColdMatterBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.BaseBuff;

        public override eProperty Property1 => eProperty.Resist_Heat;
        public override eProperty Property2 => eProperty.Resist_Cold;
        public override eProperty Property3 => eProperty.Resist_Matter;
    }

    [SpellHandlerAttribute("AllMagicResistsBuff")]
    public class AllMagicResistsBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory4 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory5 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory6 => eBuffBonusCategory.BaseBuff;

        public override eProperty Property1 => eProperty.Resist_Heat;
        public override eProperty Property2 => eProperty.Resist_Cold;
        public override eProperty Property3 => eProperty.Resist_Matter;
        public override eProperty Property4 => eProperty.Resist_Body;
        public override eProperty Property5 => eProperty.Resist_Spirit;
        public override eProperty Property6 => eProperty.Resist_Energy;
    }

    [SpellHandlerAttribute("CrushSlashThrustBuff")]
    [SpellHandlerAttribute("AllMeleeResistsBuff")]
    public class CrushSlashThrustBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.BaseBuff;

        public override eProperty Property1 => eProperty.Resist_Crush;
        public override eProperty Property2 => eProperty.Resist_Slash;
        public override eProperty Property3 => eProperty.Resist_Thrust;
    }

    [SpellHandlerAttribute("CrushResistBuff")]
    public class CrushResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Crush;
    }

    [SpellHandlerAttribute("SlashResistBuff")]
    public class SlashResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Slash;
    }

    [SpellHandlerAttribute("ThrustResistBuff")]
    public class ThrustResistBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eProperty Property1 => eProperty.Resist_Thrust;
    }

    [SpellHandlerAttribute("AllResistsBuff")]
    public class AllResistsBuff(GameLiving caster, Spell spell, SpellLine line) : AbstractResistBuff(caster, spell, line)
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory4 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory5 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory6 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory7 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory8 => eBuffBonusCategory.BaseBuff;
        public override eBuffBonusCategory BonusCategory9 => eBuffBonusCategory.BaseBuff;

        public override eProperty Property1 => eProperty.Resist_Heat;
        public override eProperty Property2 => eProperty.Resist_Cold;
        public override eProperty Property3 => eProperty.Resist_Matter;
        public override eProperty Property4 => eProperty.Resist_Body;
        public override eProperty Property5 => eProperty.Resist_Spirit;
        public override eProperty Property6 => eProperty.Resist_Energy;
        public override eProperty Property7 => eProperty.Resist_Crush;
        public override eProperty Property8 => eProperty.Resist_Slash;
        public override eProperty Property9 => eProperty.Resist_Thrust;
    }
}
