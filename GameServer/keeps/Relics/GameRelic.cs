using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Logging;

namespace DOL.GS
{
    public enum eRelicType : int
    {
        Invalid = -1,
        Strength = 0,
        Magic = 1
    }

    public class GameRelic : GameStaticItem
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string PLAYER_CARRY_RELIC_WEAK = "IAmCarryingARelic";
        private const int RELIC_EFFECT_INTERVAL = 4000;

        private GameInventoryItem _item;
        private ECSGameTimer _currentCarrierTimer;
        private DbRelic _dbRelic;
        private ECSGameTimer _returnRelicTimer;
        private long _timeRelicOnGround;
        private GameRelicPad _returnRelicPad;

        public DateTime LastCaptureDate { get; set; } = DateTime.Now;
        public eRelicType RelicType { get; private set; }
        public eRealm OriginalRealm { get; private set; }
        public eRealm LastRealm { get; private set; } = eRealm.None;
        public GameRelicPad CurrentRelicPad { get; private set; }
        public GamePlayer CurrentCarrier { get; private set; }
        public bool IsMounted => CurrentRelicPad != null;
        public static int ReturnRelicInterval => Properties.RELIC_RETURN_TIME * 1000;

        public GameRelic() : base()
        {
            m_saveInDB = true;
        }

        public GameRelic(DbRelic obj) : this()
        {
            LoadFromDatabase(obj);
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.TempProperties.GetProperty<GameRelic>(PLAYER_CARRY_RELIC_WEAK) != null)
            {
                player.Out.SendMessage("You are already carrying a relic.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (!player.IsAlive)
            {
                player.Out.SendMessage($"You cannot pickup {GetName(0, false)}. You are dead!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.IsStealthed)
            {
                player.Out.SendMessage("You cannot carry a relic while stealthed.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (IsMounted)
            {
                if (player.Realm == Realm)
                {
                    player.Out.SendMessage($"You cannot pickup {GetName(0, false)}. It is owned by your realm.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (!RelicMgr.CanPickupRelicFromShrine(player, this))
                {
                    player.Out.SendMessage($"You cannot pickup {GetName(0, false)}. You need to capture your realm's {Enum.GetName(RelicType)} relic first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if ((ePrivLevel)player.Client.Account.PrivLevel == ePrivLevel.Player)
            {
                if (IsMounted && CurrentRelicPad.GetEnemiesOnPad() < Properties.RELIC_PLAYERS_REQUIRED_ON_PAD)
                {
                    player.Out.SendMessage($"You must have {Properties.RELIC_PLAYERS_REQUIRED_ON_PAD} players nearby the pad before taking a relic.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            if (!player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, _item))
            {
                player.Out.SendMessage("You don't have enough space in your backpack to carry this.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            InventoryLogging.LogInventoryAction(this, player, eInventoryActionType.Other, _item.Template, _item.Count);
            CurrentCarrier = player;
            player.TempProperties.SetProperty(PLAYER_CARRY_RELIC_WEAK, this);
            player.Out.SendUpdateMaxSpeed();

            if (IsMounted)
            {
                CurrentRelicPad.RemoveRelic(this);
                _returnRelicPad = CurrentRelicPad;
                LastRealm = CurrentRelicPad.Realm;
                CurrentRelicPad = null;
            }

            RemoveFromWorld();
            SaveIntoDatabase();
            Realm = 0;
            SetHandlers(player, true);
            StartPlayerTimer(player);

            if (_returnRelicTimer != null)
            {
                _returnRelicTimer.Stop();
                _returnRelicTimer = null;
            }

            return true;
        }

        public virtual void RelicPadTakesOver(GameRelicPad pad, bool returning)
        {
            CurrentRelicPad = pad;
            Realm = pad.Realm;
            LastRealm = pad.Realm;
            pad.MountRelic(this, returning);
            CurrentRegionID = pad.CurrentRegionID;
            PlayerLoosesRelic(true);
            X = pad.X;
            Y = pad.Y;
            Z = pad.Z;
            Heading = pad.Heading;
            SaveIntoDatabase();
            AddToWorld();
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            IList messages = base.GetExamineMessages(player);
            messages.Add(IsMounted ? $"It is owned by {(player.Realm == Realm ? "your realm" : GlobalConstants.RealmToName(Realm))}." : "It is without owner, take it!");
            return messages;
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            InternalID = obj.ObjectId;
            _dbRelic = obj as DbRelic;
            CurrentRegionID = (ushort) _dbRelic.Region;
            X = _dbRelic.X;
            Y = _dbRelic.Y;
            Z = _dbRelic.Z;
            Heading = (ushort) _dbRelic.Heading;
            RelicType = (eRelicType) _dbRelic.relicType;
            Realm = (eRealm) _dbRelic.Realm;
            OriginalRealm = (eRealm) _dbRelic.OriginalRealm;
            LastRealm = (eRealm) _dbRelic.LastRealm;
            LastCaptureDate = _dbRelic.LastCaptureDate;
            Emblem = 0;
            Level = 99;

            MiniTemp template = GetRelicTemplate(OriginalRealm, RelicType);
            m_name = template.Name;
            m_model = template.Model;

            DbItemTemplate itemTemplate = new()
            {
                Name = Name,
                Object_Type = (int) eObjectType.Magical,
                Model = Model,
                IsDropable = true,
                IsPickable = false,
                Level = 99,
                Quality = 100,
                Price = 0,
                PackSize = 1,
                AllowAdd = false,
                Weight = 1000,
                Id_nb = "GameRelic",
                IsTradable = false,
                ClassType = "DOL.GS.GameInventoryRelic"
            };

            _item = GameInventoryItem.Create(itemTemplate);
        }

        public override void SaveIntoDatabase()
        {
            _dbRelic.Realm = (int) Realm;
            _dbRelic.OriginalRealm = (int) OriginalRealm;
            _dbRelic.LastRealm = (int) LastRealm;
            _dbRelic.Heading = Heading;
            _dbRelic.Region = CurrentRegionID;
            _dbRelic.relicType = (int) RelicType;
            _dbRelic.X = X;
            _dbRelic.Y = Y;
            _dbRelic.Z = Z;
            _dbRelic.LastCaptureDate = LastCaptureDate;

            if (InternalID == null)
            {
                GameServer.Database.AddObject(_dbRelic);
                InternalID = _dbRelic.ObjectId;
            }
            else
                GameServer.Database.SaveObject(_dbRelic);
        }

        protected virtual void Update()
        {
            if (_item == null || CurrentCarrier == null)
                return;

            CurrentRegionID = CurrentCarrier.CurrentRegionID;
            X = CurrentCarrier.X;
            Y = CurrentCarrier.Y;
            Z = CurrentCarrier.Z;
            Heading = CurrentCarrier.Heading;
        }

        protected virtual void PlayerLoosesRelic(bool removeFromInventory)
        {
            if (CurrentCarrier == null)
                return;

            GamePlayer player = CurrentCarrier;

            if (player.TempProperties.GetProperty<GameRelic>(PLAYER_CARRY_RELIC_WEAK) == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"GameRelic: {player.Name} has already lost {Name}");

                return;
            }

            if (removeFromInventory)
            {
                lock (player.Inventory.Lock)
                {
                    bool success = player.Inventory.RemoveItem(_item);
                    InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Other, _item.Template, _item.Count);

                    if (log.IsDebugEnabled)
                        log.Debug($"Remove {_item.Name} from {player.Name}'s Inventory {(success ? "successfully." : "with errors.")}");
                }
            }

            SetHandlers(player, false);
            StartPlayerTimer(null);
            player.TempProperties.RemoveProperty(PLAYER_CARRY_RELIC_WEAK);
            CurrentCarrier = null;
            player.Out.SendUpdateMaxSpeed();

            if (IsMounted == false)
            {
                _timeRelicOnGround = GameLoop.GameLoopTime;
                _returnRelicTimer = new(this, ReturnRelicTick, RELIC_EFFECT_INTERVAL);

                if (log.IsDebugEnabled)
                    log.Debug($"{Name} dropped, return timer for relic set to {ReturnRelicInterval / 1000} seconds.");

                Update();
                SaveIntoDatabase();
                AddToWorld();
            }
        }

        protected virtual int ReturnRelicTick(ECSGameTimer timer)
        {
            if (GameLoop.GameLoopTime - _timeRelicOnGround < ReturnRelicInterval)
            {
                ushort effectID = (ushort) Util.Random(5811, 5815);

                foreach (GamePlayer ppl in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    ppl.Out.SendSpellEffectAnimation(this, this, effectID, 0, false, 0x01);

                return RELIC_EFFECT_INTERVAL;
            }

            if (_returnRelicPad != null)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Relic {Name} is lost and returns to {_returnRelicPad}");

                RemoveFromWorld();
                RelicPadTakesOver(_returnRelicPad, true);
                SaveIntoDatabase();
                AddToWorld();
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error($"Relic {Name} is lost and ReturnRelicPad is null!");
            }

            _returnRelicTimer.Stop();
            _returnRelicTimer = null;
            return 0;
        }

        protected virtual void StartPlayerTimer(GamePlayer player)
        {
            if (player != null)
            {
                if (_currentCarrierTimer != null)
                {
                    if (log.IsWarnEnabled)
                        log.Warn("PlayerTimer already set on a player, stopping timer!");

                    _currentCarrierTimer.Stop();
                    _currentCarrierTimer = null;
                }
                
                _currentCarrierTimer = new(player, CarrierTimerTick);
                _currentCarrierTimer.Start(RELIC_EFFECT_INTERVAL);
            }
            else
            {
                if (_currentCarrierTimer != null)
                {
                    _currentCarrierTimer.Stop();
                    _currentCarrierTimer = null;
                }
            }
        }

        private int CarrierTimerTick(ECSGameTimer timer)
        {
            Update();

            // Disabled for OF.
            /*if (!GameServer.KeepManager.FrontierRegionsList.Contains(CurrentRegionID) == false)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"{Name} taken out of frontiers, relic returned to previous pad.");

                RelicPadTakesOver(_returnRelicPad, true);
                SaveIntoDatabase();
                AddToWorld();
                return 0;
            }*/

            if (CurrentCarrier != null && CurrentCarrier.Inventory.GetFirstItemByID(_item.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) == null)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"{Name} not found in carriers backpack, relic returned to previous pad.");

                RelicPadTakesOver(_returnRelicPad, true);
                SaveIntoDatabase();
                AddToWorld();
                return 0;
            }

            ushort effectID = (ushort) Util.Random(5811, 5815);

            foreach (GamePlayer player in CurrentCarrier.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(CurrentCarrier, CurrentCarrier, effectID, 0, false, 0x01);

            return RELIC_EFFECT_INTERVAL;
        }

        protected virtual void SetHandlers(GamePlayer player, bool activate)
        {
            if (activate)
            {
                GameEventMgr.AddHandler(player, GamePlayerEvent.Quit, new(PlayerAbsence));
                GameEventMgr.AddHandler(player, GameLivingEvent.Dying, new(PlayerAbsence));
                GameEventMgr.AddHandler(player, GamePlayerEvent.StealthStateChanged, new(PlayerAbsence));
                GameEventMgr.AddHandler(player, GamePlayerEvent.Linkdeath, new(PlayerAbsence));
                GameEventMgr.AddHandler(player, PlayerInventoryEvent.ItemDropped, new(PlayerAbsence));
            }
            else
            {
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.Quit, new(PlayerAbsence));
                GameEventMgr.RemoveHandler(player, GameLivingEvent.Dying, new(PlayerAbsence));
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.StealthStateChanged, new(PlayerAbsence));
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.Linkdeath, new(PlayerAbsence));
                GameEventMgr.RemoveHandler(player, PlayerInventoryEvent.ItemDropped, new(PlayerAbsence));
            }
        }

        protected void PlayerAbsence(DOLEvent e, object sender, EventArgs args)
        {
            Realm = 0;

            if (e == PlayerInventoryEvent.ItemDropped)
            {
                ItemDroppedEventArgs idArgs = args as ItemDroppedEventArgs;

                if (idArgs.SourceItem.Name != _item.Name)
                    return;

                idArgs.GroundItem.RemoveFromWorld();
                PlayerLoosesRelic(false);
                return;
            }

            PlayerLoosesRelic(true);
        }

        public static bool IsPlayerCarryingRelic(GamePlayer player)
        {
            return player.TempProperties.GetProperty<GameRelic>(PLAYER_CARRY_RELIC_WEAK) != null;
        }

        public static MiniTemp GetRelicTemplate(eRealm realm, eRelicType relicType)
        {
            MiniTemp template = new();

            switch (realm)
            {
                case eRealm.Albion:
                {
                    if (relicType is eRelicType.Magic)
                    {
                        template.Name = "Merlin's Staff";
                        template.Model = 630;
                    }
                    else
                    {
                        template.Name = "Scabbard of Excalibur";
                        template.Model = 631;
                    }

                    break;
                }
                case eRealm.Midgard:
                {
                    if (relicType is eRelicType.Magic)
                    {
                        template.Name = "Horn of Valhalla";
                        template.Model = 635;
                    }
                    else
                    {
                        template.Name = "Thor's Hammer";
                        template.Model = 634;
                    }

                    break;
                }
                case eRealm.Hibernia:
                {
                    if (relicType is eRelicType.Magic)
                    {
                        template.Name = "Cauldron of Dagda";
                        template.Model = 632;
                    }
                    else
                    {
                        template.Name = "Lug's Spear of Lightning";
                        template.Model = 633;
                    }

                    break;
                }
                default:
                {
                    template.Name = "Unknown Relic";
                    template.Model = 633;
                    break;
                }
            }

            return template;
        }

        public class MiniTemp
        {
            public string Name;
            public ushort Model;
        }
    }
}
