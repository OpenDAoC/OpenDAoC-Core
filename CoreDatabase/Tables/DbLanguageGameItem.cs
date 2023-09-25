namespace DOL.Database
{
    // data table attribute not set until item translations are supported.
    class DbLanguageGameItem : LanguageDataObject
    {
        public override eTranslationIdentifier TranslationIdentifier
        {
            get { return eTranslationIdentifier.eItem; }
        }
    }
}