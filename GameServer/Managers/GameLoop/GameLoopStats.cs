using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace DOL.GS
{
    public class GameLoopStats
    {
        private readonly double[] _buffer;
        private readonly uint _capacity;
        private readonly List<int> _intervals;
        private uint _writeIndex;

        public GameLoopStats(List<int> intervals, double expectedTickRate)
        {
            _intervals = intervals.OrderByDescending(x => x).ToList();
            _capacity = BitOperations.RoundUpToPowerOf2((uint) (_intervals[0] / 1000.0 * expectedTickRate * 2));
            _buffer = new double[_capacity];
            _writeIndex = 0;
        }

        public void RecordTick(double gameLoopTime)
        {
            uint index = Interlocked.Increment(ref _writeIndex) - 1;
            _buffer[index & (_capacity - 1)] = gameLoopTime;
        }

        public List<(int, double)> GetAverageTicks(long currentTime)
        {
            double[] snapshot = new double[_capacity];

            // Take a snapshot of the buffer and write index.
            uint currentWriteIndex = Volatile.Read(ref _writeIndex);
            Array.Copy(_buffer, snapshot, _capacity);

            List<double> ticks = new((int) _capacity);

            // Calculate how many valid entries we have and determine the range of valid indices in the ring buffer.
            uint start = currentWriteIndex >= _capacity ? (currentWriteIndex & (_capacity - 1)) : 0;
            uint end = Math.Min(currentWriteIndex, _capacity);

            // Collect valid ticks from the ring buffer.
            for (uint i = 0; i < end; i++)
            {
                uint index = (start + i) & (_capacity - 1);
                double tick = snapshot[index];

                if (tick > 0)
                    ticks.Add(tick);
            }

            List<(int, double)> averages = new();
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
                    averages.Add((interval, 0));
                    continue;
                }

                double actualInterval = ticks[^1] - ticks[startIndex];
                double average = (tickCount - 1) / (actualInterval / 1000.0);
                averages.Add((interval, average));
            }

            return averages;
        }
    }
}
