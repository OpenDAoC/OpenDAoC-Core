using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    public class GameLoopStats
    {
        private ConcurrentQueue<double> _tickTimestamps = new();
        private List<int> _intervals;

        public GameLoopStats(List<int> intervals)
        {
            // Intervals to use for average ticks per second. Must be in descending order.
            _intervals = intervals.OrderByDescending(x => x).ToList();
        }

        public void RecordTick(double gameLoopTime)
        {
            double oldestAllowed = gameLoopTime - _intervals[0];

            // Clean up outdated timestamps to prevent the queue from growing indefinitely.
            while (_tickTimestamps.TryPeek(out double _oldestTickTimestamp) && _oldestTickTimestamp < oldestAllowed)
                _tickTimestamps.TryDequeue(out _);

            _tickTimestamps.Enqueue(gameLoopTime);
        }

        public List<(int, double)> GetAverageTicks(long currentTime)
        {
            List<(int, double)> averages = new();
            List<double> snapshot = _tickTimestamps.ToList(); // Copy for thread safety.
            int startIndex = 0;

            // Count ticks per interval and calculate averages.
            foreach (int interval in _intervals)
            {
                double intervalStart = currentTime - interval;
                int tickCount = 0;

                // Find the number of ticks within this interval.
                for (int i = startIndex; i < snapshot.Count; i++)
                {
                    if (snapshot[i] >= intervalStart)
                    {
                        tickCount = snapshot.Count - i;
                        startIndex = i;
                        break;
                    }
                }

                if (tickCount < 2)
                {
                    averages.Add((interval, 0));
                    continue;
                }

                double actualInterval = snapshot[^1] - snapshot[startIndex];
                double average = (tickCount - 1) / (actualInterval / 1000.0);
                averages.Add((interval, average));
            }

            return averages;
        }
    }
}
