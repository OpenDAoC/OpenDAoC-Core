using System;
using System.Reflection;
using System.Text;
using DOL.Database.Attributes;
using DOL.Logging;

namespace DOL.Database
{
    [DataTable(TableName = "LanguageSystem")]
    public class DbLanguageSystem : LanguageDataObject
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public CompositeFormat FormattableText;
        private string _text = string.Empty;

        public DbLanguageSystem(): base() { }

        public override eTranslationIdentifier TranslationIdentifier => eTranslationIdentifier.eSystem;

        [DataElement(AllowDbNull = false)]
        public string Text
        {
            get => _text;
            set
            {
                Dirty = true;
                _text = value;
            }
        }

        public void PrepareForFormatting()
        {
            if (string.IsNullOrEmpty(Text) || !Text.Contains('{'))
                return;

            try
            {
                FormattableText = CompositeFormat.Parse(Text);
            }
            catch (FormatException ex)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Invalid format string in language entry. TranslationId: '{TranslationId}', Text: '{Text}'", ex);
            }
        }
    }
}
