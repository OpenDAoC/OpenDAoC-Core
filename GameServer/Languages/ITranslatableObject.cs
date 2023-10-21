using Core.Database;
using Core.Database.Enums;

namespace Core.Language;

public interface ITranslatableObject
{
    string TranslationId { get; set; }

    ETranslationIdType TranslationIdentifier { get; }
}