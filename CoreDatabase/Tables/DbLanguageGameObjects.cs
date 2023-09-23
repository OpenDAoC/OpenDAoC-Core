using DOL.Database.Attributes;

namespace DOL.Database
{
    [DataTable(TableName = "LanguageGameObject")]
    public class DbLanguageGameObjects : LanguageDataObject
    {
        #region Variables
        private string m_name = string.Empty;
        private string m_examineArticle = string.Empty;
        #endregion Variables

        public DbLanguageGameObjects()
            : base() { }

        #region Properties
        public override ETranslationIdentifier TranslationIdentifier
        {
            get { return ETranslationIdentifier.eObject; }
        }

        /// <summary>
        /// Gets or sets the translated name.
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public string Name
        {
            get { return m_name; }
            set
            {
                Dirty = true;
                m_name = value;
            }
        }

        /// <summary>
        /// Gets or sets the translated examine article.
        /// 
        /// You examine the Forge.
        /// 
        /// the = the examine article.
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public string ExamineArticle
        {
            get { return m_examineArticle; }
            set
            {
                Dirty = true;
                m_examineArticle = value;
            }
        }
        #endregion Properties
    }
}