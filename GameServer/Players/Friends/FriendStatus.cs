using System;

namespace Core.GS.Friends
{
	/// <summary>
	/// Offline Friend Status Object to display in Social Windows
	/// </summary>
	public sealed class FriendStatus
	{
		/// <summary>
		/// Friend Name
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// Friend Level
		/// </summary>
		public int Level { get; private set; }
		/// <summary>
		/// Friend Class ID
		/// </summary>
		public int ClassID { get; private set; }
		/// <summary>
		/// Friend LastPlayed
		/// </summary>
		public DateTime LastPlayed { get; private set; }
		
		/// <summary>
		/// Create a new instance of <see cref="FriendStatus"/>
		/// </summary>
		/// <param name="Name">Friend Name</param>
		/// <param name="Level">Friend Level</param>
		/// <param name="ClassID">Friend Class ID</param>
		/// <param name="LastPlayed">Friend LastPlayed</param>
		public FriendStatus(string Name, int Level, int ClassID, DateTime LastPlayed)
		{
			this.Name = Name;
			this.Level = Level;
			this.ClassID = ClassID;
			this.LastPlayed = LastPlayed;
		}
	}
}