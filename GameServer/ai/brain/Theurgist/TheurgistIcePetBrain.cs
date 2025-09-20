using DOL.GS;

namespace DOL.AI.Brain
{
    public class TheurgistIcePetBrain : TheurgistPetBrain
    {
        public TheurgistIcePetBrain(GameLiving owner) : base(owner) { }

        public override bool CheckSpells(eCheckSpellType type)
        {
            // Ice pets don't check for spells if their target is close, and attack instead.
            if (Body.IsWithinRadius(Body.TargetObject, Body.attackComponent.AttackRange + 50))
            {
                Body.castingComponent?.ClearUpQueuedSpellHandler();
                return false;
            }

            return base.CheckSpells(type);
        }
    }
}
