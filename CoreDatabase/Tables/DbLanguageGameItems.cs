namespace DOL.Database
{
    // data table attribute not set until item translations are supported.
    class DbLanguageGameItems : LanguageDataObject
    {
        public override ETranslationIdentifier TranslationIdentifier
        {
            get { return ETranslationIdentifier.eItem; }
        }
    }
}