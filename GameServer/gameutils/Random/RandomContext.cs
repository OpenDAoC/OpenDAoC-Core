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

        public static RandomContext Evade(byte styleChainStage)
        {
            return new(RandomEvent.Evade, RandomPolicy.Default, styleChainStage);
        }

        public static RandomContext Parry(byte styleChainStage)
        {
            return new(RandomEvent.Parry, RandomPolicy.Default, styleChainStage);
        }

        public static RandomContext Block(byte styleChainStage)
        {
            return new(RandomEvent.Block, RandomPolicy.Default, styleChainStage);
        }

        public static RandomContext Miss(byte styleChainStage)
        {
            return new(RandomEvent.Miss, RandomPolicy.Default, styleChainStage);
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

        public static RandomContext PhysicalVariance()
        {
            return new(RandomEvent.PhysicalVariance, RandomPolicy.Default);
        }

        public static RandomContext MagicVariance()
        {
            return new(RandomEvent.MagicVariance, RandomPolicy.Default);
        }

        public static RandomContext PhysicalCriticalChance()
        {
            return new(RandomEvent.PhysicalCriticalChance, RandomPolicy.Default);
        }

        public static RandomContext MagicCriticalChance()
        {
            return new(RandomEvent.MagicCriticalChance, RandomPolicy.Default);
        }

        public static RandomContext PhysicalCriticalVariance()
        {
            return new(RandomEvent.PhysicalCriticalVariance, RandomPolicy.Default);
        }

        public static RandomContext MagicCriticalVariance()
        {
            return new(RandomEvent.MagicCriticalVariance, RandomPolicy.Default);
        }
    }
}
