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
	[DataTable(TableName = "AccountXMoney")]
	public class AccountXMoney : DataObject
	{
		//important data
		private int m_accountxmoney_ID;
		private int m_Realm;
		private int m_copper;
		private int m_silver;
		private int m_gold;
		private int m_platinum;
		private int m_mithril;
		private string m_account_id;
		private DateTime m_lastModifiedTime;
		
		public AccountXMoney()
		{
			m_lastModifiedTime = DateTime.Now;
		}
		
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
		public int Copper
		{
			get { return m_copper; }
			set
			{
				Dirty = true;
				m_copper = value;
			}
		}
		
		[DataElement(AllowDbNull = false, Index = false)]
		public int Silver
		{
			get { return m_silver; }
			set
			{
				Dirty = true;
				m_silver = value;
			}
		}
		
		[DataElement(AllowDbNull = false, Index = false)]
		public int Gold
		{
			get { return m_gold; }
			set
			{
				Dirty = true;
				m_gold = value;
			}
		}
		
		[DataElement(AllowDbNull = false, Index = false)]
		public int Platinum
		{
			get { return m_platinum; }
			set
			{
				Dirty = true;
				m_platinum = value;
			}
		}
		
		[DataElement(AllowDbNull = false, Index = false)]
		public int Mithril
		{
			get { return m_mithril; }
			set
			{
				Dirty = true;
				m_mithril = value;
			}
		}
		
		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int AccountxMoney_ID
		{
			get { return m_accountxmoney_ID; }
			set
			{
				Dirty = true;
				m_accountxmoney_ID = value;
			}
		}
	}
}