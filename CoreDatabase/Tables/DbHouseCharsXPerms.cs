using System;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Table that holds the different characters and guilds that have been given permissions to a house.
	/// </summary>
	[DataTable(TableName = "DBHouseCharsXPerms")]
	public class DbHouseCharsXPerms : DataObject
	{
		//important data
		private int _houseNumber;
		private string _targetName;
		private string _displayName;
		private int _permissionLevel;
		private int _permissionType;
		private DateTime _creationTime;

		public DbHouseCharsXPerms()
		{}

		public DbHouseCharsXPerms(int houseNumber, string targetName, string displayName, int permissionLevel, int permissionType)
		{
			_houseNumber = houseNumber;
			_targetName = targetName;
			_displayName = displayName;
			_permissionLevel = permissionLevel;
			_permissionType = permissionType;
			_creationTime = DateTime.Now;
		}

		/// <summary>
		/// Gets or sets the house number this permission is associated with.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public int HouseNumber
		{
			get { return _houseNumber; }
			set
			{
				Dirty = true;
				_houseNumber = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of permission for this mapping
		/// </summary>
		/// <remarks>Type includes things like character, account, guild, class, etc.</remarks>
		[DataElement(AllowDbNull = false)]
		public int PermissionType
		{
			get { return _permissionType; }
			set
			{
				Dirty = true;
				_permissionType = value;
			}
		}

		/// <summary>
		/// Gets or sets the target name of the character, account, guild, etc, that is tied to this mapping.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public string TargetName
		{
			get { return _targetName; }
			set
			{
				Dirty = true;
				_targetName = value;
			}
		}

		/// <summary>
		/// Gets or sets the display name for the permission.
		/// </summary>
		/// <remarks>In cases of giving account-wide permissions to a player, this will be the character name
		/// at the time the permission was added, not the account name.</remarks>
		[DataElement(AllowDbNull = false)]
		public string DisplayName
		{
			get { return _displayName; }
			set
			{
				Dirty = true;
				_displayName = value;
			}
		}

		/// <summary>
		/// Gets or sets level of the permission.
		/// </summary>
		/// <remarks>Since permission levels are hard-coded, this value should never be anything other than 1 - 9.</remarks>
		[DataElement(AllowDbNull = false)]
		public int PermissionLevel
		{
			get { return _permissionLevel; }
			set
			{
				Dirty = true;
				_permissionLevel = value;
			}
		}

		/// <summary>
		/// Gets or sets the the time this mapping was created.
		/// </summary>
		[DataElement(AllowDbNull = false)]
		public DateTime CreationTime
		{
			get { return _creationTime; }
			set 
			{
				Dirty = true;
				_creationTime = value;
			}
		}
	}
}