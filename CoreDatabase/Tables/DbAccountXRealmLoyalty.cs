using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table that holds the different characters and guilds that have been given permissions to a house.
	/// </summary>
	[DataTable(TableName = "AccountXRealmLoyalty")]
	public class DbAccountXRealmLoyalty : DataObject
	{
		//important data
		private int m_realmLoyaltyID;
		private int m_Realm;
		private int m_LoyalDays;
		private int m_MinimumLoyalDays;
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
		
		[DataElement(AllowDbNull = false, Index = false)]
		public int MinimumLoyalDays
		{
			get { return m_MinimumLoyalDays; }
			set
			{
				Dirty = true;
				m_MinimumLoyalDays = value;
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