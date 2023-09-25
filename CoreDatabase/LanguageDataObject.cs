using DOL.Database.Attributes;

namespace DOL.Database
{
    public abstract class LanguageDataObject : DataObject
    {
        #region Enums
        public enum eTranslationIdentifier
            : byte
        {
            eArea = 0,
            eDoor = 1,
            eItem = 2,
            eNPC = 3,
            eObject = 4,
            eSystem = 5,
            eZone = 6
        }
        #endregion Enums

        #region Variables
        private string m_lng = "";
        private string m_tid = "";
        private string m_tag = "";
        #endregion Variables

        public LanguageDataObject() { }

        #region Properties
        public abstract eTranslationIdentifier TranslationIdentifier
        {
            get;
        }

        /// <summary>
        /// Gets or sets the translation id.
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true)]
        public string TranslationId
        {
            get { return m_tid; }
            set
            {
                Dirty = true;
                m_tid = value;
            }
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true)]
        public string Language
        {
            get { return m_lng; }
            set
            {
                Dirty = true;
                m_lng = value.ToUpper();
            }
        }

        /// <summary>
        /// Gets or sets costum data of / for the database row. Can be used to sort rows.
        /// </summary>
        [DataElement(AllowDbNull = true)]
        public string Tag
        {
            get { return m_tag; }
            set
            {
                Dirty = true;
                m_tag = value;
            }
        }
        #endregion Properties
    }
}