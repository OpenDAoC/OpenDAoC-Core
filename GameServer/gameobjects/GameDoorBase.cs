using System;
using System.Threading;
using DOL.Database;

namespace DOL.GS
{
    public enum eDoorState
    {
        Open,
        Closed
    }

    public class GameDoorBase : GameLiving
    {
        public override eGameObjectType GameObjectType => eGameObjectType.DOOR;

        // Used when `DbDoor` is null, for temporary objects.
        private int _doorId;
        private eDoorState _state = eDoorState.Closed;
        protected readonly Lock _stateLock = new();

        public DbDoor DbDoor { get; set; }
        public virtual bool CanBeOpenedViaInteraction => false;

        public virtual int DoorId
        {
            get => DbDoor == null ? _doorId : DbDoor.InternalID;
            set
            {
                if (DbDoor == null)
                {
                    _doorId = value;
                    return;
                }

                DbDoor.InternalID = value;
            }
        }

        public virtual eDoorState State
        {
            get => DbDoor == null ? _state :(eDoorState) DbDoor.State;
            set
            {
                if (DbDoor != null)
                {
                    if ((eDoorState) DbDoor.State == value)
                        return;
                }
                else
                {
                    if (_state == value)
                        return;
                }

                lock (_stateLock)
                {
                    if (DbDoor != null)
                    {
                        if ((eDoorState) DbDoor.State == value)
                            return;

                        DbDoor.State = (int) value;
                    }
                    else
                    {
                        if (_state == value)
                            return;

                        _state = value;
                    }

                    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        player.Out.SendDoorState(CurrentRegion, this);
                }
            }
        }

        public virtual bool Locked
        {
            get => DbDoor != null && Convert.ToBoolean(DbDoor.Locked);
            set
            {
                if (DbDoor == null)
                    return;

                DbDoor.Locked = Convert.ToInt32(value);
            }
        }

        public virtual bool IsPostern
        {
            get => DbDoor != null && DbDoor.IsPostern;
            set
            {
                if (DbDoor == null)
                    return;

                DbDoor.IsPostern = value;
            }
        }

        public virtual uint Flag
        {
            get => DbDoor == null ? 0 : DbDoor.Flags;
            set
            {
                if (DbDoor == null)
                    return;

                DbDoor.Flags = value;
            }
        }

        public virtual void Close(GameLiving closer = null) { }
        public virtual void Open(GameLiving opener = null) { }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            if (obj is not DbDoor dbDoor)
                return;

            DbDoor = dbDoor;
            Zone curZone = WorldMgr.GetZone((ushort) (dbDoor.InternalID / 1000000));

            if (curZone == null)
                return;

            CurrentRegion = curZone.ZoneRegion;
            m_name = dbDoor.Name;
            m_z = dbDoor.Z;
            m_y = dbDoor.Y;
            m_x = dbDoor.X;
            Heading = (ushort) dbDoor.Heading;
            m_guildName = dbDoor.Guild;
            m_level = dbDoor.Level;
            Realm = (eRealm) dbDoor.Realm;
            m_health = dbDoor.Health;
            m_model = 0xFFFF;

            AddToWorld();
            StartHealthRegeneration();
        }

        public override void SaveIntoDatabase()
        {
            DbDoor obj = DbDoor;
            obj ??= new DbDoor();
            obj.Name = Name;
            obj.Type = DbDoor.InternalID / 100000000;
            obj.Z = Z;
            obj.Y = Y;
            obj.X = X;
            obj.Heading = Heading;
            obj.InternalID = DbDoor.InternalID;
            obj.Guild = GuildName;
            obj.Level = Level;
            obj.Realm = (byte) Realm;
            obj.Flags = Flag;
            obj.Locked = Convert.ToInt32(Locked);
            obj.Health = Health;
            obj.IsPostern = IsPostern;
            obj.State = (int) State;

            if (InternalID == null)
            {
                GameServer.Database.AddObject(obj);
                InternalID = obj.ObjectId;
            }
            else
                GameServer.Database.SaveObject(obj);
        }
    }
}
