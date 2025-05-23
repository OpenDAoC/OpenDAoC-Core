using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Quests
{
    /// <summary>
    /// Declares a Craft task.
    /// craft Item for NPC
    /// </summary>
    public class CraftTask : AbstractTask
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string RECIEVER_ZONE = "receiverZone";

        //public string ItemName;
        public const double RewardMoneyRatio = 1.25;
        /// <summary>
        /// Constructs a new Task
        /// </summary>
        /// <param name="taskPlayer">The player doing this task</param>
        public CraftTask(GamePlayer taskPlayer)
            : base(taskPlayer)
        {
        }

        /// <summary>
        /// Constructs a new Task from a database Object
        /// </summary>
        /// <param name="taskPlayer">The player doing the task</param>
        /// <param name="dbTask">The database object</param>
        public CraftTask(GamePlayer taskPlayer, DbTask dbTask)
            : base(taskPlayer, dbTask)
        {
        }

        private long m_rewardmoney = 0;
        public override long RewardMoney
        {
            get
            {
                return m_rewardmoney;
            }
        }

        public void SetRewardMoney(long money)
        {
            m_rewardmoney = money;
        }

        public override IList RewardItems
        {
            get { return null; }
        }

        /// <summary>
        /// Retrieves the name of the task
        /// </summary>
        public override string Name
        {
            get { return "Craft Task"; }
        }

        /// <summary>
        /// Retrieves the description
        /// </summary>
        public override string Description
        {
            get { return "Craft the " + ItemName + " for " + ReceiverName + " in " + ReceiverZone; }
        }


        /// <summary>
        /// Zone related to task stored in dbTask
        /// </summary>
        public virtual String ReceiverZone
        {
            get { return GetCustomProperty(RECIEVER_ZONE); }
            set { SetCustomProperty(RECIEVER_ZONE, value); }
        }

        /// <summary>
        /// Called to finish the task.
        /// Should be overridden and some rewards given etc.
        /// </summary>
        public override void FinishTask()
        {
            base.FinishTask();
        }

        /// <summary>
        /// This method needs to be implemented in each task.
        /// It is the core of the task. The global event hook of the GamePlayer.
        /// This method will be called whenever a GamePlayer with this task
        /// fires ANY event!
        /// </summary>
        /// <param name="e">The event type</param>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">The event arguments</param>
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            // Filter only the events from task owner
            if (sender != m_taskPlayer)
                return;

            if (CheckTaskExpired())
            {
                return;
            }

            GamePlayer player = (GamePlayer)sender;

            if (e == GamePlayerEvent.GiveItem)
            {
                GiveItemEventArgs gArgs = (GiveItemEventArgs)args;
                GameLiving target = gArgs.Target as GameLiving;
                DbInventoryItem item = gArgs.Item;

                if (player.GameTask.ReceiverName == target.Name && item.Name == player.GameTask.ItemName)
                {
                    player.Inventory.RemoveItem(item);
                    InventoryLogging.LogInventoryAction(player, target, eInventoryActionType.Quest, item.Template, item.Count);
                    FinishTask();
                }
            }
        }

        /// <summary>
        /// Generate an Item random Named for NPC Drop
        /// </summary>
        /// <param name="player">Level of Generated Item</param>
        /// <returns>A Generated NPC Item</returns>
        public static DbItemTemplate GenerateNPCItem(GamePlayer player)
        {
            int mediumCraftingLevel = player.GetCraftingSkillValue(player.CraftingPrimarySkill) + 20;
            int lowLevel = mediumCraftingLevel - 20;
            int highLevel = mediumCraftingLevel + 20;

            var craftitem = DOLDB<DbCraftedItem>.SelectObjects(DB.Column("CraftingSkillType").IsEqualTo((int)player.CraftingPrimarySkill)
                .And(DB.Column("CraftingLevel").IsGreaterThan(lowLevel).And(DB.Column("CraftingLevel").IsLessThan(highLevel))));
            int craftrnd = Util.Random(craftitem.Count);

            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(craftitem[craftrnd].Id_nb);
            return template;
        }

        /// <summary>
        /// Create an Item, Search for a NPC to consign the Item and give Item to the Player
        /// </summary>
        /// <param name="player">The GamePlayer Object</param>
        /// <param name="source">The source of the task</param>
        public static bool BuildTask(GamePlayer player, GameLiving source)
        {
            if (source == null)
                return false;

            GameNPC NPC = GetRandomNPC(player);
            if (NPC == null)
            {
                player.Out.SendMessage("I have no task for you, come back some time later.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return false;
            }

            DbItemTemplate taskItem = GenerateNPCItem(player);

            if (taskItem == null)
            {
                player.Out.SendMessage("I can't think of anything for you to make, perhaps you should ask again.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                log.ErrorFormat("Craft task item is null for player {0} at level {1}.", player.Name, player.Level);
                return false;
            }

            var craftTask = new CraftTask(player)
                                {
                                    TimeOut = DateTime.Now.AddHours(2),
                                    ItemName = taskItem.Name,
                                    ReceiverName = NPC.Name,
                                    ReceiverZone = NPC.CurrentZone.Description
                                };

            craftTask.SetRewardMoney((long)(taskItem.Price * RewardMoneyRatio));

            player.GameTask = craftTask;

            player.Out.SendMessage("Craft " + taskItem.GetName(0, false) + " for " + NPC.Name + " in " + NPC.CurrentZone.Description, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            return true;

        }

        /// <summary>
        /// Find a Random NPC
        /// </summary>
        /// <param name="Player">The GamePlayer Object</param>		
        /// <returns>The GameNPC Searched</returns>
        public static GameNPC GetRandomNPC(GamePlayer Player)
        {
            return Player.CurrentZone.GetRandomNPC(new eRealm[] { eRealm.Albion, eRealm.Hibernia, eRealm.Midgard });
        }

        public new static bool CheckAvailability(GamePlayer player, GameLiving target)
        {
            if (target == null)
                return false;

            if (target is CraftNPC)
            {
                if (((target as CraftNPC).TheCraftingSkill == player.CraftingPrimarySkill))
                    return AbstractTask.CheckAvailability(player, target, CHANCE);
            }
            return false;//else return false
        }
    }
}
