namespace DOL.GS
{
    public ref struct RandomContext(RandomEvent randomEvent, RandomPolicy randomPolicy)
    {
        public readonly RandomEvent RandomEvent = randomEvent;
        public readonly RandomPolicy RandomPolicy = randomPolicy;
    }

    public static class RandomContextFactory
    {
        public static RandomContext Intercept()
        {
            return new(RandomEvent.Intercept, RandomPolicy.Default);
        }

        public static RandomContext Evade()
        {
            return new(RandomEvent.Evade, RandomPolicy.Default);
        }

        public static RandomContext Parry()
        {
            return new(RandomEvent.Parry, RandomPolicy.Default);
        }

        public static RandomContext Block()
        {
            return new(RandomEvent.Block, RandomPolicy.Default);
        }

        public static RandomContext Miss()
        {
            return new(RandomEvent.Miss, RandomPolicy.Default);
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

        public static RandomContext Variance()
        {
            return new(RandomEvent.Variance, RandomPolicy.Default);
        }

        public static RandomContext CriticalChance()
        {
            return new(RandomEvent.CriticalChance, RandomPolicy.Default);
        }

        public static RandomContext CriticalVariance()
        {
            return new(RandomEvent.CriticalVariance, RandomPolicy.Default);
        }
    }
}
