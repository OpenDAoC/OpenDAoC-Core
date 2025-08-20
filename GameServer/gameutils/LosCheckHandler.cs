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
        private readonly Dictionary<LosCheckKey, LosCheckTimer> _timers = new();
        private readonly Queue<LosCheckTimer> _timerPool = new();
        private readonly Lock _lock = new();

        public LosCheckHandler(GamePlayer owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void HandleLosResponse(ushort sourceObjectId, ushort targetObjectId, LosCheckResponse response)
        {
            LosCheckTimer timer;

            lock (_lock)
            {
                // If there is no active timer for this check, it may have already timed out or never existed.
                if (!_timers.TryGetValue(new(sourceObjectId, targetObjectId), out timer))
                    return;
            }

            // Execute the callbacks immediately to reduce latency.
            // This should be fine since `TimerService` isn't ticking yet.
            // Similarly, we're not expecting Response to be accessed concurrently here.
            timer.Response = response;
            timer.Tick();
        }

        public void StartLosCheck(GameObject source, GameObject target, CheckLosResponse callback)
        {
            ushort sourceObjectId = (ushort) source.ObjectID;
            ushort targetObjectId = (ushort) target.ObjectID;
            long sourceObjectLastMovementTick = source is not GameLiving livingSource ? 0 : livingSource.movementComponent.LastMovementTick;
            long targetObjectLastMovementTick = target is not GameLiving livingTarget ? 0 : livingTarget.movementComponent.LastMovementTick;

            lock (_lock)
            {
                LosCheckKey key = new(sourceObjectId, targetObjectId);

                // If there's a timer for this pair but no cached response, it means we're still waiting for a reply. Add the callback.
                // If the timer has a cached response, check if it's still valid.
                // * If so, invoke the callback directly and update the timer's expire time.
                // * If not, reset the timer and send a packet.
                // Note: cached responses aren't invalidated when a door between both entities opens or closes, at least until either moves.
                if (_timers.TryGetValue(key, out LosCheckTimer timer))
                {
                    if (!timer.HasCachedResponse)
                    {
                        timer.AddCallback(callback);
                        return;
                    }

                    if (sourceObjectLastMovementTick <= timer.SourceLastMovementTick &&
                        targetObjectLastMovementTick <= timer.TargetLastMovementTick)
                    {
                        callback(_owner, timer.Response, sourceObjectId, targetObjectId);
                        timer.UpdateResponseExpireTime();
                        return;
                    }

                    timer.Setup(sourceObjectId, targetObjectId, sourceObjectLastMovementTick, targetObjectLastMovementTick, callback);
                }
                else
                {
                    if (!_timerPool.TryDequeue(out LosCheckTimer newTimer))
                        newTimer = new(_owner, ReturnTimerToPool);

                    newTimer.Setup(sourceObjectId, targetObjectId, sourceObjectLastMovementTick, targetObjectLastMovementTick, callback);
                    _timers[key] = newTimer;
                }
            }

            using var pak = GSTCPPacketOut.GetForTick(p => p.Init(AbstractPacketLib.GetPacketCode(eServerPackets.CheckLOSRequest)));
            pak.WriteShort(sourceObjectId);
            pak.WriteShort(targetObjectId);
            pak.WriteShort(0x00); // ?
            pak.WriteShort(0x00); // ?
            _owner.Out.SendTCP(pak);
        }

        private void ReturnTimerToPool(LosCheckTimer timer)
        {
            lock (_lock)
            {
                // Remove the timer from the dictionary, reset its state, then add it back to the pool for reuse.
                if (!_timers.Remove(new(timer.SourceObjectId, timer.TargetObjectId)))
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Attempted to remove a LoS check timer for an invalid pair of object IDs ({timer}) (CurrentTime = {GameLoop.GameLoopTime})");
                }

                timer.Clear();
                _timerPool.Enqueue(timer);
            }
        }

        private class LosCheckTimer : ECSGameTimerWrapperBase
        {
            private const int CACHED_RESPONSE_LIFETIME = 5000;

            public ushort SourceObjectId { get; private set; }
            public ushort TargetObjectId { get; private set; }
            public long SourceLastMovementTick { get; private set; }
            public long TargetLastMovementTick { get; private set; }
            public LosCheckResponse Response { get; set; }
            public bool HasCachedResponse => _responseExpireTime > 0; // Don't rely on Response.

            private long _responseExpireTime;
            private Queue<CheckLosResponse> _callbacks = new();
            private readonly Action<LosCheckTimer> _completionCallback;

            public LosCheckTimer(GamePlayer owner, Action<LosCheckTimer> completionCallback) : base(owner)
            {
                _completionCallback = completionCallback ?? throw new ArgumentNullException(nameof(completionCallback));
            }

            public void Setup(ushort sourceObjectId, ushort targetObjectId, long sourceLastMovementTick, long targetLastMovementTick, CheckLosResponse initialCallback)
            {
                SourceObjectId = sourceObjectId;
                TargetObjectId = targetObjectId;
                SourceLastMovementTick = sourceLastMovementTick;
                TargetLastMovementTick = targetLastMovementTick;
                Response = LosCheckResponse.None;
                _responseExpireTime = 0;
                _callbacks.Enqueue(initialCallback);
                Start(ServerProperties.Properties.LOS_CHECK_TIMEOUT);
            }

            public void AddCallback(CheckLosResponse callback)
            {
                _callbacks.Enqueue(callback);
            }

            public void UpdateResponseExpireTime()
            {
                _responseExpireTime = GameLoop.GameLoopTime + CACHED_RESPONSE_LIFETIME;
            }

            public void Clear()
            {
                SourceObjectId = 0;
                TargetObjectId = 0;
                SourceLastMovementTick = 0;
                TargetLastMovementTick = 0;
                Response = LosCheckResponse.None;
                _responseExpireTime = 0;

                if (_callbacks.Count > 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Cleared a LoS check timer with uncalled callbacks ({this}) (CurrentTime = {GameLoop.GameLoopTime})");

                    _callbacks.Clear();
                }

                Stop();
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                // If the timer is in its "cached" state, wait for its lifetime to expire.
                if (HasCachedResponse)
                {
                    if (!GameServiceUtils.ShouldTick(_responseExpireTime))
                        return (int) (_responseExpireTime - GameLoop.GameLoopTime);

                    // Notify the handler that this timer is finished, so it can be pooled.
                    _completionCallback(this);
                    return 0;
                }

                // If a response wasn't set, this is a timeout.
                // We don't cache a timeout. Invoke the callbacks and remove the timer.
                if (Response is LosCheckResponse.None)
                {
                    Response = LosCheckResponse.Timeout;
                    InvokeCallbacks();
                    _completionCallback(this);

                    if (log.IsDebugEnabled)
                        log.Debug($"LoS check timeout ({this}) ({Owner})");

                    return 0;
                }

                // Invoke the callbacks and transition to the "cached" state.
                InvokeCallbacks();
                UpdateResponseExpireTime();
                return CACHED_RESPONSE_LIFETIME;
            }

            private void InvokeCallbacks()
            {
                while (_callbacks.TryDequeue(out CheckLosResponse callback))
                {
                    try
                    {
                        callback(Owner as GamePlayer, Response, SourceObjectId, TargetObjectId);
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"Error in callback for LoS check from {SourceObjectId} to {TargetObjectId} (Owner: {Owner})", e);
                    }
                }
            }

            public override string ToString()
            {
                return $"{nameof(SourceObjectId)} = {SourceObjectId}," +
                    $"{nameof(TargetObjectId)} = {TargetObjectId}, " +
                    $"{nameof(Response)} = {Response}, " +
                    $"{nameof(_responseExpireTime)} = {_responseExpireTime}, " +
                    $"{nameof(GameLoop.GameLoopTime)} = {GameLoop.GameLoopTime}, " +
                    $"{nameof(_callbacks)} = {_callbacks.Count}";
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
