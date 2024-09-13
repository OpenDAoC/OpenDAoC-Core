using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.SummonAnimistAmbusher)]
	public class SummonAnimistAmbusher : SummonSpellHandler
	{
		public SummonAnimistAmbusher(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
		
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			AtlasOF_ForestheartAmbusherECSEffect effect = (AtlasOF_ForestheartAmbusherECSEffect)EffectListService.GetEffectOnTarget(target, eEffect.ForestheartAmbusher);

			// The effect may have been cancelled already, in which case we shouldn't spawn the pet.
			// This could happen if the player dies before this method is called by the casting service.
			if (effect == null)
				return;

			effect.PetSpellHander = this;
			base.ApplyEffectOnTarget(target);
			((ControlledMobBrain) m_pet.Brain).Stay();
		}

		public override void OnPetReleased(GameSummonedPet pet)
		{
			if (pet.Brain is not ForestheartAmbusherBrain forestheartAmbusherBrain)
				return;

			AtlasOF_ForestheartAmbusherECSEffect effect = EffectListService.GetEffectOnTarget(Caster, eEffect.ForestheartAmbusher) as AtlasOF_ForestheartAmbusherECSEffect;
			effect?.Cancel(false);
			base.OnPetReleased(pet);
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
	}
}
