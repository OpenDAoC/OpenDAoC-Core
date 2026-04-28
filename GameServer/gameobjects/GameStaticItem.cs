using System.Collections;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.Language;

namespace DOL.GS
{
    public class GameStaticItem : GameObject, ITranslatableObject, IPooledList<GameStaticItem>
    {
        private int _emblem;
        protected ECSGameTimer _respawnTimer = null;
        protected readonly Lock _respawnTimerLock = new();
        protected DbWorldObject _dbWorldObject;

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

                if (ObjectState is eObjectState.Active)
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
            _dbWorldObject = obj as DbWorldObject;
            base.LoadFromDatabase(obj);
            LoadedFromScript = false;
            CurrentRegionID = _dbWorldObject.Region;
            TranslationId = _dbWorldObject.TranslationId;
            Name = _dbWorldObject.Name;
            ExamineArticle = _dbWorldObject.ExamineArticle;
            Model = _dbWorldObject.Model;
            Emblem = _dbWorldObject.Emblem;
            Realm = (eRealm) _dbWorldObject.Realm;
            Heading = _dbWorldObject.Heading;
            X = _dbWorldObject.X;
            Y = _dbWorldObject.Y;
            Z = _dbWorldObject.Z;
            RespawnInterval = _dbWorldObject.RespawnInterval;
        }

        public override void SaveIntoDatabase()
        {
            if (LoadedFromScript)
                return;

            _dbWorldObject ??= new();
            _dbWorldObject.ClassType = GetType().ToString();
            _dbWorldObject.TranslationId = TranslationId;
            _dbWorldObject.Name = Name;
            _dbWorldObject.ExamineArticle = ExamineArticle;
            _dbWorldObject.Model = Model;
            _dbWorldObject.Emblem = Emblem;
            _dbWorldObject.Realm = (byte) Realm;
            _dbWorldObject.Heading = Heading;
            _dbWorldObject.Region = CurrentRegionID;
            _dbWorldObject.X = X;
            _dbWorldObject.Y = Y;
            _dbWorldObject.Z = Z;
            _dbWorldObject.RespawnInterval = RespawnInterval;

            if (InternalID == null)
            {
                GameServer.Database.AddObject(_dbWorldObject);
                InternalID = _dbWorldObject.ObjectId;
            }
            else
                GameServer.Database.SaveObject(_dbWorldObject);
        }

        public override void Delete()
        {
            Notify(GameObjectEvent.Delete, this);
            RemoveFromWorld();
            ObjectState = eObjectState.Deleted;
        }

        public override void DeleteFromDatabase()
        {
            if (InternalID != null)
            {
                GameServer.Database.DeleteObject(_dbWorldObject);
                InternalID = null;
            }
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
            if (!base.RemoveFromWorld())
                return false;

            StartRespawn(RespawnInterval);
            return true;
        }

        public virtual void StartRespawn(int respawnSeconds)
        {
            if (respawnSeconds <= 0)
                return;

            lock (_respawnTimerLock)
            {
                if (_respawnTimer == null)
                    _respawnTimer = new(this, RespawnTimerCallback, respawnSeconds * 1000);
                else
                    _respawnTimer.Start(respawnSeconds * 1000);
            }
        }

        protected virtual int RespawnTimerCallback(ECSGameTimer respawnTimer)
        {
            AddToWorld();
            return 0;
        }
    }
}
