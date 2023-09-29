namespace DOL.Database
{
    // data table attribute not set until door translations are supported.
    class DbLanguageGameDoor : LanguageDataObject
    {
        public override eTranslationIdentifier TranslationIdentifier
        {
            get { return eTranslationIdentifier.eDoor; }
        }
    }
}