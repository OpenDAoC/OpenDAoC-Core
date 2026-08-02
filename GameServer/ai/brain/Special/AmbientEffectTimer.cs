using System;
using DOL.GS;

namespace DOL.AI.Brain
{
    /// <summary>
    /// Replays a spell effect animation on its owner at a randomized interval.
    /// </summary>
    public class AmbientEffectTimer : ECSGameTimerWrapperBase
    {
        private readonly GameNPC _owner;
        private readonly ushort _effectId;
        private readonly int _minMs;
        private readonly int _maxMs;
        private readonly Func<bool> _shouldPlay;

        public AmbientEffectTimer(GameNPC owner, ushort effectId, int minIntervalMs = 6000, int maxIntervalMs = 20000, Func<bool> shouldPlay = null) : base(owner)
        {
            _owner = owner;
            _effectId = effectId;
            _minMs = minIntervalMs;
            _maxMs = maxIntervalMs;
            _shouldPlay = shouldPlay;
            Interval = Util.Random(_minMs, _maxMs);
        }

        protected override int OnTick(ECSGameTimer timer)
        {
            if (_owner.IsAlive && (_owner is not HideableNpc hideable || !hideable.IsHidden) && (_shouldPlay == null || _shouldPlay()))
            {
                foreach (GamePlayer player in _owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellEffectAnimation(_owner, _owner, _effectId, 0, false, 1);
            }

            return Util.Random(_minMs, _maxMs);
        }
    }

    /// <summary>
    /// Brain that plays an ambient effect on its body while it is running.
    /// </summary>
    public abstract class AmbientEffectBrain : StandardMobBrain
    {
        private AmbientEffectTimer _timer;

        protected abstract ushort AmbientEffectId { get; }
        protected virtual int AmbientMinIntervalMs => 6000;
        protected virtual int AmbientMaxIntervalMs => 20000;
        protected virtual bool ShouldPlayAmbientEffect => true;

        public override bool Start()
        {
            if (!base.Start())
                return false;

            _timer ??= new AmbientEffectTimer(Body, AmbientEffectId, AmbientMinIntervalMs, AmbientMaxIntervalMs, () => ShouldPlayAmbientEffect);
            _timer.Start();
            return true;
        }

        public override bool Stop()
        {
            if (!base.Stop())
                return false;

            _timer?.Stop();
            _timer = null;
            return true;
        }
    }
}
