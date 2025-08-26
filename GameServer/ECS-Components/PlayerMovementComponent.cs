using System.Numerics;
using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;
using DOL.Language;

namespace DOL.GS
{
    public class PlayerMovementComponent : MovementComponent
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int BROADCAST_MINIMUM_INTERVAL = 200; // Clients send a position or heading update packet every 200ms at most (when moving or rotating).
        private const int SOFT_LINK_DEATH_THRESHOLD = 5000; // How long does it take without receiving a packet for a client to enter the soft link death state.

        private long _nextPositionBroadcast;
        private bool _needBroadcastPosition;

        private long _nextHeadingBroadcast;
        private bool _needBroadcastHeading;

        private PlayerMovementMonitor _playerMovementMonitor;
        private bool _validateMovementOnNextTick;

        private bool _isEncumberedMessageSent;

        public new GamePlayer Owner { get; }
        public int MaxSpeedPercent => MaxSpeed * 100 / GamePlayer.PLAYER_BASE_SPEED;
        public long LastPositionUpdatePacketReceivedTime { get; set; }

        public PlayerMovementComponent(GameLiving owner) : base(owner)
        {
            Owner = owner as GamePlayer;
            _playerMovementMonitor = new(Owner);
        }

        public override void Tick()
        {
            if (Owner.Client.ClientState is not GameClient.eClientState.Playing)
            {
                RemoveFromServiceObjectStore();
                return;
            }

            base.Tick();
        }

        protected override void TickInternal()
        {
            if (!Owner.IsLinkDeathTimerRunning)
            {
                if (GameServiceUtils.ShouldTick(LastPositionUpdatePacketReceivedTime + SOFT_LINK_DEATH_THRESHOLD))
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Position update timeout on client. Calling link death. ({Owner.Client})");

                    // The link death timer will handle the position broadcast.
                    Owner.Client.OnLinkDeath(true);
                    return;
                }

                // Always validate movement, even if the next position broadcast is not due yet.
                if (_validateMovementOnNextTick)
                {
                    _playerMovementMonitor.ValidateMovement();
                    _validateMovementOnNextTick = false;
                }

                // Position and heading broadcasts are mutually exclusive.
                if (_needBroadcastPosition && GameServiceUtils.ShouldTick(_nextPositionBroadcast))
                {
                    BroadcastPosition();
                    _nextPositionBroadcast = GameLoop.GameLoopTime + BROADCAST_MINIMUM_INTERVAL;
                    _needBroadcastPosition = false;
                }
                else if (_needBroadcastHeading && GameServiceUtils.ShouldTick(_nextHeadingBroadcast))
                {
                    BroadcastHeading();
                    _nextHeadingBroadcast = GameLoop.GameLoopTime + BROADCAST_MINIMUM_INTERVAL;
                    _needBroadcastHeading = false;
                }
            }

            base.TickInternal();
        }

        public void BroadcastPosition()
        {
            PlayerPositionUpdateHandler.BroadcastPosition(Owner.Client);
        }

        public void BroadcastHeading()
        {
            PlayerHeadingUpdateHandler.BroadcastHeading(Owner.Client);
        }

        public override void OnPositionUpdate()
        {
            base.OnPositionUpdate();

            Vector3 oldPosition = _ownerPosition;
            UpdatePosition();

            if (!oldPosition.EqualsXY(_ownerPosition))
            {
                Owner.OnPlayerMove();
                _playerMovementMonitor.RecordPosition();
                _validateMovementOnNextTick = true;
                Owner.LastPlayerActivityTime = GameLoop.GameLoopTime;
            }

            _needBroadcastPosition = true;

            if (IsMoving)
                Owner.LastPlayerActivityTime = GameLoop.GameLoopTime;

            if (Owner.IsEncumbered)
            {
                if (Owner.IsMoving)
                {
                    if (!_isEncumberedMessageSent)
                    {
                        SendEncumberedMessage(Owner);
                        _isEncumberedMessageSent = true;
                    }
                }
                else
                {
                    _isEncumberedMessageSent = false;

                    if (MaxSpeedPercent <= 0)
                        SendEncumberedMessage(Owner); // Allow it to be spammed.
                }
            }

            AddToServiceObjectStore();

            static void SendEncumberedMessage(GamePlayer player)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerMovementComponent.Encumbered"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public void OnHeadingUpdate()
        {
            _needBroadcastHeading = true;
        }

        public void OnTeleportOrRegionChange()
        {
            _playerMovementMonitor.OnTeleportOrRegionChange();
        }

        protected override void UpdatePosition()
        {
            _ownerPosition = new(Owner.X, Owner.Y, Owner.Z);
        }
    }
}
