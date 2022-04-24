using System;
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
	public class SummonJuggernaut : SummonSimulacrum
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
		
		protected override void AddHandlers()
		{
			GameEventMgr.AddHandler(m_pet, GameLivingEvent.Dying, OnPetDying);
			base.AddHandlers();
		}
		
		protected override void RemoveHandlers()
		{
			GameEventMgr.RemoveHandler(m_pet, GameLivingEvent.Dying, OnPetDying);
			base.AddHandlers();
		}
		
		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new JuggernautBrain(owner);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			RemoveHandlers();
			effect.Owner.Health = 0; // to send proper remove packet
			effect.Owner.Delete();
			return 0;
		}
		
		protected override void OnNpcReleaseCommand(DOLEvent e, object sender, EventArgs arguments)
		{
			var pet = sender as GamePet;
			var player = pet?.Owner as GamePlayer;
			if (player == null)
				return;

			AtlasOF_JuggernautECSEffect effect = (AtlasOF_JuggernautECSEffect)EffectListService.GetEffectOnTarget(player, eEffect.Juggernaut);

			effect?.Cancel(false);

			base.OnNpcReleaseCommand(e, sender, arguments);
		}

		protected void OnPetDying(DOLEvent e, object sender, EventArgs arguments)
		{
			if (e != GameLivingEvent.Dying || sender is not GamePet)
				return;
			var pet = sender as GamePet;
			var player = pet?.Owner as GamePlayer;
			if (player == null)
				return;

			AtlasOF_JuggernautECSEffect effect = (AtlasOF_JuggernautECSEffect)EffectListService.GetEffectOnTarget(player, eEffect.Juggernaut);

			effect?.Cancel(false);
			
		}
	}
}
