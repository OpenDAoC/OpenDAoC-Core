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

        public override bool CanBeOpenedViaInteraction => !Locked;

        private bool _openDead = false;
        private ECSGameTimer _closeDoorAction;

        public GameDoor() : base()
        {
            m_model = 0xFFFF;
        }

        public override void Open(GameLiving opener = null)
        {
            if (!Locked)
                State = eDoorState.Open;

            if (HealthPercent > 40 || !_openDead)
            {
                lock (_stateLock)
                {
                    _closeDoorAction ??= new CloseDoorAction(this);
                    _closeDoorAction.Start(STAYS_OPEN_DURATION);
                }
            }
        }

        public override void Close(GameLiving closer = null)
        {
            if (!_openDead)
                State = eDoorState.Closed;

            _closeDoorAction?.Stop();
            _closeDoorAction = null;
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
            }
        }

        public override int MaxHealth => 5 * GetModified(eProperty.MaxHealth);

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
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }

            GamePlayer attackerPlayer = source as GamePlayer;

            if (attackerPlayer != null)
            {
                if (!_openDead && Realm != eRealm.Door)
                {
                    attackerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    Health -= damageAmount + criticalAmount;

                    if (!IsAlive)
                    {
                        attackerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        Die(source);
                        _openDead = true;

                        if (!Locked)
                            Open();

                        Group attackerGroup = attackerPlayer.Group;

                        if (attackerGroup != null)
                        {
                            foreach (GameLiving living in attackerGroup.GetMembersInTheGroup())
                                ((GamePlayer) living)?.Out.SendMessage(LanguageMgr.GetTranslation(attackerPlayer.Client.Account.Language, "GameDoor.NowOpen", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        }
                    }
                }
            }
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            if (State is eDoorState.Open)
            {
                lock (_stateLock)
                {
                    _closeDoorAction ??= new CloseDoorAction(this);
                    _closeDoorAction.Start(STAYS_OPEN_DURATION);
                }
            }
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
