using DOL.GS;

namespace DOL.AI.Brain
{
    public class BdBufferBrain : BdPetBrain
    {
        public BdBufferBrain(GameLiving owner) : base(owner) { }

        public override void Think()
        {
            if (base.CheckSpells(eCheckSpellType.Defensive))
                Body.StopAttack();
            else
                base.Think();
        }
    }
}
