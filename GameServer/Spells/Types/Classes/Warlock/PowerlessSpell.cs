using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandler("Powerless")]
	public class PowerlessSpell : PrimerSpell
	{
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget)) return false;
            GameSpellEffect RangeSpell = SpellHandler.FindEffectOnTarget(Caster, "Range");
  			if(RangeSpell != null) { MessageToCaster("You already preparing a Range spell", EChatType.CT_System); return false; }
            GameSpellEffect UninterruptableSpell = SpellHandler.FindEffectOnTarget(Caster, "Uninterruptable");
  			if(UninterruptableSpell != null) { MessageToCaster("You already preparing a Uninterruptable spell", EChatType.CT_System); return false; }
            GameSpellEffect PowerlessSpell = SpellHandler.FindEffectOnTarget(Caster, "Powerless");
            if (PowerlessSpell != null) { MessageToCaster("You must finish casting Powerless before you can cast it again", EChatType.CT_System); return false; }
            return true;
		}
		
		// constructor
		public PowerlessSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
