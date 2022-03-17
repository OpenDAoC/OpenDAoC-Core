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
	[DataTable(TableName = "TaskRefreshIntervals")]
	public class TaskRefreshIntervals : DataObject
	{
		//important data
		private int m_DailyTaskRolloverTime_ID;
		private string m_RolloverInterval;
		private DateTime m_lastModifiedTime;
		private DateTime m_lastRollover;
		
		public TaskRefreshIntervals()
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