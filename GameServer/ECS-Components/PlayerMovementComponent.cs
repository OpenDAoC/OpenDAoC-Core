using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;
using DOL.Language;
using log4net;

namespace DOL.GS
{
    public class PlayerMovementComponent : MovementComponent
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const int BROADCAST_MINIMUM_INTERVAL = 200; // Clients send a position or heading update packet every 200ms at most (when moving or rotating).
        private const int SOFT_LINK_DEATH_THRESHOLD = 5000; // How long does it take without receiving a packet for a client to enter the soft link death state.

        private long _lastPositionUpdatePacketReceivedTime;
        private long _nextPositionBroadcast;
        private bool _needBroadcastPosition;

        private long _lastHeadingUpdatePacketReceivedTime;
        private long _nextHeadingBroadcast;
        private bool _needBroadcastHeading;

        private bool _isEncumberedMessageSent;

        public new GamePlayer Owner { get; }
        public int MaxSpeedPercent => MaxSpeed * 100 / GamePlayer.PLAYER_BASE_SPEED;
        public ref long LastPositionUpdatePacketReceivedTime => ref _lastPositionUpdatePacketReceivedTime;
        public ref long LastHeadingUpdatePacketReceivedTime => ref _lastHeadingUpdatePacketReceivedTime;

        public PlayerMovementComponent(GameLiving owner) : base(owner)
        {
            Owner = owner as GamePlayer;
        }

        public override void Tick()
        {
            if (!Owner.IsLinkDeathTimerRunning)
            {
                if (ServiceUtils.ShouldTickNoEarly(_lastPositionUpdatePacketReceivedTime + SOFT_LINK_DEATH_THRESHOLD))
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Position update timeout on client. Calling link death. ({Owner.Client})");

                    // The link death timer will handle the position broadcast.
                    Owner.Client.OnLinkDeath(true);
                    return;
                }

                // Position and heading broadcasts are mutually exclusive.
                if (_needBroadcastPosition && ServiceUtils.ShouldTickAdjust(ref _nextPositionBroadcast))
                {
                    BroadcastPosition();
                    _nextPositionBroadcast += BROADCAST_MINIMUM_INTERVAL;
                    _needBroadcastPosition = false;
                }
                else if (_needBroadcastHeading && ServiceUtils.ShouldTickAdjust(ref _nextHeadingBroadcast))
                {
                    BroadcastHeading();
                    _nextHeadingBroadcast += BROADCAST_MINIMUM_INTERVAL;
                    _needBroadcastHeading = false;
                }
            }

            base.Tick();
        }

        public void BroadcastPosition()
        {
            PlayerPositionUpdateHandler.BroadcastPosition(Owner.Client);
        }

        public void BroadcastHeading()
        {
            PlayerHeadingUpdateHandler.BroadcastHeading(Owner.Client);
        }

        public void OnPositionPacketReceivedEnd()
        {
            _needBroadcastPosition = true;
            _lastPositionUpdatePacketReceivedTime = GameLoop.GameLoopTime;

            if (Owner.IsEncumbered)
            {
                if (Owner.IsMoving)
                {
                    if (!_isEncumberedMessageSent)
                    {
                        SendEncumberedMessage();
                        _isEncumberedMessageSent = true;
                    }
                }
                else
                {
                    _isEncumberedMessageSent = false;

                    if (MaxSpeedPercent <= 0)
                        SendEncumberedMessage(); // Allow it to be spammed.
                }
            }

            void SendEncumberedMessage()
            {
                Owner.Out.SendMessage(LanguageMgr.GetTranslation(Owner.Client.Account.Language, "PlayerMovementComponent.Encumbered"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public void OnHeadingPacketReceived()
        {
            _needBroadcastHeading = true;
            _lastHeadingUpdatePacketReceivedTime = GameLoop.GameLoopTime;
        }
    }
}
