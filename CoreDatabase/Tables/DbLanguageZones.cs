using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "LanguageZone")]
    public class DbLanguageZones : LanguageDataObject
    {
        #region Variables
        private string m_description;
        private string m_screenDescription;
        #endregion Variables

        public DbLanguageZones()
            : base() { }

        #region Properties
        public override ETranslationIdentifier TranslationIdentifier
        {
            get { return ETranslationIdentifier.eZone; }
        }

        /// <summary>
        /// The translated zone description
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
        /// The translated zone screen description
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