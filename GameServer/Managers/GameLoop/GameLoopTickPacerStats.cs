using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace DOL.GS
{
    public class GameLoopTickPacerStats
    {
        private readonly double[] _buffer;
        private readonly uint _capacity;
        private readonly List<int> _intervals;
        private uint _writeIndex;

        public GameLoopTickPacerStats(List<int> intervals, double tickDuration)
        {
            if (intervals == null || intervals.Count == 0)
                throw new ArgumentNullException(nameof(intervals), "Intervals cannot be null or empty.");

            if (tickDuration <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be a positive value.");

            _intervals = intervals.OrderByDescending(x => x).ToList();
            _capacity = BitOperations.RoundUpToPowerOf2((uint) (_intervals[0] / 1000.0 * tickDuration * 2));
            _buffer = new double[_capacity];
        }

        public void RecordTick(double gameLoopTime)
        {
            uint index = Interlocked.Increment(ref _writeIndex) - 1;
            _buffer[index & (_capacity - 1)] = gameLoopTime;
        }

        public List<(int, double)> GetAverageTicks()
        {
            List<(int, double)> result = new(_intervals.Count);

            // Fast-path: We are on the game loop, run directly.
            if (SynchronizationContext.Current is GameServiceSynchronizationContext)
            {
                GetAverageTicksInternal(result);
                return result;
            }

            // Slow-path: We are on an external thread. Use Send to marshal the call.
            GameServiceSynchronizationContext context = GameServiceContext.GetContextFor(GameLoopService.Instance);

            context.Send(static state =>
            {
                var (result, tickPacer) = ((List<(int, double)>, GameLoopTickPacerStats)) state;
                tickPacer.GetAverageTicksInternal(result);
            }, (result, this));

            return result;
        }

        private void GetAverageTicksInternal(List<(int, double)> result)
        {
            uint writeIndex = Volatile.Read(ref _writeIndex);
            int count = (int) Math.Min(writeIndex, _capacity);

            if (count <= 0)
            {
                foreach (int interval in _intervals)
                    result.Add((interval, 0));

                return;
            }

            uint mask = _capacity - 1;
            uint start = writeIndex >= _capacity ? writeIndex & mask : 0;
            double latestTick = _buffer[(start + (uint) (count - 1)) & mask];

            int startIndex = 0;

            // Count ticks per interval and calculate averages.
            foreach (int interval in _intervals)
            {
                double intervalStart = latestTick - interval;
                int tickCount = 0;
                int intervalStartIndex = startIndex;

                // Find the number of ticks within this interval.
                for (int i = startIndex; i < count; i++)
                {
                    double tick = _buffer[(start + (uint) i) & mask];

                    if (tick >= intervalStart)
                    {
                        tickCount = count - i;
                        startIndex = i;
                        intervalStartIndex = i;
                        break;
                    }
                }

                if (tickCount < 2)
                {
                    result.Add((interval, 0));
                    continue;
                }

                double firstTick = _buffer[(start + (uint) intervalStartIndex) & mask];
                double actualInterval = latestTick - firstTick;

                if (actualInterval <= 0)
                {
                    result.Add((interval, 0));
                    continue;
                }

                double average = (tickCount - 1) / (actualInterval / 1000.0);
                result.Add((interval, average));
            }
        }
    }
}
