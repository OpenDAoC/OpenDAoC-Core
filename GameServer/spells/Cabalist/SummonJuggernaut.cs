using DOL.GS.Effects;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Events;

using DOL.Language;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell handler to summon a bonedancer pet.
	/// </summary>
	/// <author>IST</author>
	[SpellHandler("SummonJuggernaut")]
	public class SummonJuggernaut : SummonSpellHandler
	{
		public SummonJuggernaut(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line) { }

		public override bool CheckEndCast(GameLiving selectedTarget)
		{
			if (Caster is GamePlayer && ((GamePlayer)Caster).ControlledBrain != null)
			{
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "Summon.CheckBeginCast.AlreadyHaveaPet"), eChatType.CT_SpellResisted);
                return false;
			}
			return base.CheckEndCast(selectedTarget);
		}
		
		
		
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			
			IControlledBrain brain = effect.Owner.ControlledBrain;
			GameLiving living = brain.Owner;
			living.SetControlledBrain(null);
			
			GameEventMgr.RemoveHandler(living, GameLivingEvent.PetReleased, new DOLEventHandler(OnNpcReleaseCommand));

		    RemoveHandlers();
			effect.Owner.Health = 0; // to send proper remove packet
			effect.Owner.Delete();
			return 0;

		}
		
	}
}
