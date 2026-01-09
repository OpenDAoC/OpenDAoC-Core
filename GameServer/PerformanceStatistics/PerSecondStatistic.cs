using System;
using DOL.Timing;

namespace DOL.GS.PerformanceStatistics
{
    public class PerSecondStatistic : IPerformanceStatistic
    {
        private long _lastMeasurementTime;
        private readonly IPerformanceStatistic _totalValueStatistic;
        private double _lastTotal;

        public PerSecondStatistic(IPerformanceStatistic totalValueStatistic)
        {
            _totalValueStatistic = totalValueStatistic;
            _lastMeasurementTime = MonotonicTime.NowMs;
            _lastTotal = _totalValueStatistic.GetNextValue();
        }

        public double GetNextValue()
        {
            if (_totalValueStatistic == null)
                return 0.0;

            long currentTime = MonotonicTime.NowMs;
            double currentTotal = _totalValueStatistic.GetNextValue();
            double secondsPassed = (currentTime - _lastMeasurementTime) / 1000.0;

            if (secondsPassed <= 0)
                return 0.0;

            double valueChange = currentTotal - _lastTotal;
            double valuePerSecond = valueChange / secondsPassed;

            _lastMeasurementTime = currentTime;
            _lastTotal = currentTotal;
            return Math.Max(0, valuePerSecond);
        }
    }
}
