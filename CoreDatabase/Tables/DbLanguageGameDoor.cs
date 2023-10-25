using Core.Database.Enums;

namespace Core.Database.Tables;

// data table attribute not set until door translations are supported.
class DbLanguageGameDoor : LanguageDataObject
{
    public override ETranslationIdType TranslationIdentifier
    {
        get { return ETranslationIdType.eDoor; }
    }
}