namespace DOL.GS
{
    public ref struct RandomContext(RandomEvent randomEvent, RandomPolicy randomPolicy, byte sequenceIndex = 0)
    {
        public readonly RandomEvent RandomEvent = randomEvent;
        public readonly RandomPolicy RandomPolicy = randomPolicy;
        public readonly byte SequenceIndex = sequenceIndex;
    }

    public static class RandomContextFactory
    {
        public static RandomContext Intercept()
        {
            return new(RandomEvent.Intercept, RandomPolicy.Default);
        }

        public static RandomContext Evade(byte swingCount, byte styleChainStage)
        {
            return new(RandomEvent.Evade, GetMeleePolicy(swingCount), styleChainStage);
        }

        public static RandomContext Parry(byte swingCount, byte styleChainStage)
        {
            return new(RandomEvent.Parry, GetMeleePolicy(swingCount), styleChainStage);
        }

        public static RandomContext Block(byte swingCount, byte styleChainStage)
        {
            return new(RandomEvent.Block, GetMeleePolicy(swingCount), styleChainStage);
        }

        public static RandomContext Miss(byte swingCount, byte styleChainStage)
        {
            return new(RandomEvent.Miss, GetMeleePolicy(swingCount), styleChainStage);
        }

        public static RandomContext Resist()
        {
            return new(RandomEvent.Resist, RandomPolicy.Default);
        }

        public static RandomContext DualWield()
        {
            return new(RandomEvent.DualWield, RandomPolicy.Default);
        }

        public static RandomContext OffensiveProcChance()
        {
            return new(RandomEvent.OffensiveProcChance, RandomPolicy.Default);
        }

        public static RandomContext DefensiveProcChance()
        {
            return new(RandomEvent.DefensiveProcChance, RandomPolicy.Default);
        }

        public static RandomContext PhysicalVariance(byte swingCount)
        {
            return new(RandomEvent.PhysicalVariance, GetMeleePolicy(swingCount));
        }

        public static RandomContext MagicVariance()
        {
            return new(RandomEvent.MagicVariance, RandomPolicy.Default);
        }

        public static RandomContext PhysicalCriticalChance(byte swingCount)
        {
            return new(RandomEvent.PhysicalCriticalChance, GetMeleePolicy(swingCount));
        }

        public static RandomContext MagicCriticalChance()
        {
            return new(RandomEvent.MagicCriticalChance, RandomPolicy.Default);
        }

        public static RandomContext PhysicalCriticalVariance(byte swingCount)
        {
            return new(RandomEvent.PhysicalCriticalVariance, GetMeleePolicy(swingCount));
        }

        public static RandomContext MagicCriticalVariance()
        {
            return new(RandomEvent.MagicCriticalVariance, RandomPolicy.Default);
        }

        private static RandomPolicy GetMeleePolicy(int swingCount)
        {
            // Secondary swings use true RNG as to not affect the odds of the first swing.
            return swingCount > 0 ? RandomPolicy.ForceTrueRandom : RandomPolicy.Default;
        }
    }
}
