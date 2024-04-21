using DOL.GS;

namespace DOL.AI.Brain
{
    public class TurretMainPetTankBrain : TurretBrain
    {
        public TurretMainPetTankBrain(GameLiving owner) : base(owner) { }

        protected override bool TrustCast(Spell spell, eCheckSpellType type, GameLiving target, bool checkLos)
        {
            // Tank turrets don't check for spells if their target is close, but attack in melee instead.
            if (Body.IsWithinRadius(target, Body.attackComponent.AttackRange))
            {
                Body.StopCurrentSpellcast();
                Body.StartAttack(target);
                return true;
            }
            else
            {
                Body.StopAttack();
                return base.TrustCast(spell, type, target, checkLos);
            }
        }
    }
}
