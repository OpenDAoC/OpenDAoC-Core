using System;
using System.Reflection;

namespace DOL.GS 
{
    public class PlayerMovementMonitor
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int SPEEDHACK_ACTION_THRESHOLD = 3;     // Number of consecutive speed hack detections before action is taken.
        private const int TELEPORT_THRESHOLD = 3;             // Number of consecutive teleports before kicking.
        private const int BASE_SPEED_TOLERANCE = 25;          // Base tolerance in units/second for measurement errors.
        private const int LATENCY_BUFFER = 650;               // Buffer time to account for latency when checking speed changes.
        private const int RESET_COUNTER_DELAY = 1250;         // Delay in milliseconds since the last speed hack detection to reset counters.

        private readonly GamePlayer _player;                  // Player being monitored for movement and speed hacks.
        private PositionSample _previous;                     // Previous position sample recorded.
        private PositionSample _current;                      // Current position sample being recorded.
        private PositionSample _teleport;                     // Position sample to use to teleport the player back to if a speed hack is detected.
        private int _speedHackCount;                          // Counter for consecutive speed hack detections.
        private int _teleportCount;                           // Counter for consecutive teleports due to speed hacks.
        private long _resetCountersTime;                      // Time when the counters were last reset, used to determine if they should be reset again.
        private long _pausedUntil;                            // Time until which recording is paused after a teleport.
        private long _lastSpeedDecreaseTime;                  // Time when the last speed decrease was recorded, used to handle latency issues.
        private short _previousMaxSpeed;                      // Previous maximum speed of the player, used to determine if speed has decreased.
        private long _previousTimestamp;                      // Timestamp of the last movement validation, used to adjust time differences between position samples.

        // Cached values to avoid recalculating player max speed too often.
        // This is a workaround for the fact that `GamePlayer.MaxSpeed` currently recalculates on every access.
        // This assumes that the player's max speed does not change between calls to `RecordPosition` and `ValidateMovement`.
        private short _cachedMaxSpeed;
        private long _cachedMaxSpeedTick;

        public PlayerMovementMonitor(GamePlayer player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public void RecordPosition()
        {
            long timestamp = GameLoop.GetRealTime();

            if (_pausedUntil > timestamp)
                return;

            if (_teleport.Timestamp > 0 && _resetCountersTime <= timestamp)
            {
                _speedHackCount = 0;
                _teleportCount = 0;
                _teleport = default;
            }

            short currentMaxSpeed = GetCachedPlayerMaxSpeed();

            // Detect speed decrease.
            if (_current.MaxSpeed > 0 && currentMaxSpeed < _current.MaxSpeed)
            {
                _lastSpeedDecreaseTime = timestamp;
                _previousMaxSpeed = _current.MaxSpeed;
            }

            PositionSample sample = new(_player.X, _player.Y, _player.Z, GameLoop.GameLoopTime, timestamp, currentMaxSpeed);

            // Check if more than one position sample is being recorded in the same game loop tick.
            // If so, we replace the current sample with the new one.
            if (_current.GameLoopTime == GameLoop.GameLoopTime)
                _current = sample;
            else
            {
                _previous = _current;
                _current = sample;
            }
        }

        public void ValidateMovement()
        {
            long timeDiff = _current.Timestamp - _previous.Timestamp;

            // Skip if timestamps are invalid (should not happen).
            if (timeDiff <= 0)
                return;

            long timestamp = GameLoop.GetRealTime();

            // Account for processing delay uncertainty.
            // We don't know when the position update was actually received, only when it was processed by the game loop.
            // However, we know how much time has passed since the last game loop tick, and we can be safe by assuming the previous packet was processed late.
            // In practice, this means adding the difference between the current time and the previous time to the time difference.
            timeDiff += timestamp - _previousTimestamp;
            _previousTimestamp = timestamp;
            bool distancedViolationDetected = false;
            long dx = _current.X - _previous.X;
            long dy = _current.Y - _previous.Y;
            long squaredDistance = dx * dx + dy * dy;
            double allowedMaxSpeed = CalculateAllowedMaxSpeed(_current, timestamp) + BASE_SPEED_TOLERANCE;
            double allowedMaxDistance = allowedMaxSpeed * timeDiff / 1000.0;
            double allowedMaxDistanceSquared = allowedMaxDistance * allowedMaxDistance;

            if (squaredDistance > allowedMaxDistanceSquared)
                distancedViolationDetected = true;

            if (distancedViolationDetected)
            {
                _resetCountersTime = timestamp + RESET_COUNTER_DELAY;

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

                if (++_speedHackCount >= SPEEDHACK_ACTION_THRESHOLD)
                {
                    if (!_player.IsAllowedToFly && (ePrivLevel) _player.Client.Account.PrivLevel <= ePrivLevel.Player)
                    {
                        double actualDistance = Math.Sqrt(squaredDistance);
                        double actualSpeed = actualDistance * 1000.0 / timeDiff;
                        HandleSpeedHack(actualDistance, allowedMaxDistance, actualSpeed, allowedMaxSpeed);
                    }

                    _speedHackCount = 0;
                }
            }
        }

        public void OnTeleportOrRegionChange()
        {
            _previous = default;
            _current = default;
            _pausedUntil = GameLoop.GetRealTime() + LATENCY_BUFFER;
        }

        private double CalculateAllowedMaxSpeed(PositionSample current, long timestamp)
        {
            double newerSpeed = current.MaxSpeed;

            // If within latency buffer after a speed decrease, allow previous max speed.
            if (_lastSpeedDecreaseTime > 0 && (timestamp - _lastSpeedDecreaseTime) <= LATENCY_BUFFER)
                return Math.Max(_previousMaxSpeed, newerSpeed);

            return newerSpeed;
        }

        private void HandleSpeedHack(double actualDistance, double allowedMaxDistance, double actualSpeed, double allowedMaxSpeed)
        {
            // Make sure we have a valid previous position, otherwise this is a false positive.
            if (_previous.Timestamp == 0)
                return;

            string action = _teleportCount >= TELEPORT_THRESHOLD ? "kick" : "teleport";
            string msg = BuildSpeedHackMessage(action, actualDistance, allowedMaxDistance, actualSpeed, allowedMaxSpeed, _teleportCount);
            GameServer.Instance.LogCheatAction(msg);

            // If we've teleported too many times consecutively, kick the player.
            if (_teleportCount >= TELEPORT_THRESHOLD)
            {
                _player.Out.SendPlayerQuit(true);
                _player.SaveIntoDatabase();
                _player.Quit(true);
                _player.Client.Disconnect();
            }
            else
            {
                _previous = _current;
                _current = _teleport;
                _player.MoveTo(_player.CurrentRegionID, _teleport.X, _teleport.Y, _teleport.Z, _player.Heading); // Will call `OnTeleport`.
                _teleportCount++;
            }

            if (log.IsInfoEnabled)
                log.Info(BuildSpeedHackMessage("detected", actualDistance, allowedMaxDistance, actualSpeed, allowedMaxSpeed, _teleportCount));
        }

        private string BuildSpeedHackMessage(string action, double actualDistance, double allowedMaxDistance, double actualSpeed, double allowedMaxSpeed, int teleportCount)
        {
            return $"Speed hack ({action}): " +
                   $"CharName={_player.Name} " +
                   $"Account={_player.Client?.Account?.Name} " +
                   $"IP={_player.Client?.TcpEndpointAddress} " +
                   $"Distance={actualDistance:0.##} " +
                   $"AllowedDistance={allowedMaxDistance:0.##} " +
                   $"Speed={actualSpeed:0.##} " +
                   $"AllowedSpeed={allowedMaxSpeed:0.##} " +
                   $"TeleportCount={teleportCount}";
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
            public readonly long GameLoopTime;
            public readonly long Timestamp;
            public readonly short MaxSpeed;

            public PositionSample(int x, int y, int z, long gameLoopTime, long timestamp, short maxSpeed)
            {
                X = x;
                Y = y;
                Z = z;
                GameLoopTime = gameLoopTime;
                Timestamp = timestamp;
                MaxSpeed = maxSpeed;
            }
        }
    }
}
