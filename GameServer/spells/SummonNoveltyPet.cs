using System.Collections.Generic;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    /// <summary>
    /// This pet is purely aesthetic and can't be cast in RvR zones
    /// </summary>
    [SpellHandler(eSpellType.SummonNoveltyPet)]
    public class SummonNoveltyPet : SummonSpellHandler
    {
        /// <summary>
        /// Constructs the spell handler
        /// </summary>
		public SummonNoveltyPet(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);

			if (m_pet != null)
			{
				m_pet.Flags |= GameNPC.eFlags.PEACE; //must be peace!
				m_pet.Name = $"{Caster.Name}'s {m_pet.Name}";

				//No brain for now, so just follow owner.
				m_pet.Follow(Caster, 100, WorldMgr.VISIBILITY_DISTANCE);

				Caster.TempProperties.SetProperty(NoveltyPetBrain.HAS_PET, true);
			}
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster.CurrentZone.IsRvR)
            {
                MessageToCaster("You cannot summon your pet here!", PacketHandler.eChatType.CT_SpellResisted);
                return false;
            }

			if (Caster.TempProperties.GetProperty<bool>(NoveltyPetBrain.HAS_PET))
			{
				// no message
				MessageToCaster("You already have a pet by your side!", PacketHandler.eChatType.CT_SpellResisted);
				return false;
			}

            return base.CheckBeginCast(selectedTarget);
        }

        /// <summary>
        /// These pets aren't controllable!
        /// </summary>
        /// <param name="brain"></param>
        protected override void SetBrainToOwner(IControlledBrain brain)
        {
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new NoveltyPetBrain(owner as GamePlayer);
        }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(ShortDescription);

				return list;
			}
		}
    }
}
