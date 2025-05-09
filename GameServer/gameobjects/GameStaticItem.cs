using System;
using System.Collections;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.Language;

namespace DOL.GS
{
    public class GameStaticItem : GameObject, ITranslatableObject
    {
        private int _emblem;
        protected ECSGameTimer _respawnTimer = null;
        protected readonly Lock _respawnTimerLock = new();

        public int RespawnInterval { get; set; }
        public string TranslationId { get; set; }
        private string ExamineArticle { get; set; } // Unused?
        public bool LoadedFromScript { get; set; } = true;
        public override eGameObjectType GameObjectType => eGameObjectType.ITEM;

        public override ushort Model
        {
            get => base.Model;
            set
            {
                base.Model = value;

                if (ObjectState is eObjectState.Active)
                    ClientService.CreateObjectForPlayers(this);
            }
        }

        public virtual int Emblem
        {
            get => _emblem;
            set
            {
                _emblem = value;

                if (ObjectState is eObjectState.Active)
                    ClientService.CreateObjectForPlayers(this);
            }
        }

        public virtual LanguageDataObject.eTranslationIdentifier TranslationIdentifier => LanguageDataObject.eTranslationIdentifier.eObject;

        public override string Name
        {
            get => base.Name;
            set
            {
                base.Name = value;

                if (ObjectState is eObjectState.Active)
                    ClientService.CreateObjectForPlayers(this);
            }
        }

        public GameStaticItem() : base() { }

        public override ushort Heading
        {
            get => base.Heading;
            set
            {
                base.Heading = value;

                if (ObjectState is eObjectState.Active)
                    ClientService.CreateObjectForPlayers(this);
            }
        }

        public override byte Level
        {
            get => base.Level;
            set
            {
                base.Level = value;

                if (ObjectState == eObjectState.Active)
                    ClientService.CreateObjectForPlayers(this);
            }
        }

        public override string GetName(int article, bool firstLetterUppercase)
        {
            if (string.IsNullOrEmpty(Name))
                return string.Empty;

            if (!char.IsUpper(Name[0]))
                return base.GetName(article, firstLetterUppercase);

            if (firstLetterUppercase)
                return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameStaticItem.GetName.Article1", Name);
            else
                return LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameStaticItem.GetName.Article2", Name);
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            IList list = base.GetExamineMessages(player);
            list.Insert(0, $"You select {GetName(0, false)}.");
            return list;
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            DbWorldObject item = obj as DbWorldObject;
            base.LoadFromDatabase(obj);
            LoadedFromScript = false;
            CurrentRegionID = item.Region;
            TranslationId = item.TranslationId;
            Name = item.Name;
            ExamineArticle = item.ExamineArticle;
            Model = item.Model;
            Emblem = item.Emblem;
            Realm = (eRealm) item.Realm;
            Heading = item.Heading;
            X = item.X;
            Y = item.Y;
            Z = item.Z;
            RespawnInterval = item.RespawnInterval;
        }

        public override void SaveIntoDatabase()
        {
            DbWorldObject obj = null;

            if (InternalID != null)
                obj = GameServer.Database.FindObjectByKey<DbWorldObject>(InternalID);

            if (obj == null)
            {
                if (LoadedFromScript == false)
                    obj = new DbWorldObject();
                else
                    return;
            }

            obj.TranslationId = TranslationId;
            obj.Name = Name;
            obj.ExamineArticle = ExamineArticle;
            obj.Model = Model;
            obj.Emblem = Emblem;
            obj.Realm = (byte)Realm;
            obj.Heading = Heading;
            obj.Region = CurrentRegionID;
            obj.X = X;
            obj.Y = Y;
            obj.Z = Z;
            obj.ClassType = this.GetType().ToString();
            obj.RespawnInterval = RespawnInterval;

            if (InternalID == null)
            {
                GameServer.Database.AddObject(obj);
                InternalID = obj.ObjectId;
            }
            else
                GameServer.Database.SaveObject(obj);
        }

        public override void Delete()
        {
            Notify(GameObjectEvent.Delete, this);
            RemoveFromWorld(0);
            ObjectState = eObjectState.Deleted;
        }

        public override void DeleteFromDatabase()
        {
            if(InternalID != null)
            {
                DbWorldObject obj = GameServer.Database.FindObjectByKey<DbWorldObject>(InternalID);

                if (obj != null)
                  GameServer.Database.DeleteObject(obj);
            }

            InternalID = null;
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            ClientService.CreateObjectForPlayers(this);
            return true;
        }

        public override bool RemoveFromWorld()
        {
            return RemoveFromWorld(RespawnInterval);
        }

        public virtual bool RemoveFromWorld(int respawnSeconds)
        {
            if (base.RemoveFromWorld())
            {
                if (respawnSeconds > 0)
                    StartRespawn(Math.Max(1, respawnSeconds));

                return true;
            }

            return false;
        }

        protected virtual void StartRespawn(int respawnSeconds)
        {
            lock (_respawnTimerLock)
            {
                if (_respawnTimer == null)
                {
                    _respawnTimer = new ECSGameTimer(this);
                    _respawnTimer.Callback = new ECSGameTimer.ECSTimerCallback(RespawnTimerCallback);
                    _respawnTimer.Start(respawnSeconds * 1000);
                }
            }
        }

        protected virtual int RespawnTimerCallback(ECSGameTimer respawnTimer)
        {
            lock (_respawnTimerLock)
            {
                if (_respawnTimer != null)
                {
                    _respawnTimer.Stop();
                    _respawnTimer = null;
                    AddToWorld();
                }
            }

            return 0;
        }
    }
}
