using Core.Database.Enums;

namespace Core.GS.Languages;

public interface ITranslatableObject
{
    string TranslationId { get; set; }

    ETranslationIdType TranslationIdentifier { get; }
}