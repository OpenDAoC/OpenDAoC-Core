using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	[SpellHandler("SummonAnimistAmbusher")]
	public class SummonAnimistAmbusher : SummonSpellHandler
	{
		public SummonAnimistAmbusher(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
		
		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			base.ApplyEffectOnTarget(target, effectiveness);

			m_pet.Brain.Think();
			((ControlledNpcBrain)m_pet.Brain).Stay();

			AtlasOF_ForestheartAmbusherECSEffect effect = (AtlasOF_ForestheartAmbusherECSEffect)EffectListService.GetEffectOnTarget(target, eEffect.ForestheartAmbusher);
			effect.PetSpellHander = this;
		}

		protected override GamePet GetGamePet(INpcTemplate template)
		{
			return new GamePet(template);
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new ForestheartAmbusherBrain(owner);
		}
		
		protected override void GetPetLocation(out int x, out int y, out int z, out ushort heading, out Region region)
		{
			x = Caster.GroundTarget.X;
			y = Caster.GroundTarget.Y;
			z = Caster.GroundTarget.Z;
			heading = Caster.Heading;
			region = Caster.CurrentRegion;
		}

		protected override void SetBrainToOwner(IControlledBrain brain) { }

		protected override void OnNpcReleaseCommand(DOLEvent e, object sender, EventArgs arguments)
		{
			if (e != GameLivingEvent.PetReleased || sender is not GameNPC gameNpc)
				return;

			if (gameNpc.Brain is not ForestheartAmbusherBrain forestheartAmbusherBrain)
				return;

			var player = forestheartAmbusherBrain.Owner as GamePlayer;

			if (player == null)
				return;

			AtlasOF_ForestheartAmbusherECSEffect effect = (AtlasOF_ForestheartAmbusherECSEffect)EffectListService.GetEffectOnTarget(player, eEffect.ForestheartAmbusher);
			effect?.Cancel(false);

			base.OnNpcReleaseCommand(e, sender, arguments);
		}
	}
}
