using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
    public class OfRaReflexAttackAbility : TimedRealmAbility
    {
        public OfRaReflexAttackAbility(DbAbility dba, int level) : base(dba, level) { }

        public const int duration = 30000; // 30 seconds
        public override int MaxLevel { get { return 1; } }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins
        public override int CostForUpgrade(int level) { return 14; }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            new OfRaReflexAttackEcsEffect(new EcsGameEffectInitParams(living, duration, 1));
            DisableSkill(living);
        }
    }
}
