using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class TheurgistIcePetBrain : TheurgistPetBrain
{
    public TheurgistIcePetBrain(GameLiving owner) : base(owner) { }

    private bool DontAttemptToCastAgain { get; set; }

    public override bool CheckSpells(ECheckSpellType type)
    {
        // Ice pets don't check for spells if their target is close, and attack instead.
        // Once ice pets enter melee range, there's no going back.
        if (Body.IsWithinRadius(Body.TargetObject, Body.attackComponent.AttackRange) || DontAttemptToCastAgain)
        {
            DontAttemptToCastAgain = true;
            Body.StartAttack(Body.TargetObject);
            return true;
        }
        else
            return base.CheckSpells(type) || Body.IsCasting;
    }
}