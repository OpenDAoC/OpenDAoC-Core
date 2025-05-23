using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Quests
{
    /// <summary>
    /// Declares the abstract quest class from which all user created
    /// quests must derive!
    /// </summary>
    public class AbstractTask
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The temp property name for next check task millisecond
        /// </summary>
        protected const string CHECK_TASK_TICK = "checkTaskTick";

        /// <summary>
        /// Time player must wait after failed task check to get new chance for a task, in milliseconds
        /// </summary>
        protected static int
            CHECK_TASK_DELAY =
                ServerProperties.Properties.TASK_PAUSE_TICKS; // moved to server properties to avoid tasks overruns...

        /// <summary>
        /// Chance of npc having task for player
        /// </summary>
        protected const int CHANCE = 50;

        protected const int MAX_LEVEL = 20;

        // allowed number of tasks per level
        private static readonly int[] m_maxTasksDone = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20];

        protected const string RECEIVER_NAME = "receiverName";
        protected const string ITEM_NAME = "itemName";

        /// <summary>
        /// The player doing the quest
        /// </summary>
        protected GamePlayer m_taskPlayer = null;

        /// <summary>
        /// The quest database object, storing the information for the player
        /// and the quest. Eg. QuestStep etc.
        /// </summary>
        private DbTask m_dbTask = null;

        /// <summary>
        /// Constructs a new Quest
        /// </summary>
        /// <param name="taskPlayer">The player doing this task</param>
        public AbstractTask(GamePlayer taskPlayer)
        {
            m_taskPlayer = taskPlayer;

            DbTask dbTask = null;

            // Check if player already has a task
            // if yes reuse dbtask object to keep TasksDone from old dbtask object.
            if (taskPlayer.GameTask != null)
            {
                dbTask = taskPlayer.GameTask.m_dbTask;
            }
            else // if player has no active task, load dbtask an use tasksdone
            {
                // Load Task object of player ...
                var tasks = GameServer.Database.FindObjectByKey<DbTask>(taskPlayer.QuestPlayerID);
                if (tasks != null)
                    dbTask = tasks;
            }

            // this should happen only if player never did a task and has no entry in DBTask.
            if (dbTask == null)
            {
                dbTask = new DbTask();
                dbTask.Character_ID = taskPlayer.QuestPlayerID;
            }

            dbTask.TaskType = GetType().FullName;

            m_dbTask = dbTask;

            ParseCustomProperties();
            SaveIntoDatabase();
        }

        /// <summary>
        /// Constructs a new Quest from a database Object
        /// </summary>
        /// <param name="taskPlayer">The player doing the quest</param>
        /// <param name="dbTask">The database object</param>
        public AbstractTask(GamePlayer taskPlayer, DbTask dbTask)
        {
            m_taskPlayer = taskPlayer;
            m_dbTask = dbTask;
            ParseCustomProperties();
            SaveIntoDatabase();
        }

        /// <summary>
        /// Task already finished or still active.
        /// </summary>
        public bool TaskActive
        {
            get
            {
                return !string.IsNullOrEmpty(m_dbTask.TaskType) &&
                       m_dbTask.TaskType != typeof(AbstractTask).ToString();
            }
        }

        public DateTime TimeOut
        {
            get { return m_dbTask.TimeOut; }
            set { m_dbTask.TimeOut = value; }
        }

        public int TasksDone
        {
            get { return m_dbTask.TasksDone; }
            set { m_dbTask.TasksDone = value; }
        }

        // Characters under level 20 can do the same number of tasks as their level.
        // (eg: Level two can do two tasks) The total xp from these Tasks will be 50% of
        // the total xp required for that level.
        public virtual long RewardXP
        {
            get
            {
                long XPNeeded = m_taskPlayer.GetExperienceNeededForLevel((byte) (m_taskPlayer.Level));
                return (long) (XPNeeded * 0.50 / (m_taskPlayer.Level - 1)); // 50% of total xp for level
            }
        }

        public virtual long RewardMoney
        {
            get { return 0; }
        }

        public virtual IList RewardItems
        {
            get { return null; }
        }

        public virtual String ReceiverName
        {
            get { return GetCustomProperty(RECEIVER_NAME); }
            set { SetCustomProperty(RECEIVER_NAME, value); }
        }

        /// <summary>
        /// Item related to task stored in dbTask
        /// </summary>
        public virtual String ItemName
        {
            get { return GetCustomProperty(ITEM_NAME); }
            set { SetCustomProperty(ITEM_NAME, value); }
        }

        /// <summary>
        /// Loads a quest from the databaseobject and assigns it to a player
        /// </summary>
        /// <param name="targetPlayer">Player to assign the loaded quest</param>
        /// <param name="dbTask">Quest to load</param>
        /// <returns>The created quest</returns>
        public static AbstractTask LoadFromDatabase(GamePlayer targetPlayer, DbTask dbTask)
        {
            // if we have a active task load it, else the taksdone will be updated on creation of first task instance in AbstractTask(GamePlayer) constructor
            if (!string.IsNullOrEmpty(dbTask.TaskType))
            {
                Type taskType = null;
                foreach (Assembly asm in ScriptMgr.Scripts)
                {
                    taskType = asm.GetType(dbTask.TaskType);
                    if (taskType != null)
                        break;
                }

                if (taskType == null)
                    taskType = Assembly.GetAssembly(typeof(GameServer)).GetType(dbTask.TaskType);
                if (taskType == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Could not find task: " + dbTask.TaskType + "!!!");
                    return null;
                }

                return (AbstractTask) Activator.CreateInstance(taskType, new object[] {targetPlayer, dbTask});
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Saves this quest into the database
        /// </summary>
        public virtual void SaveIntoDatabase()
        {
            if (m_dbTask.IsPersisted)
                GameServer.Database.SaveObject(m_dbTask);
            else
                GameServer.Database.AddObject(m_dbTask);
        }

        /// <summary>
        /// Deletes this quest from the database
        /// </summary>
        public virtual void DeleteFromDatabase()
        {
            if (!m_dbTask.IsPersisted) return;

            DbTask dbTask = (DbTask) GameServer.Database.FindObjectByKey<DbTask>(m_dbTask.ObjectId);
            if (dbTask != null)
                GameServer.Database.DeleteObject(dbTask);
        }

        /// <summary>
        /// Retrieves the name of the quest
        /// </summary>
        public virtual string Name
        {
            get { return "TASK NAME UNDEFINED!"; }
        }

        /// <summary>
        /// Retrieves the description for the current quest step
        /// </summary>
        public virtual string Description
        {
            get { return "TASK DESCRIPTION UNDEFINED!"; }
        }

        /// <summary>
        /// This HybridDictionary holds all the custom properties of this quest
        /// </summary>
        protected HybridDictionary m_customProperties = new HybridDictionary();

        /// <summary>
        /// This method parses the custom properties string of the m_dbQuest
        /// into the HybridDictionary for easier use and access
        /// </summary>
        public void ParseCustomProperties()
        {
            if (m_dbTask.CustomPropertiesString == null)
                return;

            lock (m_customProperties)
            {
                m_customProperties.Clear();
                foreach (string property in Util.SplitCSV(m_dbTask.CustomPropertiesString))
                    if (property.Length > 0)
                    {
                        string[] values = property.Split('=');
                        m_customProperties[values[0]] = values[1];
                    }
            }
        }

        /// <summary>
        /// This method sets a custom Property to a specific value
        /// </summary>
        /// <param name="key">The name of the property</param>
        /// <param name="value">The value of the property</param>
        public void SetCustomProperty(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            //Make the string safe
            key = key.Replace(';', ',');
            key = key.Replace('=', '-');
            value = value.Replace(';', ',');
            value = value.Replace('=', '-');
            lock (m_customProperties)
            {
                m_customProperties[key] = value;
            }

            SaveCustomProperties();
        }

        /// <summary>
        /// Saves the custom properties into the database
        /// </summary>
        protected void SaveCustomProperties()
        {
            StringBuilder builder = new StringBuilder();
            lock (m_customProperties)
            {
                foreach (string hKey in m_customProperties.Keys)
                {
                    builder.Append(hKey);
                    builder.Append('=');
                    builder.Append(m_customProperties[hKey]);
                    builder.Append(';');
                }
            }

            m_dbTask.CustomPropertiesString = builder.ToString();
            SaveIntoDatabase();
        }

        /// <summary>
        /// Removes a custom property from the database
        /// </summary>
        /// <param name="key">The key name of the property</param>
        public void RemoveCustomProperty(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (m_customProperties)
            {
                m_customProperties.Remove(key);
            }

            SaveCustomProperties();
        }

        /// <summary>
        /// This method retrieves a custom property from the database
        /// </summary>
        /// <param name="key">The property key</param>
        /// <returns>The property value</returns>
        public string GetCustomProperty(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return (string) m_customProperties[key];
        }


        /// <summary>
        /// Called to finish the task.
        /// </summary>
        public virtual void FinishTask()
        {
            if (RewardXP > 0)
                m_taskPlayer.GainExperience(eXPSource.Task, RewardXP);

            if (RewardMoney > 0)
            {
                m_taskPlayer.AddMoney(RewardMoney, "You receive {0} for completing your task.");
                InventoryLogging.LogInventoryAction("(TASK;" + m_dbTask.TaskType + ")", m_taskPlayer,
                    eInventoryActionType.Quest, RewardMoney);
            }

            if (RewardItems != null && RewardItems.Count > 0)
            {
                m_taskPlayer.Inventory.BeginChanges();
                foreach (DbInventoryItem item in RewardItems)
                {
                    if (m_taskPlayer.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item))
                        InventoryLogging.LogInventoryAction("(TASK;" + m_dbTask.TaskType + ")", m_taskPlayer,
                            eInventoryActionType.Quest, item.Template, item.Count);
                }

                m_taskPlayer.Inventory.CommitChanges();
            }

            m_taskPlayer.Out.SendMessage("You finish the " + Name + "!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            m_dbTask.TaskType = typeof(AbstractTask).ToString();
            m_dbTask.CustomPropertiesString = null;
            lock (m_customProperties)
            {
                m_customProperties.Clear();
            }

            m_dbTask.TasksDone += 1;

            SaveIntoDatabase();
        }

        /// <summary>
        /// Called to abort the task
        /// </summary>
        public virtual void ExpireTask()
        {
            if (ItemName != null)
            {
                lock (m_taskPlayer.Inventory.Lock)
                {
                    DbInventoryItem item = m_taskPlayer.Inventory.GetFirstItemByID(ItemName, eInventorySlot.Min_Inv,
                        eInventorySlot.Max_Inv);
                    if (item != null)
                    {
                        m_taskPlayer.Inventory.RemoveItem(item);
                        InventoryLogging.LogInventoryAction(m_taskPlayer, "(TASK;" + m_dbTask.TaskType + ")",
                            eInventoryActionType.Quest, item.Template, item.Count);
                    }
                }

                m_taskPlayer.Out.SendMessage("Your task related item has been removed from your inventory.",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            m_taskPlayer.Out.SendMessage("Your " + Name + " has expired!", eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            m_dbTask.TaskType = typeof(AbstractTask).ToString();
            m_dbTask.CustomPropertiesString = null;
            m_customProperties.Clear();
            SaveIntoDatabase();
        }

        /// <summary>
        /// This method needs to be implemented in each quest.
        /// It is the core of the quest. The global event hook of the GamePlayer.
        /// This method will be called whenever a GamePlayer with this quest
        /// fires ANY event!
        /// </summary>
        /// <param name="e">The event type</param>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">The event arguments</param>
        public virtual void Notify(DOLEvent e, object sender, EventArgs args)
        {
        }

        /// <summary>
        /// Check Task available
        /// </summary>
        public bool CheckTaskExpired()
        {
            if (TaskActive && (DateTime.Compare(TimeOut, DateTime.Now) < 0 || m_taskPlayer.Level > MAX_LEVEL))
            {
                ExpireTask();
                return true;
            }

            return false;
        }

        public static int MaxTasksDone(int level)
        {
            return level > MAX_LEVEL ? 0 : m_maxTasksDone[Math.Min(level - 1, m_maxTasksDone.Length)];
        }

        /// <summary>
        /// Create an InventoryItem of given Name and Level
        /// </summary>
        /// <param name="ItemName">Name for the object</param>
        /// <param name="ItemLevel">Level to give to the object</param>
        /// <param name="Model">Model for the object</param>
        /// <returns>InventoryItem of given Name and Level</returns>
        public static DbInventoryItem GenerateItem(string ItemName, int ItemLevel, int Model)
        {
            DbItemUnique TaskItems = new DbItemUnique();
            TaskItems.Name = ItemName;
            TaskItems.Level = ItemLevel;
            TaskItems.DPS_AF = 0;
            TaskItems.SPD_ABS = 0;
            TaskItems.Hand = 0;
            //TaskItems.Type_Damage = 0;
            TaskItems.Object_Type = 0;
            TaskItems.Item_Type = 1;
            TaskItems.Weight = 1;
            TaskItems.Model = Model;
            TaskItems.Price = 0;
            TaskItems.Color = 0;
            TaskItems.IsDropable = true;
            TaskItems.IsPickable = true;
            //TaskItems.IsStackable = false;
            TaskItems.Quality = 80 + ItemLevel;
            TaskItems.MaxCondition = 90;
            TaskItems.MaxDurability = 1000;
            DbInventoryItem InvTaskItems = GameInventoryItem.Create(TaskItems);
            return InvTaskItems;
        }

        public static bool CheckAvailability(GamePlayer player, GameLiving target)
        {
            return CheckAvailability(player, target, CHANCE);
        }

        /// <summary>
        /// Check if Player can accept a new Task
        /// </summary>
        /// <param name="player">The GamePlayer Object</param>
        /// <param name="target">The target</param>
        /// <param name="chanceOfSuccess">The chance of success</param>
        /// <returns>True Player have no other Chart</returns>
        protected static bool CheckAvailability(GamePlayer player, GameLiving target, int chanceOfSuccess)
        {
            if (target == null)
                return false;

            if (target.Realm == 0)
                return false;

            if (GameServer.ServerRules.IsAllowedToUnderstand(target, player) == false)
                return false;

            if (player.Level > MAX_LEVEL)
            {
                player.Out.SendMessage($"Tasks are only available to player up to level {MAX_LEVEL}!", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.GameTask is {TaskActive: true})
            {
                player.Out.SendMessage("You already have a Task. Select yourself and type /Task for more Information.",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.GameTask != null && player.GameTask.TasksDone >= MaxTasksDone(player.Level))
            {
                player.Out.SendMessage(
                    "You cannot do more than " + MaxTasksDone(player.Level) + " tasks at your level!",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.TempProperties.GetProperty<int>(CHECK_TASK_TICK) > GameLoop.GameLoopTime)
            {
                player.Out.SendMessage(
                    "I have no tasks for you at the moment. Come back sometime later, perhaps then you can help me with something.",
                    eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                return false;
            }

            if (Util.Chance(chanceOfSuccess))
            {
                return true;
            }
            
            player.Out.SendMessage(
                "I have no tasks for you at the moment. Come back sometime later, perhaps then you can help me with something.",
                eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            // stored time of try to disable task for defined time.
            player.TempProperties.SetProperty(CHECK_TASK_TICK, GameLoop.GameLoopTime + CHECK_TASK_DELAY);
            return false;
        }
    }
}
