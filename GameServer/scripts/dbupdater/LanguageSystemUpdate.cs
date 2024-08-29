using System.Collections.Generic;
using DOL.Database;
using DOL.Database.Attributes;
using log4net;

namespace DOL.GS.DatabaseUpdate
{
    [DatabaseUpdate]
    public class LanguageSystemUpdate : IDatabaseUpdater
    {
        #region DBLanguage table structure
        private class language : DataObject
        {
            protected string m_translationid;
            protected string m_EN = string.Empty;
            protected string m_DE = string.Empty;
            protected string m_FR = string.Empty;
            protected string m_IT = string.Empty;
            protected string m_CU = string.Empty;
            protected string m_packageID;

            public language() { }

            [DataElement(AllowDbNull = false, Unique = true)]
            public string TranslationID
            {
                get { return m_translationid; }
                set { Dirty = true; m_translationid = value; }
            }

            [DataElement(AllowDbNull = false)]
            public string EN
            {
                get { return m_EN; }
                set { Dirty = true; m_EN = value; }
            }

            [DataElement(AllowDbNull = true)]
            public string DE
            {
                get { return m_DE; }
                set { Dirty = true; m_DE = value; }
            }

            [DataElement(AllowDbNull = true)]
            public string FR
            {
                get { return m_FR; }
                set { Dirty = true; m_FR = value; }
            }

            [DataElement(AllowDbNull = true)]
            public string IT
            {
                get { return m_IT; }
                set { Dirty = true; m_IT = value; }
            }

            [DataElement(AllowDbNull = true)]
            public string CU
            {
                get { return m_CU; }
                set { Dirty = true; m_CU = value; }
            }

            [DataElement(AllowDbNull = true)]
            public string PackageID
            {
                get { return m_packageID; }
                set { Dirty = true; m_packageID = value; }
            }
        }
        #endregion DBLanguage table structure

        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Update()
        {
            log.Info("Updating the LanguageSystem table (this can take a few minutes)...");

            if (GameServer.Database.GetObjectCount<DbLanguageSystem>() < 1 && ServerProperties.Properties.USE_DBLANGUAGE)
            {
                var objs = GameServer.Database.SelectAllObjects<language>();
                if (objs.Count > 0)
                {
                    List<DbLanguageSystem> lngObjs = new List<DbLanguageSystem>();

                    foreach (language obj in objs)
                    {
                        if (string.IsNullOrEmpty(obj.TranslationID))
                            continue;

                        // This kind of row will later be readded by the LanguageMgr
                        // with it's updated values.
                        if (obj.TranslationID.Contains("System.LanguagesName."))
                            continue;

                        DbLanguageSystem lngObj = null;

                        if (!string.IsNullOrEmpty(obj.EN))
                        {
                            if (!ListContainsObjectData(lngObjs, "EN", obj.TranslationID)) // Ignore duplicates
                            {
                                lngObj = new DbLanguageSystem();
                                lngObj.TranslationId = obj.TranslationID;
                                lngObj.Language = "EN";
                                lngObj.Text = obj.EN;
                                lngObj.Tag = obj.PackageID;
                                lngObjs.Add(lngObj);
                            }
                        }

                        if (!string.IsNullOrEmpty(obj.DE))
                        {
                            if (!ListContainsObjectData(lngObjs, "DE", obj.TranslationID)) // Ignore duplicates
                            {
                                lngObj = new DbLanguageSystem();
                                lngObj.TranslationId = obj.TranslationID;
                                lngObj.Language = "DE";
                                lngObj.Text = obj.DE;
                                lngObj.Tag = obj.PackageID;
                                lngObjs.Add(lngObj);
                            }
                        }

                        if (!string.IsNullOrEmpty(obj.FR))
                        {
                            if (!ListContainsObjectData(lngObjs, "FR", obj.TranslationID)) // Ignore duplicates
                            {
                                lngObj = new DbLanguageSystem();
                                lngObj.TranslationId = obj.TranslationID;
                                lngObj.Language = "FR";
                                lngObj.Text = obj.FR;
                                lngObj.Tag = obj.PackageID;
                                lngObjs.Add(lngObj);
                            }
                        }

                        if (!string.IsNullOrEmpty(obj.IT))
                        {
                            if (!ListContainsObjectData(lngObjs, "IT", obj.TranslationID)) // Ignore duplicates
                            {
                                lngObj = new DbLanguageSystem();
                                lngObj.TranslationId = obj.TranslationID;
                                lngObj.Language = "IT";
                                lngObj.Text = obj.IT;
                                lngObj.Tag = obj.PackageID;
                                lngObjs.Add(lngObj);
                            }
                        }

                        // CU will be ignored!
                    }

                    foreach (DbLanguageSystem lngObj in lngObjs)
                    {
                        GameServer.Database.AddObject(lngObj);

                        if (log.IsWarnEnabled)
                            log.Warn("Moving sentence from 'language' to 'languagesystem'. ( Language <" + lngObj.Language +
                                     "> - TranslationId <" + lngObj.TranslationId + "> )");
                    }
                }
            }
        }

        private bool ListContainsObjectData(List<DbLanguageSystem> list, string language, string translationId)
        {
            bool contains = false;

            foreach (DbLanguageSystem lngObj in list)
            {
                if (lngObj.TranslationId != translationId)
                    continue;

                if (lngObj.Language != language)
                    continue;

                contains = true;
                break;
            }

            return contains;
        }
    }
}
