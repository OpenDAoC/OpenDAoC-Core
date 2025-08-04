using System;
using System.Collections.Generic;
using System.Threading;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.GS
{
    public class LosCheckHandler
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly GamePlayer _owner;
        private readonly Dictionary<LosCheckKey, TimeoutTimer> _activeTimers = new();
        private readonly Queue<TimeoutTimer> _timerPool = new();
        private readonly Lock _lock = new();

        public LosCheckHandler(GamePlayer owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void SetResponse(ushort sourceObjectId, ushort targetObjectId, LosCheckResponse response)
        {
            TimeoutTimer timer;

            // We're not expecting `SetResponse` to be called concurrently or outside `ClientService`. The lock is just for good measure.
            lock (_lock)
            {
                // If there is no active timer for this check, it may have already timed out or never existed.
                if (!_activeTimers.Remove(new(sourceObjectId, targetObjectId), out timer))
                    return;
            }

            timer.SetResponse(response);
            timer.Tick(); // Execute the callbacks immediately to reduce latency.
        }

        public void StartLosCheck(ushort sourceObjectId, ushort targetObjectId, CheckLosResponse callback)
        {
            lock (_lock)
            {
                LosCheckKey key = new(sourceObjectId, targetObjectId);

                // If a timer already exists for this pair, just add the callback and we're done.
                if (_activeTimers.TryGetValue(key, out TimeoutTimer existingTimer))
                {
                    existingTimer.AddCallback(callback);
                    return;
                }

                // No existing timer, so get one from the pool or create a new one.
                if (!_timerPool.TryDequeue(out TimeoutTimer newTimer))
                    newTimer = new TimeoutTimer(_owner, ReturnTimerToPool);

                // Configure the timer, add it to the dictionary, and start it.
                newTimer.Setup(sourceObjectId, targetObjectId, callback);
                _activeTimers[key] = newTimer;
                newTimer.Start();
            }

            using var pak = GSTCPPacketOut.GetForTick(p => p.Init(AbstractPacketLib.GetPacketCode(eServerPackets.CheckLOSRequest)));
            pak.WriteShort(sourceObjectId);
            pak.WriteShort(targetObjectId);
            pak.WriteShort(0x00); // ?
            pak.WriteShort(0x00); // ?
            _owner.Out.SendTCP(pak);
        }

        private void ReturnTimerToPool(TimeoutTimer timer)
        {
            lock (_lock)
            {
                // Reset the timer's state and add it back to the pool for reuse.
                // It might have already been removed by `SetResponse` already, which is fine.
                _activeTimers.Remove(new(timer.SourceObjectId, timer.TargetObjectId));
                timer.Reset();
                _timerPool.Enqueue(timer);
            }
        }

        private class TimeoutTimer : ECSGameTimerWrapperBase
        {
            public ushort SourceObjectId { get; private set; }
            public ushort TargetObjectId { get; private set; }

            private LosCheckResponse _response;
            private readonly List<CheckLosResponse> _callbacks = new();
            private readonly Action<TimeoutTimer> _completionCallback;

            public TimeoutTimer(GamePlayer owner, Action<TimeoutTimer> completionCallback) : base(owner)
            {
                _completionCallback = completionCallback ?? throw new ArgumentNullException(nameof(completionCallback));
                Interval = ServerProperties.Properties.LOS_CHECK_TIMEOUT;
            }

            public void Setup(ushort sourceObjectId, ushort targetObjectId, CheckLosResponse initialCallback)
            {
                SourceObjectId = sourceObjectId;
                TargetObjectId = targetObjectId;
                _callbacks.Add(initialCallback);
            }

            public void AddCallback(CheckLosResponse callback)
            {
                _callbacks.Add(callback);
            }

            public void SetResponse(LosCheckResponse response)
            {
                _response = response;
            }

            public void Reset()
            {
                Stop();
                _response = LosCheckResponse.None;
                _callbacks.Clear();
                SourceObjectId = 0;
                TargetObjectId = 0;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                // If no response was set, it's a timeout.
                LosCheckResponse response = _response is LosCheckResponse.None ? LosCheckResponse.Timeout : _response;
                GamePlayer player = Owner as GamePlayer;

                foreach (CheckLosResponse callback in _callbacks)
                {
                    try
                    {
                        callback(player, response, SourceObjectId, TargetObjectId);
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"Error in callback for LOS check from {SourceObjectId} to {TargetObjectId} (Owner: {Owner})", e);
                    }
                }

                // Notify the handler that this timer is finished, so it can be pooled.
                _completionCallback(this);
                return 0;
            }
        }

        private readonly struct LosCheckKey : IEquatable<LosCheckKey>
        {
            public readonly ushort SourceObjectId;
            public readonly ushort TargetObjectId;

            public LosCheckKey(ushort sourceObjectId, ushort targetObjectId)
            {
                SourceObjectId = sourceObjectId;
                TargetObjectId = targetObjectId;
            }

            public bool Equals(LosCheckKey other)
            {
                return SourceObjectId == other.SourceObjectId && TargetObjectId == other.TargetObjectId;
            }

            public override bool Equals(object obj)
            {
                return obj is LosCheckKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SourceObjectId, TargetObjectId);
            }

            public static bool operator ==(LosCheckKey left, LosCheckKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(LosCheckKey left, LosCheckKey right)
            {
                return !(left == right);
            }
        }
    }

    public delegate void CheckLosResponse(GamePlayer player, LosCheckResponse response, ushort sourceId, ushort targetId);

    public enum LosCheckResponse
    {
        None,
        True,
        False,
        Timeout
    }
}
