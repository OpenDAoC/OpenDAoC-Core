namespace DOL.Database
{
    // data table attribute not set until door translations are supported.
    class DbLanguageDoors : LanguageDataObject
    {
        public override ETranslationIdentifier TranslationIdentifier
        {
            get { return ETranslationIdentifier.eDoor; }
        }
    }
}