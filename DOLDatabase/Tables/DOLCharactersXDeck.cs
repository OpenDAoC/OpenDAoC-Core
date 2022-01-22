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

using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table that holds the different characters and guilds that have been given permissions to a house.
	/// </summary>
	[DataTable(TableName = "DOLCharactersXDeck")]
	public class DOLCharactersXDeck : DataObject
	{
		//important data
		private int m_DOLCharactersXDeck_ID;
		private string m_deck;
		private string m_dOLCharactersObjectId;
		private DateTime m_lastModifiedTime;
		
		public DOLCharactersXDeck()
		{
			m_lastModifiedTime = DateTime.Now;
		}
		
		/// <summary>
		/// DOLCharacters Table ObjectId Reference
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true, Varchar = 255)]
		public string DOLCharactersObjectId
		{
			get { return m_dOLCharactersObjectId; }
			set
			{
				Dirty = true;
				m_dOLCharactersObjectId = value;
			}
		}

		/// <summary>
		/// DOLCharacters Table ObjectId Reference
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true)]
		public string Deck
		{
			get { return m_deck; }
			set
			{
				Dirty = true;
				m_deck = value;
			}
		}

		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int DOLCharactersXDeck_ID
		{
			get { return m_DOLCharactersXDeck_ID; }
			set
			{
				Dirty = true;
				m_DOLCharactersXDeck_ID = value;
			}
		}

		/// <summary>
		/// Gets or sets the the time this mapping was created.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public DateTime LastModifiedTime
		{
			get { return m_lastModifiedTime; }
			set 
			{
				Dirty = true;
				m_lastModifiedTime = value;
			}
		}
	}
}