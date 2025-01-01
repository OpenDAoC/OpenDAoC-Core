using System.Threading;
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

        private bool _openDead = false;
        private eDoorState _state;
        private readonly Lock _lock = new();
        private ECSGameTimer _closeDoorAction;
        private ECSGameTimer _repairTimer;

        public int Locked { get; set; }
        public override int DoorID { get; set; }
        public override uint Flag { get; set; }
        public override eDoorState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    lock (_lock)
                    {
                        _state = value;

                        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            player.Out.SendDoorState(CurrentRegion, this);
                    }
                }
            }
        }

        public GameDoor() : base()
        {
            _state = eDoorState.Closed;
            m_model = 0xFFFF;
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            DbDoor dbDoor = obj as DbDoor;

            if (dbDoor == null)
                return;

            Zone curZone = WorldMgr.GetZone((ushort) (dbDoor.InternalID / 1000000));

            if (curZone == null)
                return;

            CurrentRegion = curZone.ZoneRegion;
            m_name = dbDoor.Name;
            Heading = (ushort) dbDoor.Heading;
            m_x = dbDoor.X;
            m_y = dbDoor.Y;
            m_z = dbDoor.Z;
            m_level = 0;
            m_model = 0xFFFF;
            DoorID = dbDoor.InternalID;
            m_guildName = dbDoor.Guild;
            Realm = (eRealm) dbDoor.Realm;
            m_level = dbDoor.Level;
            m_health = dbDoor.Health;
            Locked = dbDoor.Locked;
            Flag = dbDoor.Flags;

            // Open mile gates on PVE and PVP server types.
            if (CurrentRegion.IsFrontier && (GameServer.Instance.Configuration.ServerType is EGameServerType.GST_PvE or EGameServerType.GST_PvP))
                State = eDoorState.Open;

            AddToWorld();
            StartHealthRegeneration();
        }

        public override void SaveIntoDatabase()
        {
            DbDoor obj = null;

            if (InternalID != null)
                obj = GameServer.Database.FindObjectByKey<DbDoor>(InternalID);

            if (obj == null)
                obj = new DbDoor();

            obj.Name = Name;
            obj.InternalID = DoorID;
            obj.Type = DoorID / 100000000;
            obj.Guild = GuildName;
            obj.Flags = Flag;
            obj.Realm = (byte) Realm;
            obj.Level = Level;
            obj.Health = Health;
            obj.Locked = Locked;

            if (InternalID == null)
            {
                GameServer.Database.AddObject(obj);
                InternalID = obj.ObjectId;
            }
            else
                GameServer.Database.SaveObject(obj);
        }

        public override void Open(GameLiving opener = null)
        {
            if (Locked == 0)
                State = eDoorState.Open;

            if (HealthPercent > 40 || !_openDead)
            {
                lock (_lock)
                {
                    if (_closeDoorAction == null)
                        _closeDoorAction = new CloseDoorAction(this);

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

        public override void NPCManipulateDoorRequest(GameNPC npc, bool open)
        {
            npc.TurnTo(X, Y);

            if (open && _state != eDoorState.Open)
                Open();
            else if (!open && _state != eDoorState.Closed)
                Close();
        }

        public override int Health
        {
            get => m_health;
            set
            {
                int maxhealth = MaxHealth;

                if (value >= maxhealth)
                {
                    m_health = maxhealth;

                    lock (XpGainersLock)
                    {
                        m_xpGainers.Clear();
                    }
                }
                else
                    m_health = value > 0 ? value : 0;

                if (IsAlive && m_health < maxhealth)
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
            if ((_repairTimer != null && _repairTimer.IsAlive) || Health >= MaxHealth)
                return;

            _repairTimer = new ECSGameTimer(this);
            _repairTimer.Callback = new ECSGameTimer.ECSTimerCallback(RepairTimerCallback);
            _repairTimer.Start(REPAIR_INTERVAL);
        }

        private int RepairTimerCallback(ECSGameTimer timer)
        {
            if (HealthPercent != 100 && !InCombat)
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

                if (HealthPercent >= 100)
                    return 0;
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

                        if (Locked == 0)
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
