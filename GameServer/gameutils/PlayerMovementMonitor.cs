using System;
using System.Reflection;

namespace DOL.GS
{
    public class PlayerMovementMonitor
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int SPEEDHACK_ACTION_THRESHOLD = 3;     // Number of consecutive speed hack detections before action is taken.
        private const int TELEPORT_THRESHOLD = 3;             // Number of consecutive teleports before kicking.
        private const int BASE_SPEED_TOLERANCE = 30;          // Base tolerance in units/second for measurement errors.
        private const int LATENCY_BUFFER = 650;               // Buffer time to account for latency when checking speed changes.
        private const int RESET_COUNTER_DELAY = 1250;         // Delay in milliseconds since the last speed hack detection to reset counters.

        private readonly GamePlayer _player;                  // Player being monitored for movement and speed hacks.
        private PositionSample _previous;                     // Previous position sample recorded.
        private PositionSample _current;                      // Current position sample being recorded.
        private PositionSample _teleport;                     // Position sample to use to teleport the player back to if a speed hack is detected.
        private int _speedHackCounter;                        // Counter for consecutive speed hack detections.
        private int _teleportCounter;                         // Counter for consecutive teleports due to speed hacks.
        private long _resetCountersTime;                      // Time when the counters were last reset, used to determine if they should be reset again.
        private long _pausedUntil;                            // Time until which recording is paused after a teleport.
        private long _lastSpeedDecreaseTime;                  // Time when the last speed decrease was recorded, used to handle latency issues.
        private short _previousMaxSpeed;                      // Previous maximum speed of the player, used to determine if speed has decreased.

        // Cached values to avoid recalculating player max speed too often.
        // This is a workaround the fact that `GamePlayer.MaxSpeed` currently recalculates on every access.
        // This assumes that the player's max speed does not change between calls to `RecordPosition` and `ValidateMovement`.
        private short _cachedMaxSpeed;
        private long _cachedMaxSpeedTick;

        public PlayerMovementMonitor(GamePlayer player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public void RecordPosition()
        {
            _player.LastPlayerActivityTime = GameLoop.GameLoopTime;

            if (_pausedUntil > GameLoop.GameLoopTime)
                return;

            if (_teleport.Timestamp > 0 && _resetCountersTime <= GameLoop.GameLoopTime)
            {
                _speedHackCounter = 0;
                _teleportCounter = 0;
                _teleport = default;
            }

            short currentMaxSpeed = GetCachedPlayerMaxSpeed();

            // Detect speed decrease.
            if (_current.MaxSpeed > 0 && currentMaxSpeed < _current.MaxSpeed)
            {
                _lastSpeedDecreaseTime = GameLoop.GameLoopTime;
                _previousMaxSpeed = _current.MaxSpeed;
            }

            PositionSample sample = new(_player.X, _player.Y, _player.Z, GameLoop.GameLoopTime, currentMaxSpeed);

            // Check if this is a duplicate timestamp (same tick).
            // If so, we replace the current sample with the new one.
            if (_current.Timestamp == GameLoop.GameLoopTime)
                _current = sample;
            else
            {
                _previous = _current;
                _current = sample;
            }
        }

        public void ValidateMovement()
        {
            bool speedViolationDetected = false;
            double violation = 0;
            long timeDiff = _current.Timestamp - _previous.Timestamp;

            // Skip if timestamps are invalid (should not happen).
            if (timeDiff <= 0)
                return;

            // Handle latency by allowing higher speed for recent samples after speed decreases.
            double speed = CalculateSpeed(_previous, _current, timeDiff);
            double allowedMaxSpeed = CalculateAllowedMaxSpeed(_current);
            double allowedSpeed = allowedMaxSpeed + BASE_SPEED_TOLERANCE;

            if (speed > allowedSpeed)
            {
                violation = speed - allowedSpeed;
                speedViolationDetected = true;
            }

            if (speedViolationDetected)
            {
                _resetCountersTime = GameLoop.GameLoopTime + RESET_COUNTER_DELAY;

                // Set the teleport position on the first speed violation.
                if (_teleport.Timestamp == 0)
                {
                    if (_previous.Timestamp > 0)
                        _teleport = _previous;
                    else if (_current.Timestamp > 0)
                        _teleport = _current;
                    else
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"Speed hack detected but no previous position available for player {_player.Name}. Cannot teleport back.");

                        return;
                    }
                }

                if (++_speedHackCounter >= SPEEDHACK_ACTION_THRESHOLD)
                {
                    if (!_player.IsAllowedToFly && (ePrivLevel) _player.Client.Account.PrivLevel <= ePrivLevel.Player)
                    {
                        short maxSpeed = GetCachedPlayerMaxSpeed();
                        HandleSpeedHack(maxSpeed + violation, maxSpeed);
                    }

                    _speedHackCounter = 0;
                }
            }
        }

        public void OnTeleportOrRegionChange()
        {
            _previous = default;
            _current = default;
            _pausedUntil = GameLoop.GameLoopTime + LATENCY_BUFFER;
        }

        private static double CalculateSpeed(PositionSample previous, PositionSample current, long timeDiff)
        {
            // Ignore vertical movement for speed calculation.
            int dx = current.X - previous.X;
            int dy = current.Y - previous.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            return distance * 1000.0 / timeDiff;
        }

        private double CalculateAllowedMaxSpeed(PositionSample current)
        {
            double newerSpeed = current.MaxSpeed;

            // If within latency buffer after a speed decrease, allow previous max speed.
            if (_lastSpeedDecreaseTime > 0 && (GameLoop.GameLoopTime - _lastSpeedDecreaseTime) <= LATENCY_BUFFER)
                return Math.Max(_previousMaxSpeed, newerSpeed);

            return newerSpeed;
        }

        private void HandleSpeedHack(double detectedSpeed, short allowedSpeed)
        {
            // Make sure we have a valid previous position, otherwise this is a false positive.
            if (_previous.Timestamp == 0)
                return;

            string msg;

            // If we've teleported too many times consecutively, kick the player.
            if (_teleportCounter >= TELEPORT_THRESHOLD)
            {
                msg = $"Speed hack (kick): CharName={_player.Name} Account={_player.Client?.Account?.Name} IP={_player.Client?.TcpEndpointAddress} Speed={detectedSpeed:F2} Allowed={allowedSpeed} TeleportCount={_teleportCounter}";
                GameServer.Instance.LogCheatAction(msg);

                _player.Out.SendPlayerQuit(true);
                _player.SaveIntoDatabase();
                _player.Quit(true);
                _player.Client.Disconnect();
            }
            else
            {
                msg = $"Speed hack (teleport): CharName={_player.Name} Account={_player.Client?.Account?.Name} IP={_player.Client?.TcpEndpointAddress} Speed={detectedSpeed:F2} Allowed={allowedSpeed} TeleportCount={_teleportCounter}";
                GameServer.Instance.LogCheatAction(msg);

                _previous = _current;
                _current = _teleport;
                _player.MoveTo(_player.CurrentRegionID, _teleport.X, _teleport.Y, _teleport.Z, _player.Heading); // Will call `OnTeleport`.
                _teleportCounter++;
            }

            if (log.IsInfoEnabled)
                log.Info($"Speed hack detected: CharName={_player.Name} Account={_player.Client?.Account?.Name} IP={_player.Client?.TcpEndpointAddress} Speed={detectedSpeed:F2} Allowed={allowedSpeed:F2} TeleportCount={_teleportCounter}");
        }

        private short GetCachedPlayerMaxSpeed()
        {
            long now = GameLoop.GameLoopTime;

            if (_cachedMaxSpeedTick != now)
            {
                _cachedMaxSpeed = _player.Steed?.MaxSpeed ?? _player.MaxSpeed;
                _cachedMaxSpeedTick = now;
            }

            return _cachedMaxSpeed;
        }

        private readonly struct PositionSample
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public readonly long Timestamp;
            public readonly short MaxSpeed;

            public PositionSample(int x, int y, int z, long timestamp, short maxSpeed)
            {
                X = x;
                Y = y;
                Z = z;
                Timestamp = timestamp;
                MaxSpeed = maxSpeed;
            }
        }
    }
}
