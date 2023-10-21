using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Spells
{
	[SpellHandler("BodyResistDebuff")]
	public class BodyResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Body; } }
		public override string DebuffTypeName { get { return "Body"; } }

		// constructor
		public BodyResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("ColdResistDebuff")]
	public class ColdResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Cold; } }
		public override string DebuffTypeName { get { return "Cold"; } }

		// constructor
		public ColdResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("EnergyResistDebuff")]
	public class EnergyResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Energy; } }
		public override string DebuffTypeName { get { return "Energy"; } }

		// constructor
		public EnergyResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("HeatResistDebuff")]
	public class HeatResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Heat; } }
		public override string DebuffTypeName { get { return "Heat"; } }

		// constructor
		public HeatResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("MatterResistDebuff")]
	public class MatterResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Matter; } }
		public override string DebuffTypeName { get { return "Matter"; } }

		// constructor
		public MatterResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("SpiritResistDebuff")]
	public class SpiritResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Spirit; } }
		public override string DebuffTypeName { get { return "Spirit"; } }

		// constructor
		public SpiritResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("SlashResistDebuff")]
	public class SlashResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Slash; } }
		public override string DebuffTypeName { get { return "Slash"; } }

		// constructor
		public SlashResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}

	[SpellHandler("ThrustResistDebuff")]
	public class ThrustResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Thrust; } }
		public override string DebuffTypeName { get { return "Thrust"; } }

		// constructor
		public ThrustResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	
	[SpellHandler("CrushResistDebuff")]
	public class CrushResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Crush; } }
		public override string DebuffTypeName { get { return "Crush"; } }

		// constructor
		public CrushResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	
	[SpellHandler("CrushSlashThrustDebuff")]
	public class CrushSlashThrustDebuffSpell : AResistDebuff
	{
		public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.Debuff; } }
		public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.Debuff; } }
		public override EBuffBonusCategory BonusCategory3 { get { return EBuffBonusCategory.Debuff; } }
		
		public override EProperty Property1 { get { return EProperty.Resist_Crush; } }
		public override EProperty Property2 { get { return EProperty.Resist_Slash; } }
		public override EProperty Property3 { get { return EProperty.Resist_Thrust; } }

		public override string DebuffTypeName { get { return "Crush/Slash/Thrust"; } }

		// constructor
		public CrushSlashThrustDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
	
	[SpellHandler("EssenceSear")]
	public class EssenceResistDebuffSpell : AResistDebuff
	{
		public override EProperty Property1 { get { return EProperty.Resist_Natural; } }
		public override string DebuffTypeName { get { return "Essence"; } }

		// constructor
		public EssenceResistDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
