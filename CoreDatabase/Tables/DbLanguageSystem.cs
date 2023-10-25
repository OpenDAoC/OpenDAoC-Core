using Core.Database.Enums;

namespace Core.Database.Tables
{
    [DataTable(TableName = "LanguageSystem")]
    public class DbLanguageSystem : LanguageDataObject
    {
        #region Variables
        private string m_text = string.Empty;
        #endregion Variables

        public DbLanguageSystem()
            : base() { }


        #region Properties
        public override ETranslationIdType TranslationIdentifier
        {
            get { return ETranslationIdType.eSystem; }
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