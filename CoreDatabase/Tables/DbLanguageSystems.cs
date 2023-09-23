using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "LanguageSystem")]
    public class DbLanguageSystems : LanguageDataObject
    {
        #region Variables
        private string m_text = string.Empty;
        #endregion Variables

        public DbLanguageSystems()
            : base() { }


        #region Properties
        public override ETranslationIdentifier TranslationIdentifier
        {
            get { return ETranslationIdentifier.eSystem; }
        }

        [DataElement(AllowDbNull = false)]
        public string Text
        {
            get { return m_text; }
            set
            {
                Dirty = true;
                m_text = value;
            }
        }
        #endregion Properties
    }
}