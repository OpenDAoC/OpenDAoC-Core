using System;
using System.Linq;
using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// DOL Characters Backup Custom Params linked to Character Backup Entry
	/// </summary>
	[DataTable(TableName = "DOLCharactersBackupXCustomParam")]
	public class DbCoreCharactersBackupXCustomParam : DbCoreCharactersXCustomParam
	{
		/// <summary>
		/// Create new instance of <see cref="DbCoreCharactersBackupXCustomParam"/> linked to Backup'd Character ObjectId
		/// </summary>
		/// <param name="DOLCharactersObjectId">DOLCharacters ObjectId</param>
		/// <param name="KeyName">Key Name</param>
		/// <param name="Value">Value</param>
		public DbCoreCharactersBackupXCustomParam(string DOLCharactersObjectId, string KeyName, string Value)
			: base(DOLCharactersObjectId, KeyName, Value)
		{
		}

		/// <summary>
		/// Create new instance of <see cref="DbCoreCharactersBackupXCustomParam"/>
		/// </summary>
		public DbCoreCharactersBackupXCustomParam()
		{
		}
	}
}
