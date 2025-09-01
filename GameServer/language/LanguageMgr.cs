using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DOL.Database;
using DOL.GS;
using DOL.GS.ServerProperties;
using DOL.Logging;

namespace DOL.Language
{
    public class LanguageMgr
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const string TRANSLATION_ID_EMPTY = "No translation ID could be found for this message.";

        private static LanguageMgr _soleInstance = new();
        protected string _langPathImpl = string.Empty;

        private static string LangPath
        {
            get
            {
                if (_soleInstance._langPathImpl == string.Empty)
                    _soleInstance._langPathImpl = Path.Combine(GameServer.Instance.Configuration.RootDirectory, "languages");

                return _soleInstance._langPathImpl;
            }
        }

        public static string DefaultLanguage => Properties.SERV_LANGUAGE;

        public static IEnumerable<string> Languages
        {
            get
            {
                foreach (string language in Translations.Keys)
                    yield return language;

                yield break;
            }
        }

        public static Dictionary<string, Dictionary<LanguageDataObject.eTranslationIdentifier, Dictionary<string, LanguageDataObject>>> Translations { get; private set; }

        public static void LoadTestDouble(LanguageMgr testDouble)
        {
            _soleInstance = testDouble;
        }

        protected virtual bool TryGetRawTranslation(out DbLanguageSystem translationObject, string language, string translationId)
        {
            translationObject = null;

            if (string.IsNullOrEmpty(language))
                language = DefaultLanguage;

            LanguageDataObject result = GetLanguageDataObject(language, translationId, LanguageDataObject.eTranslationIdentifier.eSystem);

            if (result is DbLanguageSystem dbResult && !string.IsNullOrEmpty(dbResult.Text))
            {
                translationObject = dbResult;
                return true;
            }

            return false;
        }

        public static bool Init()
        {
            Translations = new();
            return LoadTranslations();
        }

        private static bool LoadTranslations()
        {
            if (log.IsInfoEnabled)
                log.Info("Loading language system...");

            List<TranslationEntry> fileSentences = new();
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
                    List<TranslationEntry> sentences = ReadLanguageDirectory(Path.Combine(LangPath, language), language);

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
                if (log.IsErrorEnabled)
                    log.Error($"Could not find default '{DefaultLanguage}' language directory, server can't start without it");

                return false;
            }

            if (!defaultLanguageFilesFound)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Default '{DefaultLanguage}' language files missing, server can't start without those files");

                return false;
            }

            if (Properties.USE_DBLANGUAGE)
            {
                int newEntries = 0;
                int updatedEntries = 0;

                IList<DbLanguageSystem> dbos = GameServer.Database.SelectAllObjects<DbLanguageSystem>();

                if (Properties.UPDATE_EXISTING_DB_SYSTEM_SENTENCES_FROM_FILES)
                {
                    foreach (TranslationEntry sentence in fileSentences)
                    {
                        bool found = false;
                        foreach (DbLanguageSystem dbo in dbos)
                        {
                            if (dbo.TranslationId != sentence.Id)
                                continue;

                            if (dbo.Language != sentence.Language)
                                continue;

                            if (dbo.Text != sentence.Text)
                            {
                                dbo.Text = sentence.Text;
                                dbo.PrepareForFormatting(); // Reparse if text changes.
                                GameServer.Database.SaveObject(dbo);
                                updatedEntries++;

                                if (log.IsWarnEnabled)
                                    log.Warn($"Language {sentence.Language} TranslationId {dbo.TranslationId} updated in database");
                            }

                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            DbLanguageSystem dbo = new()
                            {
                                TranslationId = sentence.Id,
                                Text = sentence.Text,
                                Language = sentence.Language
                            };

                            dbo.PrepareForFormatting();
                            GameServer.Database.AddObject(dbo);
                            _ = RegisterLanguageDataObject(dbo);
                            newEntries++;

                            if (log.IsWarnEnabled)
                                log.Warn($"Language {dbo.Language} TranslationId {dbo.TranslationId} added into the database");
                        }
                    }
                }
                else // Add missing translations.
                {
                    foreach (TranslationEntry sentence in fileSentences)
                    {
                        bool found = false;
                        foreach (DbLanguageSystem lngObj in dbos)
                        {
                            if (lngObj.TranslationId != sentence.Id || lngObj.Language != sentence.Language)
                                continue;

                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            DbLanguageSystem dbo = new()
                            {
                                TranslationId = sentence.Id,
                                Text = sentence.Text,
                                Language = sentence.Language
                            };

                            dbo.PrepareForFormatting();
                            GameServer.Database.AddObject(dbo);
                            _ = RegisterLanguageDataObject(dbo);
                            newEntries++;

                            if (log.IsWarnEnabled)
                                log.Warn($"Language {dbo.Language} TranslationId {dbo.TranslationId} added into the database");
                        }
                    }
                }

                foreach (DbLanguageSystem dbo in dbos)
                {
                    dbo.PrepareForFormatting();
                    _ = RegisterLanguageDataObject(dbo);
                }

                if (newEntries > 0)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Added {newEntries} new entries into the database");
                }

                if (updatedEntries > 0)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Updated {updatedEntries} entries in database");
                }
            }
            else
            {
                foreach (TranslationEntry sentence in fileSentences)
                {
                    DbLanguageSystem obj = new()
                    {
                        TranslationId = sentence.Id,
                        Text = sentence.Text,
                        Language = sentence.Language
                    };

                    obj.PrepareForFormatting();
                    _ = RegisterLanguageDataObject(obj);
                }
            }

            if (log.IsInfoEnabled)
                log.Info("Loading object translations...");

            List<LanguageDataObject> lngObjs =
            [
                .. GameServer.Database.SelectAllObjects<DbLanguageArea>(),
                .. GameServer.Database.SelectAllObjects<DbLanguageGameObject>(),
                .. GameServer.Database.SelectAllObjects<DbLanguageGameNpc>(),
                .. GameServer.Database.SelectAllObjects<DbLanguageZone>(),
            ];

            foreach (LanguageDataObject lngObj in lngObjs)
                _ = RegisterLanguageDataObject(lngObj);

            return true;
        }

        private static List<TranslationEntry> ReadLanguageDirectory(string path, string language)
        {
            List<TranslationEntry> sentences = new();
            HashSet<(string, string)> uniqueKeys = new();

            foreach (string languageFile in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (!languageFile.EndsWith(".txt"))
                    continue;

                string[] lines = File.ReadAllLines(languageFile, Encoding.GetEncoding("utf-8"));
                string[] translation = new string[3];

                foreach (string line in lines)
                {
                    if (line.StartsWith('#') || !line.Contains(':'))
                        continue;

                    string id = line[..line.IndexOf(':')];
                    string text = line[(line.IndexOf(':') + 1)..].Replace("\t", " ").Trim();
                    TranslationEntry entry = new(id, text, language);

                    if (uniqueKeys.Add((id, language)))
                        sentences.Add(entry);
                }
            }

            return sentences;
        }

        public static LanguageDataObject GetLanguageDataObject(string language, string translationId, LanguageDataObject.eTranslationIdentifier translationIdentifier)
        {
            if (string.IsNullOrEmpty(translationId))
                return null;

            if (!Translations.TryGetValue(language, out var languages) && language != DefaultLanguage)
                _ = Translations.TryGetValue(DefaultLanguage, out languages);

            if (languages == null)
            {
                lock (Translations)
                {
                    _ = Translations.Remove(language);
                }

                return null;
            }

            if (!languages.TryGetValue(translationIdentifier, out var translationIdentifiers))
                return null;

            if (translationIdentifiers == null)
            {
                lock (Translations)
                {
                    _ = languages.Remove(translationIdentifier);
                }

                return null;
            }

            return !translationIdentifiers.TryGetValue(translationId, out LanguageDataObject result) ? null : result;
        }

        public static LanguageDataObject GetTranslation(GameClient client, ITranslatableObject obj)
        {
            _ = TryGetTranslation(out LanguageDataObject translation, client, obj);
            return translation;
        }

        public static LanguageDataObject GetTranslation(GamePlayer player, ITranslatableObject obj)
        {
            return GetTranslation(player.Client, obj);
        }

        public static LanguageDataObject GetTranslation(string language, ITranslatableObject obj)
        {
            _ = TryGetTranslation(out LanguageDataObject translation, language, obj);
            return translation;
        }

        public static string GetTranslation(GameClient client, string translationId)
        {
            _ = TryGetTranslation(out string t, client, translationId);
            return t;
        }

        public static string GetTranslation(GameClient client, string translationId, object arg1)
        {
            _ = TryGetTranslation(out string t, client, translationId, arg1);
            return t;
        }

        public static string GetTranslation(GameClient client, string translationId, object arg1, object arg2)
        {
            _ = TryGetTranslation(out string t, client, translationId, arg1, arg2);
            return t;
        }

        public static string GetTranslation(GameClient client, string translationId, object arg1, object arg2, object arg3)
        {
            _ = TryGetTranslation(out string t, client, translationId, arg1, arg2, arg3);
            return t;
        }

        public static string GetTranslation(GameClient client, string translationId, params ReadOnlySpan<object> args)
        {
            _ = TryGetTranslation(out string t, client, translationId, args);
            return t;
        }

        public static string GetTranslation(string language, string translationId)
        {
            _ = TryGetTranslation(out string t, language, translationId);
            return t;
        }

        public static string GetTranslation(string language, string translationId, object arg1)
        {
            _ = TryGetTranslation(out string t, language, translationId, arg1);
            return t;
        }

        public static string GetTranslation(string language, string translationId, object arg1, object arg2)
        {
            _ = TryGetTranslation(out string t, language, translationId, arg1, arg2);
            return t;
        }

        public static string GetTranslation(string language, string translationId, object arg1, object arg2, object arg3)
        {
            _ = TryGetTranslation(out string t, language, translationId, arg1, arg2, arg3);
            return t;
        }

        public static string GetTranslation(string language, string translationId, params ReadOnlySpan<object> args)
        {
            _ = TryGetTranslation(out string t, language, translationId, args);
            return t;
        }

        public static bool TryGetTranslation(out LanguageDataObject translation, GameClient client, ITranslatableObject obj)
        {
            if (client == null)
            {
                translation = null;
                return false;
            }

            return TryGetTranslation(out translation, client.Account?.Language ?? string.Empty, obj);
        }

        public static bool TryGetTranslation(out LanguageDataObject translation, string language, ITranslatableObject obj)
        {
            translation = null;

            if (obj == null)
                return false;

            if (string.IsNullOrEmpty(language) || language == DefaultLanguage)
                return false;

            translation = GetLanguageDataObject(language, obj.TranslationId, obj.TranslationIdentifier);
            return translation != null;
        }

        private static bool TryGetAndFormatTranslation(out string translation, GameClient client, string translationId, ReadOnlySpan<object> args)
        {
            string language = client?.Account?.Language ?? DefaultLanguage;
            bool result = TryGetTranslation(out translation, language, translationId, args);

            if (result && client?.Account?.PrivLevel > 1 && client.Player != null && client.ClientState == GameClient.eClientState.Playing)
            {
                if (client.Player.TempProperties.GetProperty<bool>("LANGUAGEMGR-DEBUG"))
                    translation = $"Id is {translationId} {translation}";
            }

            return result;
        }

        public static bool TryGetTranslation(out string translation, GameClient client, string translationId)
        {
            return TryGetAndFormatTranslation(out translation, client, translationId, []);
        }

        public static bool TryGetTranslation(out string translation, GameClient client, string translationId, object arg1)
        {
            return TryGetAndFormatTranslation(out translation, client, translationId, [arg1]);
        }

        public static bool TryGetTranslation(out string translation, GameClient client, string translationId, object arg1, object arg2)
        {
            return TryGetAndFormatTranslation(out translation, client, translationId, [arg1, arg2]);
        }

        public static bool TryGetTranslation(out string translation, GameClient client, string translationId, object arg1, object arg2, object arg3)
        {
            return TryGetAndFormatTranslation(out translation, client, translationId, [arg1, arg2, arg3]);
        }

        public static bool TryGetTranslation(out string translation, GameClient client, string translationId, params ReadOnlySpan<object> args)
        {
            return TryGetAndFormatTranslation(out translation, client, translationId, args);
        }

        public static bool TryGetTranslation(out string translation, string language, string translationId)
        {
            return TryGetTranslation(out translation, language, translationId, []);
        }

        public static bool TryGetTranslation(out string translation, string language, string translationId, object arg1)
        {
            return TryGetTranslation(out translation, language, translationId, [arg1]);
        }

        public static bool TryGetTranslation(out string translation, string language, string translationId, object arg1, object arg2)
        {
            return TryGetTranslation(out translation, language, translationId, [arg1, arg2]);
        }

        public static bool TryGetTranslation(out string translation, string language, string translationId, object arg1, object arg2, object arg3)
        {
            return TryGetTranslation(out translation, language, translationId, [arg1, arg2, arg3]);
        }

        public static bool TryGetTranslation(out string translation, string language, string translationId, params ReadOnlySpan<object> args)
        {
            if (string.IsNullOrEmpty(translationId))
            {
                translation = TRANSLATION_ID_EMPTY;
                return false;
            }

            if (!_soleInstance.TryGetRawTranslation(out DbLanguageSystem langObj, language, translationId) || langObj == null)
            {
                translation = GetTranslationErrorText(language, translationId);
                return false;
            }

            if (args.IsEmpty || langObj.FormattableText == null)
            {
                translation = langObj.Text;
                return true;
            }

            try
            {
                translation = args.Length switch
                {
                    // For 1-3 args, the dedicated `string.Format` overloads are the fastest path.
                    // Type inference prioritizes non-generic overloads over generic ones, so we specify <object> explicitly.
                    1 => string.Format<object>(null, langObj.FormattableText, args[0]),
                    2 => string.Format<object, object>(null, langObj.FormattableText, args[0], args[1]),
                    3 => string.Format<object, object, object>(null, langObj.FormattableText, args[0], args[1], args[2]),
                    _ => string.Format(null, langObj.FormattableText, args)
                };
            }
            catch (FormatException)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Parameter number incorrect: {translationId} for language {language}, Arg count = {args.Length}, sentence = '{langObj.Text}', args[0] = '{(args.Length > 0 ? args[0] : "null")}'");

                translation = langObj.Text;
            }

            return true;
        }

        public static string GetTranslationErrorText(string lang, string TranslationID)
        {
            try
            {
                if (TranslationID.Contains('.') && !TranslationID.TrimEnd().EndsWith('.') && !TranslationID.StartsWith('\''))
                    return string.Concat(lang, " ", TranslationID.AsSpan(TranslationID.LastIndexOf('.') + 1));
                else
                    return TranslationID; // Odds are a literal string was passed with no translation, so just return the string unmodified
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Error getting translation error text for {lang}:{TranslationID}", ex);
            }

            return $"{lang} translation error";
        }

        public static string TryTranslateOrDefault(GamePlayer player, string missingDefault, string translationId)
        {
            return player == null || !TryGetTranslation(out string retVal, player.Client, translationId) ? missingDefault : retVal;
        }

        public static string TryTranslateOrDefault(GamePlayer player, string missingDefault, string translationId, object arg1)
        {
            if (player != null && TryGetTranslation(out string retVal, player.Client, translationId, arg1))
                return retVal;
            try
            {
                return string.Format(missingDefault, arg1);
            }
            catch
            {
                return missingDefault;
            }
        }

        public static string TryTranslateOrDefault(GamePlayer player, string missingDefault, string translationId, object arg1, object arg2)
        {
            if (player != null && TryGetTranslation(out string retVal, player.Client, translationId, arg1, arg2))
                return retVal;
            try
            {
                return string.Format(missingDefault, arg1, arg2);
            }
            catch
            {
                return missingDefault;
            }
        }

        public static string TryTranslateOrDefault(GamePlayer player, string missingDefault, string translationId, ReadOnlySpan<object> args)
        {
            if (player != null && TryGetTranslation(out string retVal, player.Client, translationId, args))
                return retVal;

            try
            {
                return string.Format(missingDefault, args);
            }
            catch
            {
                return missingDefault;
            }
        }

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

                return translationIdentifiers.TryAdd(obj.TranslationId, obj);
            }
        }

        private record struct TranslationEntry(string Id, string Text, string Language);
    }
}
