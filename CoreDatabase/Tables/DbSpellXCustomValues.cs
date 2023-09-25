using DOL.Database.Attributes;

namespace DOL.Database
{
    /// <summary>
    /// Spell Custom Values Table containing entries linked to spellID.
    /// </summary>
    [DataTable(TableName = "SpellXCustomValues")]
    public class DbSpellXCustomValues : CustomParam
    {
        private int m_spellID;

        /// <summary>
        /// Spell Table SpellID Reference
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true)]
        public int SpellID
        {
            get { return m_spellID; }
            set { Dirty = true; m_spellID = value; }
        }

        /// <summary>
        /// Create new instance of <see cref="DbSpellXCustomValues"/> linked to Spell ID.
        /// </summary>
        /// <param name="SpellID">Spell ID</param>
        /// <param name="KeyName">Key Name</param>
        /// <param name="Value">Value</param>
        public DbSpellXCustomValues(int SpellID, string KeyName, string Value)
            : base(KeyName, Value)
        {
            this.SpellID = SpellID;
        }

        /// <summary>
        /// Create new instance of <see cref="DbSpellXCustomValues"/>
        /// </summary>
        public DbSpellXCustomValues()
        {
        }
    }
}
