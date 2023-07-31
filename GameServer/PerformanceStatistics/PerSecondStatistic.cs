using System;

namespace DOL.GS.PerformanceStatistics
{
    public class PerSecondStatistic : IPerformanceStatistic
    {
        private DateTime _lastMeasurementTime;
        private IPerformanceStatistic _totalValueStatistic;
        private double _lastTotal;
        private double _cachedLastStatisticValue;

        public PerSecondStatistic(IPerformanceStatistic totalValueStatistic)
        {
            _lastMeasurementTime = DateTime.UtcNow;
            _totalValueStatistic = totalValueStatistic;
            _lastTotal = totalValueStatistic.GetNextValue();
            _cachedLastStatisticValue = -1;
        }

        public double GetNextValue()
        {
            if (_totalValueStatistic == null)
                return -1;

            DateTime currentTime = DateTime.UtcNow;
            double secondsPassed = (currentTime - _lastMeasurementTime).TotalSeconds;

            if (secondsPassed < 1)
                return _cachedLastStatisticValue;

            double currentTotal = _totalValueStatistic.GetNextValue();
            double valuePerSecond = (currentTotal - _lastTotal) / secondsPassed;

            _lastMeasurementTime = currentTime;
            _lastTotal = currentTotal;
            _cachedLastStatisticValue = valuePerSecond;
            return _cachedLastStatisticValue;
        }
    }
}
