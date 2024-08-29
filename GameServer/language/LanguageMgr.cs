using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DOL.Database;
using DOL.GS;
using log4net;

namespace DOL.Language
{
    public class LanguageMgr
    {
        private static LanguageMgr soleInstance = new();

        public static void LoadTestDouble(LanguageMgr testDouble) { soleInstance = testDouble; }

        protected virtual bool TryGetTranslationImpl(out string translation, ref string language, string translationId, ref object[] args)
        {
            if (string.IsNullOrEmpty(translationId))
            {
                translation = TRANSLATION_ID_EMPTY;
                return false;
            }

            if (string.IsNullOrEmpty(language))
                language = DefaultLanguage;

            LanguageDataObject result = GetLanguageDataObject(language, translationId, LanguageDataObject.eTranslationIdentifier.eSystem);

            if (result == null)
            {
                translation = GetTranslationErrorText(language, translationId);
                return false;
            }
            else
            {
                if (!string.IsNullOrEmpty(((DbLanguageSystem) result).Text))
                    translation = ((DbLanguageSystem) result).Text;
                else
                {
                    translation = GetTranslationErrorText(language, translationId);
                    return false;
                }
            }

            args ??= Array.Empty<object>();

            try
            {
                if (args.Length > 0)
                    translation = string.Format(translation, args);
            }
            catch
            {
                log.ErrorFormat("[Language-Manager] Parameter number incorrect: {0} for language {1}, Arg count = {2}, sentence = '{3}', args[0] = '{4}'", translationId, language, args.Length, translation, args.Length > 0 ? args[0] : "null");
            }

            return true;
        }

        #region Variables
        private const string TRANSLATION_ID_EMPTY = "No translation ID could be found for this message.";

        /// <summary>
        /// Translation ID for the sentence, array position 0
        /// </summary>
        private const int ID = 0;

        /// <summary>
        /// The translated sentence, array position 1
        /// </summary>
        private const int TEXT = 1;

        /// <summary>
        /// The sentence language, array position 2
        /// </summary>
        private const int LANGUAGE = 2;

        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Give a way to change or relocate the lang files
        /// </summary>
        private static string LangPath {
            get
            {
                if (soleInstance.LangPathImpl == string.Empty)
                    soleInstance.LangPathImpl = Path.Combine(GameServer.Instance.Configuration.RootDirectory, "languages");

                return soleInstance.LangPathImpl;
            }
        }
        protected string LangPathImpl = string.Empty;
        #endregion Variables

        #region Properties
        /// <summary>
        /// Returns the default language.
        /// </summary>
        public static string DefaultLanguage => GS.ServerProperties.Properties.SERV_LANGUAGE;

        /// <summary>
        /// Returns all registered languages.
        /// </summary>
        public static IEnumerable<string> Languages
        {
            get
            {
                foreach (string language in Translations.Keys)
                    yield return language;

                yield break;
            }
        }

        /// <summary>
        /// Returns the translations collection. MODIFY AT YOUR OWN RISK!!!
        /// </summary>
        public static Dictionary<string, Dictionary<LanguageDataObject.eTranslationIdentifier, Dictionary<string, LanguageDataObject>>> Translations { get; private set; }
        #endregion Properties

        #region Initialization
        /// <summary>
        /// Initial function
        /// </summary>
        /// <returns></returns>
        public static bool Init()
        {
            Translations = new();
            return LoadTranslations();
        }

        #region LoadTranslations
        private static bool LoadTranslations()
        {
            #region Load system translations
            if (log.IsDebugEnabled)
                log.Info("[Language-Manager] Loading system sentences...");

            ArrayList fileSentences = new();
            bool defaultLanguageDirectoryFound = false;
            bool defaultLanguageFilesFound = false;
            foreach (string langDir in Directory.GetDirectories(LangPath, "*", SearchOption.TopDirectoryOnly))
            {
                string language = langDir[(langDir.LastIndexOf(Path.DirectorySeparatorChar) + 1)..].ToUpper();
                if (language != DefaultLanguage)
                {
                    if (language != "CU") // Ignore the custom language folder. This check should be removed in the future! (code written: may 2012)
                        fileSentences.AddRange(ReadLanguageDirectory(Path.Combine(LangPath, language), language));
                }
                else
                {
                    defaultLanguageDirectoryFound = true;
                    ArrayList sentences = ReadLanguageDirectory(Path.Combine(LangPath, language), language);

                    if (sentences.Count < 1)
                        break;
                    else
                    {
                        fileSentences.AddRange(sentences);
                        defaultLanguageFilesFound = true;
                    }
                }
            }

            if (!defaultLanguageDirectoryFound)
            {
                log.Error("Could not find default '" + DefaultLanguage + "' language directory, server can't start without it!");
                return false;
            }

            if (!defaultLanguageFilesFound)
            {
                log.Error("Default '" + DefaultLanguage + "' language files missing, server can't start without those files!");
                return false;
            }

            if (DOL.GS.ServerProperties.Properties.USE_DBLANGUAGE)
            {
                int newEntries = 0;
                int updatedEntries = 0;

                IList<DbLanguageSystem> dbos = GameServer.Database.SelectAllObjects<DbLanguageSystem>();

                if (GS.ServerProperties.Properties.UPDATE_EXISTING_DB_SYSTEM_SENTENCES_FROM_FILES)
                {
                    foreach (string[] sentence in fileSentences)
                    {
                        bool found = false;
                        foreach (DbLanguageSystem dbo in dbos)
                        {
                            if (dbo.TranslationId != sentence[ID])
                                continue;

                            if (dbo.Language != sentence[LANGUAGE])
                                continue;

                            if (dbo.Text != sentence[TEXT])
                            {
                                dbo.Text = sentence[TEXT];
                                GameServer.Database.SaveObject(dbo); // Please be sure to use the UTF-8 format for your language files, otherwise
                                // some database rows will be updated on each server start, because one char
                                // differs from the one within the database.
                                updatedEntries++;

                                if (log.IsWarnEnabled)
                                    log.Warn("[Language-Manager] Language <" + sentence[LANGUAGE] + "> TranslationId <" + dbo.TranslationId + "> updated in database!");
                            }

                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            DbLanguageSystem dbo = new()
                            {
                                TranslationId = sentence[ID],
                                Text = sentence[TEXT],
                                Language = sentence[LANGUAGE]
                            };

                            GameServer.Database.AddObject(dbo);
                            RegisterLanguageDataObject(dbo);
                            newEntries++;

                            if (log.IsWarnEnabled)
                                log.Warn("[Language-Manager] Language <" + dbo.Language + "> TranslationId <" + dbo.TranslationId + "> added into the database.");
                        }
                    }
                }
                else // Add missing translations.
                {
                    foreach (string[] sentence in fileSentences)
                    {
                        bool found = false;
                        foreach (DbLanguageSystem lngObj in dbos)
                        {
                            if (lngObj.TranslationId != sentence[ID])
                                continue;

                            if (lngObj.Language != sentence[LANGUAGE])
                                continue;

                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            DbLanguageSystem dbo = new()
                            {
                                TranslationId = sentence[ID],
                                Text = sentence[TEXT],
                                Language = sentence[LANGUAGE]
                            };

                            GameServer.Database.AddObject(dbo);
                            RegisterLanguageDataObject(dbo);
                            newEntries++;

                            if (log.IsWarnEnabled)
                                log.Warn("[Language-Manager] Language <" + dbo.Language + "> TranslationId <" + dbo.TranslationId + "> added into the database.");
                        }
                    }
                }

                // Register all DBLanguageSystem rows. Must be done in this way to
                // register ALL database rows. The reason for this is simple:
                //
                // If a user adds new rows into the database without also adding those
                // data into the language files, the above foreach loop just adds the
                // sentences which have been added in the language files.
                foreach (DbLanguageSystem dbo in dbos)
                    RegisterLanguageDataObject(dbo);

                if (newEntries > 0)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("[Language-Manager] Added <" + newEntries + "> new entries into the Database.");
                }

                if (updatedEntries > 0)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("[Language-Manager] Updated <" + updatedEntries + "> entries in Database.");
                }
            }
            else
            {
                foreach (string[] sentence in fileSentences)
                {
                    DbLanguageSystem obj = new()
                    {
                        TranslationId = sentence[ID],
                        Text = sentence[TEXT],
                        Language = sentence[LANGUAGE]
                    };

                    RegisterLanguageDataObject(obj);
                }
            }

            #endregion Load system translations

            #region Load object translations
            if (log.IsDebugEnabled)
                log.Info("[Language-Manager] Loading object translations...");

            List<LanguageDataObject> lngObjs = new();
            lngObjs.AddRange(GameServer.Database.SelectAllObjects<DbLanguageArea>());
            lngObjs.AddRange(GameServer.Database.SelectAllObjects<DbLanguageGameObject>());
            lngObjs.AddRange(GameServer.Database.SelectAllObjects<DbLanguageGameNpc>());
            lngObjs.AddRange(GameServer.Database.SelectAllObjects<DbLanguageZone>());

            foreach (LanguageDataObject lngObj in lngObjs)
                RegisterLanguageDataObject(lngObj);

            #endregion Load object translations
            return true;
        }
        #endregion LoadTranslations

        #region ReadLanguageDirectory
        private static ArrayList ReadLanguageDirectory(string path, string language)
        {
            ArrayList sentences = new();
            foreach (string languageFile in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (!languageFile.EndsWith(".txt"))
                    continue;

                string[] lines = File.ReadAllLines(languageFile, Encoding.GetEncoding("utf-8"));
                ArrayList textList = new(lines);

                foreach (string line in textList)
                {
                    // do not read comments
                    if (line.StartsWith("#"))
                        continue;

                    // ignore any line that is not formatted  'identifier: sentence'
                    if (!line.Contains(':'))
                        continue;

                    string[] translation = new string[3];

                    // 0 is the identifier for the sentence
                    translation[ID] = line[..line.IndexOf(':')];
                    translation[TEXT] = line[(line.IndexOf(':') + 1)..];

                    // 1 is the sentence with any tabs (used for readability in language file) removed
                    translation[TEXT] = translation[TEXT].Replace("\t", " ");
                    translation[TEXT] = translation[TEXT].Trim();

                    // 2 is the language of the sentence
                    translation[LANGUAGE] = language;

                    // Ignore duplicates
                    bool ignore = false;
                    foreach (string[] sentence in sentences)
                    {
                        if (sentence[ID] != translation[ID])
                            continue;

                        if (sentence[LANGUAGE] != translation[LANGUAGE])
                            continue;

                        ignore = true;
                        break;
                    }

                    if (ignore)
                        continue;

                    sentences.Add(translation);
                }
            }

            return sentences;
        }
        #endregion ReadLanguageDirectory

        #endregion Initialization

        #region GetLanguageDataObject
        public static LanguageDataObject GetLanguageDataObject(string language, string translationId, LanguageDataObject.eTranslationIdentifier translationIdentifier)
        {
            if (string.IsNullOrEmpty(translationId))
                return null;

            if (!Translations.TryGetValue(language, out var languages) && language != DefaultLanguage)
                Translations.TryGetValue(DefaultLanguage, out languages);

            if (languages == null)
            {
                lock (Translations)
                    Translations.Remove(language);

                return null;
            }

            if (!languages.TryGetValue(translationIdentifier, out var translationIdentifiers))
                return null;

            if (translationIdentifiers == null)
            {
                lock (Translations)
                    languages.Remove(translationIdentifier);

                return null;
            }

            return !translationIdentifiers.TryGetValue(translationId, out LanguageDataObject result) ? null : result;
        }
        #endregion GetLanguageDataObject

        #region GetTranslation / TryGetTranslation

        #region GetTranslation
        public static LanguageDataObject GetTranslation(GameClient client, ITranslatableObject obj)
        {
            TryGetTranslation(out LanguageDataObject translation, client, obj);
            return translation;
        }
        
        public static LanguageDataObject GetTranslation(GamePlayer player, ITranslatableObject obj)
        {
            return GetTranslation(player.Client, obj);
        }

        public static LanguageDataObject GetTranslation(string language, ITranslatableObject obj)
        {
            TryGetTranslation(out LanguageDataObject translation, language, obj);
            return translation;
        }

        public static string GetTranslation(GameClient client, string translationId, params object[] args)
        {
            TryGetTranslation(out string translation, client, translationId, args);
            return translation;
        }

        public static string GetTranslation(string language, string translationId, params object[] args)
        {
            TryGetTranslation(out string translation, language, translationId, args);
            return translation;
        }
        #endregion GetTranslation

        #region TryGetTranslation
        public static bool TryGetTranslation(out LanguageDataObject translation, GameClient client, ITranslatableObject obj)
        {
            if (client == null)
            {
                translation = null;
                return false;
            }

            return TryGetTranslation(out translation, client.Account == null ? string.Empty : client.Account.Language, obj);
        }

        public static bool TryGetTranslation(out LanguageDataObject translation, string language, ITranslatableObject obj)
        {
            if (obj == null)
            {
                translation = null;
                return false;
            }

            if (string.IsNullOrEmpty(language) || language == DefaultLanguage /*Use the objects base data (e.g. NPC.Name)*/)
            {
                translation = null;
                return false;
            }

            translation = GetLanguageDataObject(language, obj.TranslationId, obj.TranslationIdentifier);
            return translation != null;
        }

        public static bool TryGetTranslation(out string translation, GameClient client, string translationId, params object[] args)
        {
            if (client == null)
            {
                translation = null;
                return true;
            }

            bool result = TryGetTranslation(out translation, client.Account == null ? DefaultLanguage : client.Account.Language, translationId, args);

            if (client.Account != null)
            {
                if (client.Account.PrivLevel > 1 && client.Player != null && result)
                {
                    if (client.ClientState == GameClient.eClientState.Playing)
                    {
                        if (client.Player.TempProperties.GetProperty<bool>("LANGUAGEMGR-DEBUG"))
                            translation = "Id is " + translationId + " " + translation;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// This returns the last part of the translation text id if actual translation fails
        /// This helps to avoid returning strings that are too long and overflow the client
        /// When the name overflows players my not be targetable or even visible!
        /// PLEASE DO NOT REMOVE THIS FUNCTIONALITY  - tolakram
        /// </summary>
        public static string GetTranslationErrorText(string lang, string TranslationID)
        {
            try
            {
                if (TranslationID.Contains('.') && TranslationID.TrimEnd().EndsWith('.') == false && TranslationID.StartsWith('\'') == false)
                    return string.Concat(lang, " ", TranslationID.AsSpan(TranslationID.LastIndexOf('.') + 1));
                else
                    return TranslationID; // Odds are a literal string was passed with no translation, so just return the string unmodified
            }
            catch (Exception ex)
            {
                log.Error("Error Getting Translation Error Text for " + lang + ":" + TranslationID, ex);
            }

            return lang + " Translation Error!";
        }
        

        public static bool TryGetTranslation(out string translation, string language, string translationId, params object[] args)
        {
            return soleInstance.TryGetTranslationImpl(out translation, ref language, translationId, ref args);
        }
        #endregion TryGetTranslation

        #endregion GetTranslation / TryGetTranslation

        #region utils

        /// <summary>
        /// Try Translating some Sentence into Player target Language or Default to given String.
        /// </summary>
        /// <param name="player">Targeted player</param>
        /// <param name="missingDefault">Default String if Missing Translation</param>
        /// <param name="translationId">Translation Sentence ID</param>
        /// <param name="args">Translation Sentence Params</param>
        /// <returns>Translated Sentence or Default string.</returns>
        public static string TryTranslateOrDefault(GamePlayer player, string missingDefault, string translationId, params object[] args)
        {
            string missing = missingDefault;

            if (args.Length > 0)
            {
                try
                {
                    missing = string.Format(missingDefault, args);
                }
                catch { }
            }

            return player == null || player.Client == null || player.Client.Account == null || !TryGetTranslation(out string retval, player.Client.Account.Language, translationId, args)
                ? missing
                : retval;
        }

        #endregion

        #region RegisterLanguageDataObject
        public static bool RegisterLanguageDataObject(LanguageDataObject obj)
        {
            if (obj == null)
                return false;

            lock (Translations)
            {
                if (!Translations.TryGetValue(obj.Language, out var languages))
                {
                    languages = new();
                    Translations.Add(obj.Language, languages);
                }

                if (!languages.TryGetValue(obj.TranslationIdentifier, out var translationIdentifiers))
                {
                    translationIdentifiers = new();
                    languages.Add(obj.TranslationIdentifier, translationIdentifiers);
                }

                if (!translationIdentifiers.TryGetValue(obj.TranslationId, out LanguageDataObject languageDataObject))
                {
                    translationIdentifiers.Add(obj.TranslationId, obj);
                    return true;
                }
            }

            return false; // Object is 'NULL' or already in list.
        }
        #endregion RegisterLanguageDataObject
    }
}
