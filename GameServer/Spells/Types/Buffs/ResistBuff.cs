using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells;

/// <summary>
/// Base class for all resist buffs, needed to set effectiveness
/// </summary>
public abstract class AResistBuff : PropertyChangingSpell
{
	public override void ApplyEffectOnTarget(GameLiving target)
	{
		Effectiveness *= (1.0 + m_caster.GetModified(EProperty.BuffEffectiveness) * 0.01);
		base.ApplyEffectOnTarget(target);
	}

    public override void CreateECSEffect(EcsGameEffectInitParams initParams)
    {
		new StatBuffEcsSpellEffect(initParams);
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

	public AResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Body resistance buff
/// </summary>
[SpellHandler("BodyResistBuff")]
public class BodyResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Body; } }

	// constructor
	public BodyResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Cold resistance buff
/// </summary>
[SpellHandler("ColdResistBuff")]
public class ColdResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Cold; } }

	// constructor
	public ColdResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Energy resistance buff
/// </summary>
[SpellHandler("EnergyResistBuff")]
public class EnergyResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Energy; } }

	// constructor
	public EnergyResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Heat resistance buff
/// </summary>
[SpellHandler("HeatResistBuff")]
public class HeatResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Heat; } }

	// constructor
	public HeatResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Matter resistance buff
/// </summary>
[SpellHandler("MatterResistBuff")]
public class MatterResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Matter; } }

	// constructor
	public MatterResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Spirit resistance buff
/// </summary>
[SpellHandler("SpiritResistBuff")]
public class SpiritResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Spirit; } }

	// constructor
	public SpiritResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Body/Spirit/Energy resistance buff
/// </summary>
[SpellHandler("BodySpiritEnergyBuff")]
public class BodySpiritEnergyBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory3 { get { return EBuffBonusCategory.BaseBuff; } }

	public override EProperty Property1 { get { return EProperty.Resist_Body; } }
	public override EProperty Property2 { get { return EProperty.Resist_Spirit; } }
	public override EProperty Property3 { get { return EProperty.Resist_Energy; } }

	// constructor
	public BodySpiritEnergyBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Heat/Cold/Matter resistance buff
/// </summary>
[SpellHandler("HeatColdMatterBuff")]
public class HeatColdMatterBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory3 { get { return EBuffBonusCategory.BaseBuff; } }

	public override EProperty Property1 { get { return EProperty.Resist_Heat; } }
	public override EProperty Property2 { get { return EProperty.Resist_Cold; } }
	public override EProperty Property3 { get { return EProperty.Resist_Matter; } }

	// constructor
	public HeatColdMatterBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Body/Spirit/Energy/Heat/Cold/Matter resistance buff
/// </summary>
[SpellHandler("AllMagicResistsBuff")]
public class AllMagicResistsBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory3 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory4 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory5 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory6 { get { return EBuffBonusCategory.BaseBuff; } }
	
	public override EProperty Property1 { get { return EProperty.Resist_Heat; } }
	public override EProperty Property2 { get { return EProperty.Resist_Cold; } }
	public override EProperty Property3 { get { return EProperty.Resist_Matter; } }
	public override EProperty Property4 { get { return EProperty.Resist_Body; } }
	public override EProperty Property5 { get { return EProperty.Resist_Spirit; } }
	public override EProperty Property6 { get { return EProperty.Resist_Energy; } }

	// constructor
	public AllMagicResistsBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Crush/Slash/Thrust resistance buff
/// </summary>
[SpellHandler("CrushSlashThrustBuff")]
[SpellHandler("AllMeleeResistsBuff")]
public class CrushSlashThrustBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory3 { get { return EBuffBonusCategory.BaseBuff; } }

	public override EProperty Property1 { get { return EProperty.Resist_Crush; } }
	public override EProperty Property2 { get { return EProperty.Resist_Slash; } }
	public override EProperty Property3 { get { return EProperty.Resist_Thrust; } }

	// constructor
	public CrushSlashThrustBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

[SpellHandler("CrushResistBuff")]
public class CrushResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Crush; } }

	// constructor
	public CrushResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Slash buff
/// </summary>
[SpellHandler("SlashResistBuff")]
public class SlashResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Slash; } }

	// constructor
	public SlashResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Thrust buff
/// </summary>
[SpellHandler("ThrustResistBuff")]
public class ThrustResistBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EProperty Property1 { get { return EProperty.Resist_Thrust; } }

	// constructor
	public ThrustResistBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}

/// <summary>
/// Resist all 
/// </summary>
[SpellHandler("AllResistsBuff")]
public class AllResistsBuff : AResistBuff
{
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory3 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory4 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory5 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory6 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory7 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory8 { get { return EBuffBonusCategory.BaseBuff; } }
	public override EBuffBonusCategory BonusCategory9 { get { return EBuffBonusCategory.BaseBuff; } }

	public override EProperty Property1 { get { return EProperty.Resist_Heat; } }
	public override EProperty Property2 { get { return EProperty.Resist_Cold; } }
	public override EProperty Property3 { get { return EProperty.Resist_Matter; } }
	public override EProperty Property4 { get { return EProperty.Resist_Body; } }
	public override EProperty Property5 { get { return EProperty.Resist_Spirit; } }
	public override EProperty Property6 { get { return EProperty.Resist_Energy; } }
	public override EProperty Property7 { get { return EProperty.Resist_Crush; } }
	public override EProperty Property8 { get { return EProperty.Resist_Slash; } }
	public override EProperty Property9 { get { return EProperty.Resist_Thrust; } }

	// constructor
	public AllResistsBuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}