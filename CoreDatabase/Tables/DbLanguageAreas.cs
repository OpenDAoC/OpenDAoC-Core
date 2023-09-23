using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "LanguageArea")]
    public class DbLanguageAreas : LanguageDataObject
    {
        #region Variables
        private string m_description;
        private string m_screenDescription;
        #endregion Variables

        public DbLanguageAreas()
            : base() { }

        #region Properties
        public override ETranslationIdentifier TranslationIdentifier
        {
            get { return ETranslationIdentifier.eArea; }
        }

        /// <summary>
        /// The translated area description
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public string Description
        {
            get { return m_description; }
            set
            {
                Dirty = true;
                m_description = value;
            }
        }

        /// <summary>
        /// The translated area screen description
        /// </summary>
        [DataElement(AllowDbNull = false)]
        public string ScreenDescription
        {
            get { return m_screenDescription; }
            set
            {
                Dirty = true;
                m_screenDescription = value;
            }
        }
        #endregion Properties
    }
}