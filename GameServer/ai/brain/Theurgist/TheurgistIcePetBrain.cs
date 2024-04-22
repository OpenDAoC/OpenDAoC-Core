using DOL.GS;

namespace DOL.AI.Brain
{
    public class TheurgistIcePetBrain : TheurgistPetBrain
    {
        public TheurgistIcePetBrain(GameLiving owner) : base(owner) { }

        private bool IsInMeleeMode { get; set; }

        public override bool CheckSpells(eCheckSpellType type)
        {
            // Ice pets don't check for spells if their target is close, and attack instead.
            // Once ice pets enter melee range, there's no going back.
            if (IsInMeleeMode)
            {
                Body.StartAttack(Body.TargetObject);
                return true;
            }
            else if (Body.IsWithinRadius(Body.TargetObject, Body.attackComponent.AttackRange + 50))
            {
                IsInMeleeMode = true;
                Body.castingComponent?.ClearUpQueuedSpellHandler();
                Body.StartAttack(Body.TargetObject);
                return true;
            }

            return base.CheckSpells(type);
        }
    }
}
