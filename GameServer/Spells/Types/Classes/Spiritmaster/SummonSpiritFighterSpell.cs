using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Spells
{
	[SpellHandler("SummonSpiritFighter")]
	public class SummonSpiritFighterSpell : SummonSpellHandler
	{
		public SummonSpiritFighterSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
		}

		public override bool CheckEndCast(GameLiving selectedTarget)
		{
			if(Caster is GamePlayer && ((GamePlayer)Caster).ControlledBrain != null)
			{
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "Summon.CheckBeginCast.AlreadyHaveaPet"), EChatType.CT_SpellResisted);
                return false;
			}
			return base.CheckEndCast(selectedTarget);
		}
	}
}