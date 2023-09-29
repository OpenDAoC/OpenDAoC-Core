using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table that holds the different characters and guilds that have been given permissions to a house.
	/// </summary>
	[DataTable(TableName = "TaskRefreshIntervals")]
	public class DbTaskRefreshInterval : DataObject
	{
		//important data
		private int m_DailyTaskRolloverTime_ID;
		private string m_RolloverInterval;
		private DateTime m_lastModifiedTime;
		private DateTime m_lastRollover;
		
		public DbTaskRefreshInterval()
		{
			m_lastModifiedTime = DateTime.Now;
		}

		/// <summary>
		/// Gets or sets the last time daily quests rolled over
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public DateTime LastRollover
		{
			get { return m_lastRollover; }
			set 
			{
				Dirty = true;
				m_lastRollover = value;
			}
		}
		
		/// <summary>
		/// The frequency with which this task should update.
		/// Options: DAILY | WEEKLY
		/// </summary>
		[DataElement(AllowDbNull = false, Index = false, Varchar = 0)]
		public string RolloverInterval
		{
			get { return m_RolloverInterval; }
			set
			{
				Dirty = true;
				m_RolloverInterval = value;
			}
		}

		/// <summary>
		/// Primary Key Auto Increment.
		/// </summary>
		[PrimaryKey(AutoIncrement = true)]
		public int DailyTaskRolloverTime_ID
		{
			get { return m_DailyTaskRolloverTime_ID; }
			set
			{
				Dirty = true;
				m_DailyTaskRolloverTime_ID = value;
			}
		}
	}
}