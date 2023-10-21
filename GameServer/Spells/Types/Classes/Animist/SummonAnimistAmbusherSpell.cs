using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.Spells
{
	[SpellHandler("SummonAnimistAmbusher")]
	public class SummonAnimistAmbusherSpell : SummonSpellHandler
	{
		public SummonAnimistAmbusherSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
		
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			OfRaForestheartAmbusherEcsEffect effect = (OfRaForestheartAmbusherEcsEffect)EffectListService.GetEffectOnTarget(target, EEffect.ForestheartAmbusher);

			// The effect may have been cancelled already, in which case we shouldn't spawn the pet.
			// This could happen if the player dies before this method is called by the casting service.
			if (effect != null)
				effect.PetSpellHander = this;
			else
				return;

			base.ApplyEffectOnTarget(target);

			m_pet.Brain.Think();
			((ControlledNpcBrain)m_pet.Brain).Stay();
		}

		protected override GameSummonedPet GetGamePet(INpcTemplate template)
		{
			return new GameSummonedPet(template);
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

		protected override void OnNpcReleaseCommand(CoreEvent e, object sender, EventArgs arguments)
		{
			if (e != GameLivingEvent.PetReleased || sender is not GameNpc gameNpc)
				return;

			if (gameNpc.Brain is not ForestheartAmbusherBrain forestheartAmbusherBrain)
				return;

			var player = forestheartAmbusherBrain.Owner as GamePlayer;

			if (player == null)
				return;

			OfRaForestheartAmbusherEcsEffect effect = (OfRaForestheartAmbusherEcsEffect)EffectListService.GetEffectOnTarget(player, EEffect.ForestheartAmbusher);
			effect?.Cancel(false);

			base.OnNpcReleaseCommand(e, sender, arguments);
		}
	}
}
