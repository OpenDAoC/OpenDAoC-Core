using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table that holds progress towards achievements for accounts
	/// </summary>
	[DataTable(TableName = "Achievement")]
	public class DbAchievement : DataObject
	{
		//important data
		public string m_achievementName;
		private int m_Realm;
		public int m_count;
		private string m_account_id;
		public int m_achievementID;
		
		
		/// <summary>
		/// DOLCharacters Table AccountId Reference
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
		
		[DataElement(AllowDbNull = false, Index = false, Varchar = 0)]
		public string AchievementName
		{
			get { return m_achievementName; }
			set
			{
				Dirty = true;
				m_achievementName = value;
			}
		}

		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int AchievementID
		{
			get { return m_achievementID; }
			set
			{
				Dirty = true;
				m_achievementID = value;
			}
		}
		
		[DataElement(AllowDbNull = false, Index = false)]
		public int Count
		{
			get { return m_count; }
			set
			{
				Dirty = true;
				m_count = value;
			}
		}
		
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
	}
}