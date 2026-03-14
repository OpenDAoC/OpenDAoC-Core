using System.Linq;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class TheurgistAirPetBrain : TheurgistPetBrain
    {
        // Theurgist air pets have a certain chance to cast their stun at a regular interval.
        // 8 cast attempts during the pet's lifetime (20s).
        // https://uthgard.net/tracker/issue/4147/@/Theurgist_air_pets_do_not_stun_at_proper_range

        private const int STUN_ATTEMPT_INTERVAL = 2500;
        private const double STUN_CHANCE = 0.25;

        private static readonly int[] _stunAttemptIntervalDivisors = Enumerable.Range(1, STUN_ATTEMPT_INTERVAL).Where(i => STUN_ATTEMPT_INTERVAL % i == 0).ToArray();

        private int _thinkInterval;
        private long _nextStunAttemptTime;

        public override int ThinkInterval => _thinkInterval;

        public TheurgistAirPetBrain(GameLiving owner) : base(owner) { }

        public override bool CheckSpells(eCheckSpellType type)
        {
            // This override is expected to be called on every think tick.

            if (!GameServiceUtils.ShouldTick(_nextStunAttemptTime))
                return false;

            _nextStunAttemptTime += STUN_ATTEMPT_INTERVAL;
            return Util.Chance(STUN_CHANCE) && base.CheckSpells(type);
        }

        public override bool Start()
        {
            if (base.Start())
            {
                // The think interval must be adjusted to ensure the cast attempts happen at the intended time.
                _thinkInterval = AdjustThinkInterval();
                _nextStunAttemptTime = GameLoop.GameLoopTime;
                return true;
            }

            return false;
        }

        private int AdjustThinkInterval()
        {
            int baseInterval = base.ThinkInterval;

            if (baseInterval >= STUN_ATTEMPT_INTERVAL)
                return STUN_ATTEMPT_INTERVAL;

            for (int i = _stunAttemptIntervalDivisors.Length - 1; i >= 0; i--)
            {
                if (_stunAttemptIntervalDivisors[i] <= baseInterval)
                    return _stunAttemptIntervalDivisors[i];
            }

            return 1;
        }
    }
}
