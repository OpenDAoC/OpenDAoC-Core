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
 * Written by Biceps (thebiceps@gmail.com)
 * Distributed under the Creative Commons Attribution-NonCommercial-ShareAlike 3.0 license
 * http://creativecommons.org/licenses/by-nc-sa/3.0/
 * 
 * Added: 14:48 2007-07-04
 * Last updated: 22:32 2017-05
 * 
 * Updated by Unty for latest DOL revisions
 */

using System;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS;

namespace DOL.Database
{
    [DataTable(TableName = "JumpPoint")]
    public class DBJumpPoint : DataObject
    {
        private string m_name;
        private int m_xpos;
        private int m_ypos;
        private int m_zpos;
        private ushort m_region;
        private ushort m_heading;       

        /// <summary>
        /// Name of this JP
        /// </summary>
        [DataElement(AllowDbNull = false, Unique = true)]
        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                Dirty = true;
                m_name = value;
            }
        }

        /// <summary>
        /// The region of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public ushort Region
        {
            get
            {
                return m_region;
            }
            set
            {
                Dirty = true;
                m_region = value;
            }
        }

        /// <summary>
        /// The X position of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Xpos
        {
            get
            {
                return m_xpos;
            }
            set
            {
                Dirty = true;
                m_xpos = value;
            }
        }

        /// <summary>
        /// The Y position of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Ypos
        {
            get
            {
                return m_ypos;
            }
            set
            {
                Dirty = true;
                m_ypos = value;
            }
        }

        /// <summary>
        /// The Z position of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public int Zpos
        {
            get
            {
                return m_zpos;
            }
            set
            {
                Dirty = true;
                m_zpos = value;
            }
        }

        /// <summary>
        /// Heading of this JP
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public ushort Heading
        {
            get
            {
                return m_heading;
            }
            set
            {
                Dirty = true;
                m_heading = value;
            }
        }        

        [ScriptLoadedEvent]
		public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {           
        	GameServer.Database.RegisterDataObject(typeof (DBJumpPoint));                
                               
			Console.WriteLine("JumpPoints DB registered!");
        }
    }
}