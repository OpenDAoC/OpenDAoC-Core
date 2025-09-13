using System;

namespace DOL.GS
{
    public abstract class TickPoolBase
    {
        protected const int INITIAL_CAPACITY = 64;     // Initial capacity of the pool.
        private const double TRIM_SAFETY_FACTOR = 2.5; // Trimming allowed when size > smoothed usage * this factor.
        private const int HALF_LIFE = 20_000;          // Half-life (ms) for EMA decay.
        private const int TRIM_DELAY_MS = 5_000;       // Delay (ms) before an oversized pool is trimmed.

        private static readonly double _decayFactor;   // EMA decay factor based on HALF_LIFE and tick rate.
        private static readonly int _trimDelayInTicks; // Trim delay period converted into game ticks.

        protected int _used;                           // Objects rented this tick.
        protected int _logicalSize;                    // Current allocated capacity of the pool.
        private double _smoothedUsage;                 // Smoothed recent peak usage.
        private int _trimCooldown;                     // Countdown timer for trimming.

        static TickPoolBase()
        {
            // Will become outdated if `GameLoop.TickDuration` is changed at runtime.
            _decayFactor = Math.Exp(-Math.Log(2) / (GameLoop.TickDuration * HALF_LIFE / 1000));
            _trimDelayInTicks = (int) Math.Ceiling(TRIM_DELAY_MS / GameLoop.TickDuration);
        }

        public void Reset()
        {
            OnResetItems(_used);

            _smoothedUsage = Math.Max(_used, _smoothedUsage * _decayFactor + _used * (1 - _decayFactor));
            int newLogicalSize = (int) (_smoothedUsage * TRIM_SAFETY_FACTOR);

            if (newLogicalSize < INITIAL_CAPACITY)
                newLogicalSize = INITIAL_CAPACITY;

            // If the pool has grown much larger than our smoothed target, trim it.
            if (_logicalSize > newLogicalSize)
            {
                _trimCooldown--;

                if (_trimCooldown <= 0)
                {
                    OnTrim(_logicalSize, newLogicalSize);
                    _logicalSize = newLogicalSize;
                    _trimCooldown = _trimDelayInTicks;
                }
            }
            else
                _trimCooldown = _trimDelayInTicks;

            _used = 0;
        }

        protected abstract void OnResetItems(int itemsInUse);
        protected abstract void OnTrim(int currentSize, int newSize);
    }
}
