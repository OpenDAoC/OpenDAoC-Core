using DOL.GS;
using DOL.GS.RealmAbilities;

namespace DOL.AI.Brain
{
    public class BdMeleeBrain : BdPetBrain
    {
        public BdMeleeBrain(GameLiving owner) : base(owner) { }

        public override void CheckAbilities()
        {
            CheckCharge();
        }

        public override bool CheckSpells(eCheckSpellType type) { return false; }
    }
}
