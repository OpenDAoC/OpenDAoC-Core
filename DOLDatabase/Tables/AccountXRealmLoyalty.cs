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
	[DataTable(TableName = "AccountXRealmLoyalty")]
	public class AccountXRealmLoyalty : DataObject
	{
		//important data
		private int m_realmLoyaltyID;
		private int m_Realm;
		private int m_LoyalDays;
		private string m_account_id;
		private DateTime m_lastLoyaltyUpdate;
		

		
		/// <summary>
		/// DOLCharacters Table ObjectId Reference
		/// </summary>
		[DataElement(AllowDbNull = false, Index = true, Varchar = 255)]
		public string AccountId
		{
			get { return m_account_id; }
			set
			{
				Dirty = true;
				m_account_id = value;
			}
		}

		/// <summary>
		/// DOLCharacters Table Deck Reference
		/// </summary>
		[DataElement(AllowDbNull = false, Index = false)]
		public int Realm
		{
			get { return m_Realm; }
			set
			{
				Dirty = true;
				m_Realm = value;
			}
		}
		
		[DataElement(AllowDbNull = false, Index = false)]
		public int LoyalDays
		{
			get { return m_LoyalDays; }
			set
			{
				Dirty = true;
				m_LoyalDays = value;
			}
		}
		
		[DataElement(AllowDbNull=true)]
		public DateTime LastLoyaltyUpdate
		{
			get
			{
				return m_lastLoyaltyUpdate;
			}
			set
			{
				Dirty = true;
				m_lastLoyaltyUpdate = value;
			}
		}

		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int RealmLoyaltyID
		{
			get { return m_realmLoyaltyID; }
			set
			{
				Dirty = true;
				m_realmLoyaltyID = value;
			}
		}
	}
}