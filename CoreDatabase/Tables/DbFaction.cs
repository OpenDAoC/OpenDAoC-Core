using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Faction table
	/// </summary>
	[DataTable(TableName = "Faction")]
	public class DbFaction : DataObject
	{
		private string m_name;
		private int m_index;
		private int m_baseAggroLevel;

		/// <summary>
		/// Create faction
		/// </summary>
		public DbFaction()
		{
			AllowAdd = true;//Sinswolf 08.2011 false;
			ID = 0;
			m_baseAggroLevel = 0;
			m_name = String.Empty;
		}

		/// <summary>
		/// Index of faction
		/// </summary>
		[PrimaryKey]
		public int ID
		{
			get
			{
				return m_index;
			}
			set
			{
				m_index = value;
			}
		}

		/// <summary>
		/// Name of faction
		/// </summary>
		[DataElement(AllowDbNull = true)]
		public string Name
		{
			get
			{
				return m_name;
			}
			set
			{
				m_name = value;
			}
		}

		/// <summary>
		/// base friendship/relationship/aggro level at start for playe when never it before
		///
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int BaseAggroLevel
		{
			get { return m_baseAggroLevel; }
			set { m_baseAggroLevel = value; }
		}
	}
}