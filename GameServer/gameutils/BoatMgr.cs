using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS
{
    /// <summary>
    /// The BoatMgr holds pointers to all player boats
    /// </summary>
    public sealed class BoatMgr
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// ArrayList of all player boats in the game
        /// </summary>
        static private readonly HybridDictionary m_boats = new HybridDictionary();

        /// <summary>
        /// ArrayList of all boatid's to BoatNames
        /// </summary>
        static private readonly HybridDictionary m_boatids = new HybridDictionary();

        /// <summary>
        /// Adds a player boat to the list of boats
        /// </summary>
        /// <param name="boat">The boat to add</param>
        /// <returns>True if the function succeeded, otherwise false</returns>
        public static bool AddBoat(GameBoat boat)
        {
            if (boat == null)
                return false;

            lock (m_boats.SyncRoot)
            {
                if (!m_boats.Contains(boat.Name))
                {
                    m_boats.Add(boat.Name, boat);
                    m_boatids.Add(boat.BoatID, boat.Name);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a player boat from the manager
        /// </summary>
        /// <param name="boat">the boat</param>
        /// <returns></returns>
        public static bool RemoveBoat(GameBoat boat)
        {
            if (boat == null)
                return false;

            lock (m_boats.SyncRoot)
            {
                m_boats.Remove(boat.Name);
                m_boatids.Remove(boat.InternalID);
            }
            return true;
        }

        /// <summary>
        /// Checks if a boat with that boat name exists
        /// </summary>
        /// <param name="boatName">The boat to check</param>
        /// <returns>true or false</returns>
        public static bool DoesBoatExist(string boatName)
        {
            lock (m_boats.SyncRoot)
            {
                if (m_boats.Contains(boatName))
                    return true;
                return false;
            }
        }

        /// <summary>
        /// Creates a new boat
        /// </summary>
        /// <returns>BoatEntry</returns>
        public static GameBoat CreateBoat(GamePlayer creator, GameBoat boat)
        {
            if (log.IsDebugEnabled)
                log.Debug("Create boat; boat name=\"" + boat.Name + "\"");
            try
            {
                // Does boat exist, if so return null
                if (DoesBoatExist(boat.Name) == true)
                {
                    if (creator != null)
                        creator.Out.SendMessage(boat.Name + " already exists!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return null;
                }

                // Check if client exists
                if (creator == null)
                    return null;


                //create table of GameBoat
                boat.DBBoat = new DbPlayerBoat();
                boat.DBBoat.BoatOwner = creator.InternalID;
                boat.DBBoat.BoatID = boat.BoatID;
                boat.DBBoat.BoatMaxSpeedBase = boat.MaxSpeedBase;
                boat.DBBoat.BoatModel = boat.Model;
                boat.DBBoat.BoatName = boat.Name;
                boat.OwnerID = creator.InternalID;
                boat.Flags ^= GameNPC.eFlags.PEACE;

                AddBoat(boat);
                GameServer.Database.AddObject(boat.DBBoat);
                return boat;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("CreateBoat", e);
                return null;
            }
        }

        /// <summary>
        /// Delete's a boat
        /// </summary>
        /// <returns>true or false</returns>
        public static bool DeleteBoat(string boatName)
        {
            try
            {
                GameBoat removeBoat = GetBoatByName(boatName);
                // Does boat exist, if not return null
                if (removeBoat == null)
                {
                    return false;
                }

                var boats = GameServer.Database.FindObjectByKey<DbPlayerBoat>(boatName);
                GameServer.Database.DeleteObject(boats);

                RemoveBoat(removeBoat);

                return true;
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("DeleteBoat", e);
                return false;
            }
        }

        /// <summary>
        /// Returns a boat according to the matching name
        /// </summary>
        /// <returns>Boat</returns>
        public static GameBoat GetBoatByName(string boatName)
        {
            if (boatName == null) return null;
            lock (m_boats.SyncRoot)
            {
                return (GameBoat)m_boats[boatName];
            }
        }

        /// <summary>
        /// Returns a boat according to the matching database ID.
        /// </summary>
        /// <returns>Boat</returns>
        public static GameBoat GetBoatByBoatID(string boatid)
        {
            if (boatid == null) return null;

            lock (m_boatids.SyncRoot)
            {
                if (m_boatids[boatid] == null) return null;

                lock (m_boats.SyncRoot)
                {
                    return (GameBoat)m_boats[m_boatids[boatid]];
                }
            }
        }

        /// <summary>
        /// Returns a database ID for a matching boat name.
        /// </summary>
        /// <returns>Boat</returns>
        public static string BoatNameToBoatID(string boatName)
        {
            GameBoat b = GetBoatByName(boatName);
            if (b == null)
                return "";
            return b.BoatID;
        }

        /// <summary>
        /// Returns a boat according to the matching database boat Owner.
        /// </summary>
        /// <returns>Boat</returns>
        public static GameBoat GetBoatByOwner(string owner)
        {
            if (owner == null) return null;

            lock (m_boatids.SyncRoot)
            {
                foreach (GameBoat boat in m_boats.Values)
                {
                    if (boat.OwnerID == owner)
                        return boat;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Returns a list of boats by their status
        /// </summary>
        /// <returns>ArrayList of boats</returns>
        public static ICollection ListBoat()
        {
            return m_boats.Values;
        }

        /// <summary>
        /// Load all boats from the database
        /// </summary>
        public static bool LoadAllBoats()
        {
            lock (m_boats.SyncRoot)
            {
                m_boats.Clear();
            }

            //load boats
            var objs = GameServer.Database.SelectAllObjects<DbPlayerBoat>();
            foreach (var obj in objs)
            {
                GameBoat myboat = new GameBoat();
                myboat.LoadFromDatabase(obj);
                AddBoat(myboat);
            }

            return true;
        }

        /// <summary>
        /// Save all boats into database
        /// </summary>
        public static int SaveAllBoats()
        {
            int count = 0;

            try
            {
                lock (m_boats.SyncRoot)
                {
                    foreach (GameBoat b in m_boats.Values)
                    {
                        b.SaveIntoDatabase();
                        count++;
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Error saving boats", e);
            }

            return count;
        }

        public static ArrayList GetAllBoats()
        {
            ArrayList boats = new ArrayList();
            lock (m_boats.SyncRoot)
            {
                foreach (GameBoat boat in m_boats.Values)
                {
                    boats.Add(boat);
                }
            }
            return boats;
        }

        public static bool IsBoatOwner(string playerstrID, GameBoat boat)
        {
            if (playerstrID == null || boat == null)
                return false;

            if (playerstrID == boat.OwnerID)
                return true;
            else
                return false;
        }
    }
}
