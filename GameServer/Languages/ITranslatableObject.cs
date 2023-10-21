using Core.Database;

namespace Core.Language;

public interface ITranslatableObject
{
    string TranslationId { get; set; }

    LanguageDataObject.eTranslationIdentifier TranslationIdentifier { get; }
}