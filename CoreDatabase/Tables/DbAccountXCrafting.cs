using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table that holds the crafting skills for each account-realm combination
	/// </summary>
	[DataTable(TableName = "AccountXCrafting")]
	public class DbAccountXCrafting : DataObject
	{
		//important data
		private int m_accountxcrafting_ID;
		private string m_account_id;
		private int m_Realm;
		private string m_craftingSkills = "1|1;2|1;3|1;4|1;6|1;7|1;8|1;9|1;10|1;11|1;12|1;13|1;14|1;15|1";// crafting skills
		private int m_primaryCraftingSkill = 0;// primary crafting skill
		private DateTime m_lastModifiedTime;
		
		public DbAccountXCrafting()
		{
			m_lastModifiedTime = DateTime.Now;
		}
		
		/// <summary>
		/// The account
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
		/// Crafting skills are account bound
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
		
		/// <summary>
		/// The crafting skills for the account
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string SerializedCraftingSkills
		{
			get { return m_craftingSkills; }
			set
			{
				Dirty = true;
				m_craftingSkills = value;
			}
		}
		
		/// <summary>
		/// Crafting skills are account bound
		/// </summary>
		[DataElement(AllowDbNull = false, Index = false)]
		public int CraftingPrimarySkill
		{
			get { return m_primaryCraftingSkill; }
			set
			{
				Dirty = true;
				m_primaryCraftingSkill = value;
			}
		}

		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int AccountxCrafting_ID
		{
			get { return m_accountxcrafting_ID; }
			set
			{
				Dirty = true;
				m_accountxcrafting_ID = value;
			}
		}
	}
}