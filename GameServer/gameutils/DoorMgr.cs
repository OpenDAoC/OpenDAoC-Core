using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.Keeps;

namespace DOL.GS
{
    /// <summary>
    /// DoorMgr is manager of all door regular door and keep door
    /// </summary>
    public sealed class DoorMgr
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Lock Lock = new();

        private static Dictionary<int, List<GameDoorBase>> m_doors = new Dictionary<int, List<GameDoorBase>>();

        public const string WANT_TO_ADD_DOORS = "WantToAddDoors";

        /// <summary>
        /// this function load all door from DB
        /// </summary>
        public static bool Init()
        {
            var dbdoors = GameServer.Database.SelectAllObjects<DbDoor>();
            foreach (DbDoor door in dbdoors)
            {
                if (!LoadDoor(door))
                {
                    log.Error("Unable to load door id " + door.ObjectId + ", correct your database");
                    // continue loading, no need to stop server for one bad door!
                }
            }

            return true;
        }

        public static int SaveKeepDoors()
        {
            int count = 0;

            try
            {
                lock (Lock)
                {
                    foreach (List<GameDoorBase> doorList in m_doors.Values)
                    {
                        foreach (GameDoorBase door in doorList)
                        {
                            if (door.DbDoor != null &&
                                door is GameKeepDoor keepDoor &&
                                keepDoor.IsAttackableDoor)
                            {
                                keepDoor.SaveIntoDatabase();
                                count++;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Error saving keep doors.", e);
            }

            return count;
        }

        /// <summary>
        /// This function loads a door from the database, checking for Relic, Keep, or Standard type.
        /// </summary>
        public static bool LoadDoor(DbDoor door)
        {
            GameDoorBase mydoor = null;
            
            // Die Zone wird immer noch anhand der InternalID berechnet (geht nur mit INT/LONG)
            ushort zone = (ushort)(door.InternalID / 1000000); 

            Zone currentZone = WorldMgr.GetZone(zone);
            if (currentZone == null) return false;
            
            // RelicGate Typ-Erkennung und Instanziierung
            Type requiredType = RelicGateMgr.GetRelicGateType(door.InternalID); 

            if (requiredType != null)
            {
                mydoor = (GameDoorBase)Activator.CreateInstance(requiredType);
                if (mydoor is RelicGate relicGate)
                {
                    RelicGateMgr.AssignRelicDoor(relicGate, door.InternalID);
                }
            }

            // check if the door is a keep door (Nur, wenn mydoor noch nicht instanziiert wurde)
            string[] villages = { "Crair Treflan", "Magh Tuireadh", "Catterick Hamlet", "Dinas Emrys", "Godrborg", "Rensamark" };
            if (mydoor == null)
            {
                foreach (AbstractArea area in currentZone.GetAreasOfSpot(door.X, door.Y, door.Z))
                {
                    // Villages in NF have normal doors => we will do this better
                    // We have more keeps, which are not really keeps (Cathal Valley portal keeps should be normal doors)
                    // We should check if area is a real keep
                    // We have keeps in NF & Battlegrounds
                    if (!villages.Contains(area.Description))
                    {
                        if (area is KeepArea)
                        {
                            mydoor = new GameKeepDoor();
                            break;
                        }
                    }
                }
            }

            // if the door is not a keep door, create a standard door
            if (mydoor == null)
            {
                mydoor = new GameDoor();
            }
            mydoor.LoadFromDatabase(door);

            // add to the list of doors
            if (mydoor != null)
            {
                RegisterDoor(mydoor);
            }

            return true;
        }

        public static void RegisterDoor(GameDoorBase door)
        {
            lock (Lock)
            {
                if (!m_doors.TryGetValue(door.DoorId, out List<GameDoorBase> doorsOfId))
                {
                    doorsOfId = [];
                    m_doors.Add(door.DoorId, doorsOfId);
                }

                doorsOfId.Add(door);
            }
        }

        public static void UnRegisterDoor(int doorID)
        {
            m_doors.Remove(doorID);
        }

        /// <summary>
        /// This function get the door object by door index
        /// </summary>
        /// <returns>return the door with the index</returns>
        public static List<GameDoorBase> GetDoorByID(int id)
        {
            return m_doors.TryGetValue(id, out List<GameDoorBase> value) ? value : [];
        }
    }
}