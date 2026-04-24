using System.Runtime.CompilerServices;
using DOL.Timing;

namespace ECS.Debug
{
    public ref struct TickMonitor
    {
        private readonly bool _isEnabled = Diagnostics.EnableTickProfiling;
        private readonly long _startTick;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TickMonitor()
        {
            _startTick = _isEnabled ? MonotonicTime.NowMs : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsLongTick(out long elapsedMs)
        {
            if (!_isEnabled)
            {
                elapsedMs = 0;
                return false;
            }

            elapsedMs = MonotonicTime.NowMs - _startTick;
            return elapsedMs > Diagnostics.LongTickThreshold;
        }
    }
}
