/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using DOL.Database;

namespace DOL.GS
{
    public class GameBoat : GameMovingObject
    {
        private byte m_boatType = 0;
        protected DbPlayerBoat m_dbBoat;
        private string m_boatID;
        private string m_boatOwner;
        private string m_boatName;
        private ushort m_boatModel;
        private short m_boatMaxSpeedBase;
        private AuxECSGameTimer m_removeTimer = null;

        public GameBoat(byte type) : base()
        {
            m_boatType = type;
            base.OwnerID = BoatOwner;
        }

        public GameBoat() : base()
        {
            base.OwnerID = BoatOwner;
        }

        public override bool AddToWorld()
        {
            return base.AddToWorld();
        }

        /// <summary>
        /// Gets or sets the boats db
        /// </summary>
        public DbPlayerBoat DBBoat
        {
            get => m_dbBoat;
            set => m_dbBoat = value;
        }

        public string BoatID
        {
            get => m_boatID;
            set => m_boatID = value;
        }

        public override string Name
        {
            get => m_boatName;
            set => m_boatName = value;
        }

        public override ushort Model
        {
            get => m_boatModel;
            set => m_boatModel = value;
        }

        public override short MaxSpeedBase
        {
            get => m_boatMaxSpeedBase;
            set => m_boatMaxSpeedBase = value;
        }

        public string BoatOwner
        {
            get => m_boatOwner;
            set => m_boatOwner = value;
        }

        public override int MAX_PASSENGERS => m_boatType switch
        {
            0 => 8,
            1 => 8,
            2 => 16,
            3 => 32,
            4 => 32,
            5 => 31,
            6 => 24,
            7 => 64,
            8 => 33,
            _ => 2,
        };

        public override int REQUIRED_PASSENGERS => m_boatType switch
        {
            0 => 1,
            1 => 1,
            2 => 1,
            3 => 1,
            4 => 1,
            5 => 1,
            6 => 1,
            7 => 1,
            8 => 1,
            _ => 1,
        };

        public override int SLOT_OFFSET => 1;

        public override bool RiderMount(GamePlayer rider, bool forced)
        {
            if (!base.RiderMount(rider, forced))
                return false;

            if (m_removeTimer != null && m_removeTimer.IsAlive)
                m_removeTimer.Stop();

            return true;
        }

        public override bool RiderDismount(bool forced, GamePlayer player)
        {
            if (!base.RiderDismount(forced, player))
                return false;

            if (CurrentRiders.Length == 0)
            {
                if (m_removeTimer == null)
                    m_removeTimer = new AuxECSGameTimer(this, new AuxECSGameTimer.AuxECSTimerCallback(RemoveCallback));
                else if (m_removeTimer.IsAlive)
                    m_removeTimer.Stop();

                m_removeTimer.Start(15 * 60 * 1000);
            }

            return true;
        }

        protected int RemoveCallback(AuxECSGameTimer timer)
        {
            m_removeTimer.Stop();
            m_removeTimer = null;
            Delete();
            return 0;
        }

        public override bool Interact(GamePlayer player)
        {
            return OwnerID != "" ? false : base.Interact(player);
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            if (obj is not Database.DbPlayerBoat)
                return;

            m_dbBoat = (DbPlayerBoat) obj;
            m_boatID = m_dbBoat.ObjectId;
            m_boatName = m_dbBoat.BoatName;
            m_boatMaxSpeedBase = m_dbBoat.BoatMaxSpeedBase;
            m_boatModel = m_dbBoat.BoatModel;
            m_boatOwner = m_dbBoat.BoatOwner;

            switch (m_boatModel)
            {
                case 1616: m_boatType = 0; break;
                case 2648: m_boatType = 1; break;
                case 2646: m_boatType = 2; break;
                case 2647: m_boatType = 3; break;
                case 1615: m_boatType = 4; break;
                case 1595: m_boatType = 5; break;
                case 1612: m_boatType = 6; break;
                case 1613: m_boatType = 7; break;
                case 1614: m_boatType = 8; break;
            }

            DBBoat = m_dbBoat;
            base.LoadFromDatabase(obj);
        }

        public override void SaveIntoDatabase()
        {
            GameServer.Database.SaveObject(DBBoat);
        }
    }
}
