using Core.Database.Enums;

namespace Core.Database.Tables
{
    [DataTable(TableName = "LanguageArea")]
    public class DbLanguageArea : LanguageDataObject
    {
        #region Variables
        private string m_description;
        private string m_screenDescription;
        #endregion Variables

        public DbLanguageArea()
            : base() { }

        #region Properties
        public override ETranslationIdType TranslationIdentifier
        {
            get { return ETranslationIdType.eArea; }
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