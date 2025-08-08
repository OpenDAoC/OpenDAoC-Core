using System;

namespace DOL.GS.PerformanceStatistics
{
    public class PerSecondStatistic : IPerformanceStatistic
    {
        private DateTime _lastMeasurementTime;
        private readonly IPerformanceStatistic _totalValueStatistic;
        private double _lastTotal;

        public PerSecondStatistic(IPerformanceStatistic totalValueStatistic)
        {
            _totalValueStatistic = totalValueStatistic;
            _lastMeasurementTime = DateTime.UtcNow;
            _lastTotal = _totalValueStatistic.GetNextValue();
        }

        public double GetNextValue()
        {
            if (_totalValueStatistic == null)
                return 0.0;

            DateTime currentTime = DateTime.UtcNow;
            double currentTotal = _totalValueStatistic.GetNextValue();
            double secondsPassed = (currentTime - _lastMeasurementTime).TotalSeconds;

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
