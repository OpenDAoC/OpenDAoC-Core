namespace DOL.GS.PerformanceStatistics
{
    public class DummyPerformanceStatistic : IPerformanceStatistic
    {
        public static DummyPerformanceStatistic Instance { get; } = new();

        private DummyPerformanceStatistic() { }

        public double GetNextValue()
        {
            return 0;
        }
    }
}
