using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    /// <summary>
    /// GameDoor is class for regular doors.
    /// </summary>
    public class GameDoor : GameDoorBase
    {
        private const int STAYS_OPEN_DURATION = 5000;
        private const int REPAIR_INTERVAL = 30 * 1000;

        private long _nextAllowedStateChangeTime;

        private static HashSet<int> _borderKeepDoorIds =
        [
            11020501, 11020502,
            12000101, 12000102,
            102093501, 102093502,
            111161301, 111161302,
            206016801, 206016802,
            207156901, 207156902
        ];

        private bool _openDead = false;
        private CloseDoorAction _closeDoorAction;

        public override bool CanBeOpenedViaInteraction => !Locked;

        public GameDoor() : base()
        {
            m_model = 0xFFFF;
        }

        public override void Open(GameLiving opener = null)
        {
            lock (_stateLock)
            {
                // Ignore interaction packets that could cause the door to change state again during its current animation.
                // Normally the client doesn't send a packet, but latency makes it possible.
                // This is primarily important for border keep doors.
                if (!_openDead && !GameServiceUtils.ShouldTick(_nextAllowedStateChangeTime))
                    return;

                if (!Locked)
                    State = eDoorState.Open;

                if (!_openDead || HealthPercent > 40)
                {
                    _closeDoorAction ??= new(this);

                    // Don't extend the timer for border keep doors, otherwise players can block them.
                    // The client doesn't send any interaction packet during the animation,
                    // ensuring the door's state alternates between closed and open.
                    if (IsBorderKeepDoor() && _closeDoorAction.IsAlive)
                        return;

                    _closeDoorAction.Start(STAYS_OPEN_DURATION);
                }
            }
        }

        public override void Close(GameLiving closer = null)
        {
            lock (_stateLock)
            {
                if (!_openDead)
                    SetState(eDoorState.Closed);

                _closeDoorAction?.Stop();
                _closeDoorAction = null;
            }
        }

        private void SetState(eDoorState newState)
        {
            if (State == newState)
                return;

            State = newState;
            int stateChangeCooldown = IsBorderKeepDoor() ? 3000 : 1000;
            _nextAllowedStateChangeTime = GameLoop.GameLoopTime + stateChangeCooldown;
        }

        public override int Health
        {
            get => m_health;
            set
            {
                int maxHealth = MaxHealth;

                if (value >= maxHealth)
                {
                    m_health = maxHealth;

                    lock (XpGainersLock)
                    {
                        m_xpGainers.Clear();
                    }
                }
                else
                    m_health = value > 0 ? value : 0;

                if (IsAlive && m_health < maxHealth)
                    StartHealthRegeneration();

                UpdateCachedHealthPercent(maxHealth);
            }
        }

        public override void Die(GameObject killer)
        {
            base.Die(killer);
            StartHealthRegeneration();
        }

        public override void StartHealthRegeneration()
        {
            if (m_healthRegenerationTimer.IsAlive || Health >= MaxHealth)
                return;

            m_healthRegenerationTimer.Start(REPAIR_INTERVAL);
        }

        protected override int HealthRegenerationTimerCallback(ECSGameTimer timer)
        {
            if (HealthPercent >= 100)
                return 0;

            if (!InCombat)
            {
                Health += MaxHealth / 100 * 5;

                if (HealthPercent >= 40 && _openDead)
                {
                    _openDead = false;
                    Close();
                }

                // This should normally be done by 'DoorMgr'.
                // But for now it's here because basic doors aren't attackable anyway.
                SaveIntoDatabase();
            }

            return REPAIR_INTERVAL;
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (!_openDead && Realm != eRealm.Door)
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);

            if (source is not GamePlayer attackerPlayer || _openDead || Realm is eRealm.Door)
                return;

            attackerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            Health -= damageAmount + criticalAmount;

            if (IsAlive)
                return;

            attackerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            Die(source);
            _openDead = true;

            if (!Locked)
                Open();

            Group attackerGroup = attackerPlayer.Group;

            if (attackerGroup != null)
            {
                foreach (GameLiving living in attackerGroup.GetMembersInTheGroup())
                    (living as GamePlayer)?.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            if (State is eDoorState.Open)
            {
                lock (_stateLock)
                {
                    _closeDoorAction ??= new(this);
                    _closeDoorAction.Start(STAYS_OPEN_DURATION);
                }
            }
        }

        public bool IsBorderKeepDoor()
        {
            return IsBorderKeepDoor(DoorId);
        }

        public static bool IsBorderKeepDoor(int doorId)
        {
            return _borderKeepDoorIds.Contains(doorId);
        }

        private class CloseDoorAction : ECSGameTimerWrapperBase
        {
            public CloseDoorAction(GameDoor door) : base(door) { }

            protected override int OnTick(ECSGameTimer timer)
            {
                GameDoor door = (GameDoor) timer.Owner;
                door.Close();
                return 0;
            }
        }
    }
}
