using Core.Database.Enums;

namespace Core.Database.Tables;

// data table attribute not set until item translations are supported.
class DbLanguageGameItem : LanguageDataObject
{
    public override ETranslationIdType TranslationIdentifier
    {
        get { return ETranslationIdType.eItem; }
    }
}