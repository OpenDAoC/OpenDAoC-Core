using System;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class WorldInventoryItem : GameStaticItemTimed
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ECSGameTimer _pickupTimer;

        public int GetPickupTime => _pickupTimer == null ? 0 : _pickupTimer.TimeUntilElapsed;
        public DbInventoryItem Item { get; private set; }
        public override LanguageDataObject.eTranslationIdentifier TranslationIdentifier => LanguageDataObject.eTranslationIdentifier.eItem;
        public virtual bool IsPlayerDiscarded => false;

        public WorldInventoryItem() : base() { }

        public WorldInventoryItem(DbInventoryItem item) : this()
        {
            Item = item;
            Item.SlotPosition = 0;
            Item.OwnerID = null;
            Item.AllowAdd = true;
            Level = (byte) item.Level;
            Model = (ushort) item.Model;
            Emblem = item.Emblem;
            Name = item.Name;
        }

        public static WorldInventoryItem CreateFromTemplate(DbInventoryItem item)
        {
            return item.Template is DbItemUnique ? null : CreateFromTemplate(item.Id_nb);
        }

        public static WorldInventoryItem CreateFromTemplate(string templateID)
        {
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateID);

            if (template == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Template not found (ID: {templateID}){Environment.NewLine}{Environment.StackTrace}");

                return null;
            }

            return CreateFromTemplate(template);
        }

        public static WorldInventoryItem CreateUniqueFromTemplate(string templateID)
        {
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(templateID);

            if (template == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Template not found (ID: {templateID}){Environment.NewLine}{Environment.StackTrace}");

                return null;
            }
            
            return CreateUniqueFromTemplate(template);
        }

        public static WorldInventoryItem CreateFromTemplate(DbItemTemplate template)
        {
            if (template == null)
                return null;

            WorldInventoryItem invItem = new();
            invItem.Item = GameInventoryItem.Create(template);
            invItem.Item.SlotPosition = 0;
            invItem.Item.OwnerID = null;
            invItem.Level = (byte)template.Level;
            invItem.Model = (ushort)template.Model;
            invItem.Emblem = template.Emblem;
            invItem.Name = template.Name;
            return invItem;
        }

        public static WorldInventoryItem CreateUniqueFromTemplate(DbItemTemplate template)
        {
            if (template == null)
                return null;

            WorldInventoryItem invItem = new();
            DbItemUnique item = new(template);
            invItem.Item = GameInventoryItem.Create(item);
            invItem.Item.SlotPosition = 0;
            invItem.Item.OwnerID = null;
            invItem.Level = (byte)template.Level;
            invItem.Model = (ushort)template.Model;
            invItem.Emblem = template.Emblem;
            invItem.Name = template.Name;
            return invItem;
        }

        public override bool RemoveFromWorld()
        {
            if (base.RemoveFromWorld())
            {
                (Item as IGameInventoryItem)?.OnRemoveFromWorld();
                return true;
            }

            return false;
        }

        public void StartPickupTimer(int time)
        {
            if (_pickupTimer != null)
            {
                _pickupTimer.Stop();
                _pickupTimer = null;
            }

            _pickupTimer = new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(CallBack), time * 1000);
        }

        private int CallBack(ECSGameTimer timer)
        {
            _pickupTimer.Stop();
            _pickupTimer = null;
            return 0;
        }

        public void StopPickupTimer()
        {
            foreach (GamePlayer player in Owners.OfType<GamePlayer>())
            {
                if (player.ObjectState is eObjectState.Active)
                    player.Out.SendMessage($"You may now pick up {Name}!", eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
            }

            _pickupTimer.Stop();
            _pickupTimer = null;
        }

        public override bool TryAutoPickUp(IGameStaticItemOwner itemOwner)
        {
            lock (_pickUpLock)
            {
                return ObjectState is not eObjectState.Deleted && itemOwner.TryAutoPickUpItem(this);
            }
        }

        public override TryPickUpResult TryPickUp(GamePlayer source, IGameStaticItemOwner itemOwner)
        {
            lock (_pickUpLock)
            {
                return ObjectState is not eObjectState.Deleted ? itemOwner.TryPickUpItem(source, this) : TryPickUpResult.FAILED;
            }
        }
    }

    public class PlayerDiscardedWorldInventoryItem : WorldInventoryItem
    {
        public override bool IsPlayerDiscarded => true;

        public PlayerDiscardedWorldInventoryItem(DbInventoryItem item) : base(item) { }
    }
}
