using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.Bomber)]
    public class BomberSpellHandler : SummonSpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BomberSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
            m_isSilent = true;
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Spell.SubSpellID == 0)
            {
                MessageToCaster("SPELL NOT IMPLEMENTED: CONTACT GM", eChatType.CT_Important);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);

            if (m_pet is not null)
            {
                m_pet.Level = m_pet.Owner?.Level ?? 1; // No bomber class to override SetPetLevel() in, so set level here.
                m_pet.Name = Spell.Name;
                m_pet.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
                m_pet.Flags ^= GameNPC.eFlags.PEACE;
                m_pet.FixedSpeed = true;
                m_pet.MaxSpeedBase = 350;
                m_pet.TargetObject = target;
                m_pet.Follow(target, 5, Spell.Range * 5);
            }
        }

        public override void OnPetReleased(GameSummonedPet pet) { }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new BomberBrain(owner, Spell, SpellLine);
        }

        protected override void SetBrainToOwner(IControlledBrain brain) { }

        public override void CastSubSpells(GameLiving target) { }
    }
}
