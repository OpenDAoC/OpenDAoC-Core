using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    /// DOL Characters (Player) Custom Params linked to Character Entry
    /// </summary>
    [DataTable(TableName = "DOLCharactersXCustomParam")]
    public class DbCoreCharacterXCustomParam : CustomParam
    {
        private string m_dOLCharactersObjectId;

        /// <summary>
        /// DOLCharacters Table ObjectId Reference
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true, Varchar = 255)]
        public string DOLCharactersObjectId
        {
            get { return m_dOLCharactersObjectId; }
            set { Dirty = true; m_dOLCharactersObjectId = value; }
        }

        /// <summary>
        /// Create new instance of <see cref="DbCoreCharacterXCustomParam"/> linked to Character ObjectId
        /// </summary>
        /// <param name="DOLCharactersObjectId">DOLCharacters ObjectId</param>
        /// <param name="KeyName">Key Name</param>
        /// <param name="Value">Value</param>
        public DbCoreCharacterXCustomParam(string DOLCharactersObjectId, string KeyName, string Value)
            : base(KeyName, Value)
        {
            this.DOLCharactersObjectId = DOLCharactersObjectId;
        }

        /// <summary>
        /// Create new instance of <see cref="DbCoreCharacterXCustomParam"/>
        /// </summary>
        public DbCoreCharacterXCustomParam()
        {
        }
    }
}
