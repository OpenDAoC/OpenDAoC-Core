using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.AstralPetSummon)]
    public class AstralPetSummon : SummonSpellHandler
    {
        public AstralPetSummon(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);

            m_pet.TempProperties.SetProperty("target", target);
            (m_pet.Brain as IOldAggressiveBrain).AddToAggroList(target, 1);
            (m_pet.Brain as ProcPetBrain).Think();

        }

        public override void OnPetReleased(GameSummonedPet pet)
        {
            Effects.GameSpellEffect effect = FindEffectOnTarget(pet, this);
            effect?.Cancel(false);
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template)
        {
            return new AstralPet(template);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new ProcPetBrain(owner);
        }

        protected override void SetBrainToOwner(IControlledBrain brain) { }



        protected override void GetPetLocation(out int x, out int y, out int z, out ushort heading, out Region region)
        {
            base.GetPetLocation(out x, out y, out z, out heading, out region);
            heading = Caster.Heading;
        }
    }
}

namespace DOL.GS
{
    public class AstralPet : GameSummonedPet
    {
        public override int MaxHealth => Level * 10;

        public AstralPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

        public override void OnAttackedByEnemy(AttackData ad) { }
    }
}
