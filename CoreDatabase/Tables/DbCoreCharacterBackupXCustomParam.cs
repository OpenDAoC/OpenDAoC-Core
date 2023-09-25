using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    /// DOL Characters Backup Custom Params linked to Character Backup Entry
    /// </summary>
    [DataTable(TableName = "DOLCharactersBackupXCustomParam")]
    public class DbCoreCharacterBackupXCustomParam : DbCoreCharacterXCustomParam
    {
        /// <summary>
        /// Create new instance of <see cref="DbCoreCharacterBackupXCustomParam"/> linked to Backup'd Character ObjectId
        /// </summary>
        /// <param name="DOLCharactersObjectId">DOLCharacters ObjectId</param>
        /// <param name="KeyName">Key Name</param>
        /// <param name="Value">Value</param>
        public DbCoreCharacterBackupXCustomParam(string DOLCharactersObjectId, string KeyName, string Value)
            : base(DOLCharactersObjectId, KeyName, Value)
        {
        }

        /// <summary>
        /// Create new instance of <see cref="DbCoreCharacterBackupXCustomParam"/>
        /// </summary>
        public DbCoreCharacterBackupXCustomParam()
        {
        }
    }
}
