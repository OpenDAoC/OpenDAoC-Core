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
            if (SynchronizationContext.Current == GameLoopThreadPool.Context)
            {
                GetAverageTicksInternal(GameLoop.GameLoopTime, result);
                return result;
            }

            // Slow-path: We are on an external thread. Use Send to marshal the call.
            GameLoopThreadPool.Context.Send(static state =>
            {
                var (result, tickPacer) = ((List<(int, double)>, GameLoopTickPacerStats)) state;
                tickPacer.GetAverageTicksInternal(GameLoop.GameLoopTime, result);
            }, (result, this));

            return result;
        }

        private void GetAverageTicksInternal(long currentTime, List<(int, double)> result)
        {
            List<double> ticks = new((int) _capacity);

            // Calculate how many valid entries we have and determine the range of valid indices in the ring buffer.
            uint start = _writeIndex >= _capacity ? (_writeIndex & (_capacity - 1)) : 0;
            uint end = Math.Min(_writeIndex, _capacity);

            // Collect valid ticks from the ring buffer.
            for (uint i = 0; i < end; i++)
            {
                uint index = (start + i) & (_capacity - 1);
                double tick = _buffer[index];

                if (tick > 0)
                    ticks.Add(tick);
            }

            int startIndex = 0;

            // Count ticks per interval and calculate averages.
            foreach (int interval in _intervals)
            {
                double intervalStart = currentTime - interval;
                int tickCount = 0;

                // Find the number of ticks within this interval.
                for (int i = startIndex; i < ticks.Count; i++)
                {
                    if (ticks[i] >= intervalStart)
                    {
                        tickCount = ticks.Count - i;
                        startIndex = i;
                        break;
                    }
                }

                if (tickCount < 2)
                {
                    result.Add((interval, 0));
                    continue;
                }

                double actualInterval = ticks[^1] - ticks[startIndex];
                double average = (tickCount - 1) / (actualInterval / 1000.0);
                result.Add((interval, average));
            }
        }
    }
}
