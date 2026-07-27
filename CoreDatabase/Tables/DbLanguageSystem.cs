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

        public CompositeFormat FormattableText { get; private set; }
        public override eTranslationIdentifier TranslationIdentifier => eTranslationIdentifier.eSystem;

        [DataElement(AllowDbNull = false)]
        public string Text
        {
            get;
            set
            {
                Dirty = true;
                field = value;
            }
        } = string.Empty;

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
