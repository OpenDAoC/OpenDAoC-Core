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
            if (Body.IsWithinRadius(Body.TargetObject, Body.attackComponent.AttackRange + 50))
            {
                if (IsInMeleeMode)
                    return true;

                IsInMeleeMode = true;
                Body.castingComponent?.ClearUpQueuedSpellHandler();
                Body.StartAttack(Body.TargetObject);
                return true;
            }

            IsInMeleeMode = false;
            return base.CheckSpells(type);
        }
    }
}
