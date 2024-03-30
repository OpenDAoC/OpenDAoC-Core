using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.Quests
{
    /// <summary>
    /// Declares the abstract quest class from which all user created
    /// quests must derive!
    /// </summary>
    public abstract class AbstractQuest
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The level of the quest.
        /// </summary>
        protected int m_questLevel = 1;

        /// <summary>
        /// The player doing the quest
        /// </summary>
        protected GamePlayer m_questPlayer = null;

        /// <summary>
        /// The quest database object, storing the information for the player
        /// and the quest. Eg. QuestStep etc.
        /// </summary>
        private DbQuest m_dbQuest = null;

        /// <summary>
        /// List of all QuestParts that can be fired on notify method of quest.
        /// </summary>
        protected static IList questParts = null;

        private List<QuestSearchArea> m_searchAreas = new();

        public AbstractQuest() { }

        public AbstractQuest(GamePlayer questingPlayer) : this(questingPlayer, 1) { }

        public AbstractQuest(GamePlayer questingPlayer,int step)
        {
            m_questPlayer = questingPlayer;

            DbQuest dbQuest = new()
            {
                Character_ID = questingPlayer.QuestPlayerID,
                Name = GetType().FullName,
                Step = step
            };

            m_dbQuest = dbQuest;
            SaveIntoDatabase();
        }

        public AbstractQuest(GamePlayer questingPlayer, DbQuest dbQuest)
        {
            m_questPlayer = questingPlayer;
            m_dbQuest = dbQuest;
            ParseCustomProperties();
            SaveIntoDatabase();
        }

        public static AbstractQuest LoadFromDatabase(GamePlayer targetPlayer, DbQuest dbQuest)
        {
            Type questType = null;

            foreach (Assembly asm in ScriptMgr.Scripts)
            {
                questType = asm.GetType(dbQuest.Name);
                if (questType != null)
                    break;
            }

            if (questType==null)
                questType = Assembly.GetAssembly(typeof(GameServer)).GetType(dbQuest.Name);

            if (questType==null)
            {
                if (log.IsErrorEnabled)
                    log.Error("Could not find quest: "+dbQuest.Name+"!");
                return null;
            }

            return (AbstractQuest) Activator.CreateInstance(questType, new object[] { targetPlayer, dbQuest });
        }

        public virtual void SaveIntoDatabase()
        {
            if (m_dbQuest is {IsPersisted: true})
                GameServer.Database.SaveObject(m_dbQuest);
            else
                GameServer.Database.AddObject(m_dbQuest);
        }

        public virtual void DeleteFromDatabase()
        {
            if (!m_dbQuest.IsPersisted)
                return;

            DbQuest dbQuest = GameServer.Database.FindObjectByKey<DbQuest>(m_dbQuest.ObjectId);

            if (dbQuest!=null)
                GameServer.Database.DeleteObject(dbQuest);
        }

        public virtual int MaxQuestCount => 1;

        public GamePlayer QuestPlayer
        {
            get => m_questPlayer;
            set
            {
                m_questPlayer = value;
                m_dbQuest.Character_ID = QuestPlayer.QuestPlayerID;
            }
        }

        public virtual string Name => "QUEST NAME UNDEFINED!";

        public virtual string Description => Step switch
        {
            -2 => "You have completed the quest!",
            -1 => "You have failed the quest!",
            _ => "QUEST DESCRIPTION UNDEFINED!",
        };

        public virtual int Level
        {
            get => m_questLevel;
            set
            {
                if (value is >= 1 and <= 50)
                    m_questLevel = value;
            }
        }

        public virtual int Step
        {
            get => m_dbQuest.Step;
            set
            {
                m_dbQuest.Step = value;
                SaveIntoDatabase();
                m_questPlayer.Out.SendQuestUpdate(this);
            }
        }

        public virtual bool IsDoingQuest()
        {
            return Step != -1;
        }

        public abstract bool CheckQuestQualification(GamePlayer player);

        public virtual void FinishQuest()
        {
            Step = -2; // -2 indicates quest finished, -1 indicates aborted quests etc, they won't show up in the list.
            m_questPlayer.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(m_questPlayer.Client, "AbstractQuest.FinishQuest.Completed", Name)), eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);

            // Move quest from active list to finished list.
            if (m_questPlayer.QuestList.TryRemove(this, out byte value))
                m_questPlayer.AvailableQuestIndexes.Enqueue(value);

            if (m_questPlayer.HasFinishedQuest(GetType()) == 0)
                m_questPlayer.AddFinishedQuest(this);

            m_questPlayer.Out.SendQuestRemove(value);
            m_questPlayer.SaveIntoDatabase();
        }

        public virtual void AbortQuest()
        {
            Step = -1;

            if (m_questPlayer.QuestList.TryRemove(this, out byte value))
                m_questPlayer.AvailableQuestIndexes.Enqueue(value);

            DeleteFromDatabase();
            m_questPlayer.Out.SendQuestRemove(value);
            m_questPlayer.Out.SendMessage(LanguageMgr.GetTranslation(m_questPlayer.Client, "AbstractQuest.AbortQuest"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        public abstract void Notify(DOLEvent e, object sender, EventArgs args);

        public virtual void OnQuestAssigned(GamePlayer player)
        {
            player.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractQuest.OnQuestAssigned.GetQuest", Name)), eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }

        #region Quest Commands

        protected eQuestCommand m_currentCommand = eQuestCommand.NONE;

        protected void AddSearchArea(QuestSearchArea searchArea)
        {
            if (!m_searchAreas.Contains(searchArea))
                m_searchAreas.Add(searchArea);
        }

        public virtual bool Command(GamePlayer player, eQuestCommand command, AbstractArea area = null)
        {
            if (m_searchAreas == null || m_searchAreas.Count == 0)
                return false;

            if (player == null || command == eQuestCommand.NONE)
                return false;

            if (command == eQuestCommand.SEARCH)
            {
                foreach (AbstractArea playerArea in player.CurrentAreas)
                {
                    if (playerArea is not QuestSearchArea)
                        continue;

                    if (playerArea is QuestSearchArea questSearchArea && questSearchArea.Step == Step)
                    {
                        foreach (QuestSearchArea searchArea in m_searchAreas)
                        {
                            if (searchArea != questSearchArea)
                                continue;

                            StartQuestActionTimer(player, command, questSearchArea.SearchSeconds);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public virtual void StartQuestActionTimer(GamePlayer player, eQuestCommand command, int seconds, string label = "")
        {
            if (player.QuestActionTimer == null)
            {
                m_currentCommand = command;
                AddActionHandlers(player);

                if (label == "")
                    label = Enum.GetName(typeof(eQuestCommand), command);

                player.Out.SendTimerWindow(label, seconds);
                player.QuestActionTimer = new(player, new ECSGameTimer.ECSTimerCallback(QuestActionCallback), seconds * 1000);
            }
        }

        protected virtual int QuestActionCallback(ECSGameTimer timer)
        {
            if (timer.Owner is GamePlayer player)
            {
                RemoveActionHandlers(player);
                player.Out.SendCloseTimerWindow();
                player.QuestActionTimer.Stop();
                player.QuestActionTimer = null;
                QuestCommandCompleted(m_currentCommand, player);
            }

            m_currentCommand = eQuestCommand.NONE;
            return 0;
        }

        protected void AddActionHandlers(GamePlayer player)
        {
            if (player != null)
            {
                GameEventMgr.AddHandler(player, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(InterruptAction));
                GameEventMgr.AddHandler(player, GameLivingEvent.Dying, new DOLEventHandler(InterruptAction));
                GameEventMgr.AddHandler(player, GameLivingEvent.AttackFinished, new DOLEventHandler(InterruptAction));
            }
        }

        protected void RemoveActionHandlers(GamePlayer player)
        {
            if (player != null)
            {
                GameEventMgr.RemoveHandler(player, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(InterruptAction));
                GameEventMgr.RemoveHandler(player, GameLivingEvent.Dying, new DOLEventHandler(InterruptAction));
                GameEventMgr.RemoveHandler(player, GameLivingEvent.AttackFinished, new DOLEventHandler(InterruptAction));
            }
        }

        protected void InterruptAction(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is GamePlayer player)
            {
                if (m_currentCommand != eQuestCommand.NONE)
                {
                    string commandName = Enum.GetName(typeof(eQuestCommand), m_currentCommand).ToLower();
                    if (m_currentCommand == eQuestCommand.SEARCH_START)
                    {
                        commandName = Enum.GetName(typeof(eQuestCommand), eQuestCommand.SEARCH).ToLower();
                    }

                    player.Out.SendMessage("Your " + commandName + " is interrupted!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }

                RemoveActionHandlers(player);
                player.Out.SendCloseTimerWindow();
                player.QuestActionTimer.Stop();
                player.QuestActionTimer = null;
                m_currentCommand = eQuestCommand.NONE;
            }
        }

        protected virtual void QuestCommandCompleted(eQuestCommand command, GamePlayer player)
        {
            // Override this to do whatever needs to be done when the command is completed.
            // Typically this would be when giving the player an item and advancing the step.
            QuestPlayer.Out.SendMessage("Error, command completed handler not overridden for quest!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        #endregion Quest Commands

        #region Items

        protected static void RemoveItem(GamePlayer player, DbItemTemplate itemTemplate)
        {
            RemoveItem(null, player, itemTemplate, true);
        }

        protected static void RemoveItem(GamePlayer player, DbItemTemplate itemTemplate, bool notify)
        {
            RemoveItem(null, player, itemTemplate, notify);
        }

        protected static void RemoveItem(GameLiving target, GamePlayer player, DbItemTemplate itemTemplate)
        {
            RemoveItem(target, player, itemTemplate, true);
        }

        protected static void ReplaceItem(GamePlayer target, DbItemTemplate itemTemplateOut, DbItemTemplate itemTemplateIn)
        {
            target.Inventory.BeginChanges();
            RemoveItem(target, itemTemplateOut, false);
            GiveItem(target, itemTemplateIn);
            target.Inventory.CommitChanges();
        }

        protected static void RemoveItem(GameLiving target, GamePlayer player, DbItemTemplate itemTemplate, bool notify)
        {
            if (itemTemplate == null)
            {
                log.Error($"{nameof(itemTemplate)} is null in {nameof(RemoveItem)}:" + Environment.StackTrace);
                return;
            }

            lock (player.Inventory.LockObject)
            {
                DbInventoryItem item = player.Inventory.GetFirstItemByID(itemTemplate.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

                if (item != null)
                {
                    player.Inventory.RemoveItem(item);
                    InventoryLogging.LogInventoryAction(player, target, eInventoryActionType.Quest, item.Template, item.Count);

                    if (target != null)
                        player.Out.SendMessage($"You give the {itemTemplate.Name} to {target.GetName(0, false)}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else if (notify)
                    player.Out.SendMessage($"You cannot remove the {itemTemplate.Name} because you don't have it.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        protected static void RemoveItem(GameObject target, GamePlayer player, DbInventoryItem item, bool notify)
        {
            if (item == null)
            {
                log.Error($"{nameof(item)} is null in {nameof(RemoveItem)}:" + Environment.StackTrace);
                return;
            }

            lock (player.Inventory.LockObject)
            {
                if (item != null)
                {
                    player.Inventory.RemoveItem(item);
                    InventoryLogging.LogInventoryAction(player, target, eInventoryActionType.Quest, item.Template, item.Count);

                    if (target != null)
                        player.Out.SendMessage($"You give the {item.Name} to {target.GetName(0, false)}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else if (notify)
                    player.Out.SendMessage($"You cannot remove the {item.Name} because you don't have it.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        protected static int RemoveAllItem(GameLiving target, GamePlayer player, DbItemTemplate itemTemplate, bool notify)
        {
            int itemsRemoved = 0;

            if (itemTemplate == null)
            {
                log.Error($"{nameof(itemTemplate)} is null in {nameof(RemoveAllItem)}:" + Environment.StackTrace);
                return 0;
            }

            lock (player.Inventory.LockObject)
            {
                DbInventoryItem item = player.Inventory.GetFirstItemByID(itemTemplate.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

                while (item != null)
                {
                    player.Inventory.RemoveItem(item);
                    InventoryLogging.LogInventoryAction(player, target, eInventoryActionType.Quest, item.Template, item.Count);
                    itemsRemoved++;
                    item = player.Inventory.GetFirstItemByID(itemTemplate.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
                }

                if (notify)
                {
                    if (itemsRemoved == 0)
                        player.Out.SendMessage($"You cannot remove the {itemTemplate.Name} because you don't have it.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    else if (target != null)
                    {
                        if (itemTemplate.Name.EndsWith("s"))
                            player.Out.SendMessage($"You give the {itemTemplate.Name} to {target.Name}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        else
                            player.Out.SendMessage($"You give the {itemTemplate.Name}'s to {target.Name}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }

            return itemsRemoved;
        }

        #endregion

        public static Queue m_sayTimerQueue = new();
        public static Queue m_sayObjectQueue = new();
        public static Queue m_sayMessageQueue = new();
        public static Queue m_sayChatTypeQueue = new();
        public static Queue m_sayChatLocQueue = new();

        protected static int MakeSaySequence(ECSGameTimer callingTimer)
        {
            m_sayTimerQueue.Dequeue();
            GamePlayer player = (GamePlayer) m_sayObjectQueue.Dequeue();
            string message = (string) m_sayMessageQueue.Dequeue();
            eChatType chatType = (eChatType) m_sayChatTypeQueue.Dequeue();
            eChatLoc chatLoc = (eChatLoc) m_sayChatLocQueue.Dequeue();
            player.Out.SendMessage(message, chatType, chatLoc);
            return 0;
        }

        protected void SendSystemMessage(string msg)
        {
            SendSystemMessage(m_questPlayer, msg);
        }

        protected void SendEmoteMessage(string msg)
        {
            SendEmoteMessage(m_questPlayer, msg, 0);
        }

        protected static void SendSystemMessage(GamePlayer player, string msg)
        {
            SendEmoteMessage(player, msg, 0);
        }

        protected static void SendSystemMessage(GamePlayer player, string msg, uint delay)
        {
            SendMessage(player, msg, delay, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        protected static void SendEmoteMessage(GamePlayer player, string msg)
        {
            SendEmoteMessage(player, msg, 0);
        }

        protected static void SendEmoteMessage(GamePlayer player, string msg, uint delay)
        {
            SendMessage(player, msg, delay, eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
        }

        protected static void SendReply(GamePlayer player, string msg)
        {
            SendMessage(player, msg, 0, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
        }

        protected static void SendMessage(GamePlayer player, string msg, uint delay, eChatType chatType, eChatLoc chatLoc)
        {
            msg = BehaviourUtils.GetPersonalizedMessage(msg, player);

            if (delay == 0)
                player.Out.SendMessage(msg, chatType, chatLoc);
            else
            {
                m_sayMessageQueue.Enqueue(msg);
                m_sayObjectQueue.Enqueue(player);
                m_sayChatLocQueue.Enqueue(chatLoc);
                m_sayChatTypeQueue.Enqueue(chatType);
                m_sayTimerQueue.Enqueue(new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(MakeSaySequence), (int) delay * 100));
            }
        }

        protected static bool TryGiveItem(GamePlayer player, DbItemTemplate itemTemplate)
        {
            return GiveItem(null, player, itemTemplate, false);
        }

        protected static bool TryGiveItem(GameLiving source, GamePlayer player, DbItemTemplate itemTemplate)
        {
            return GiveItem(source, player, itemTemplate, false);
        }

        protected static bool GiveItem(GamePlayer player, DbItemTemplate itemTemplate)
        {
            return GiveItem(null, player, itemTemplate, true);
        }

        protected static bool GiveItem(GamePlayer player, DbItemTemplate itemTemplate, bool canDrop)
        {
            return GiveItem(null, player, itemTemplate, canDrop);
        }

        protected static bool GiveItem(GameLiving source, GamePlayer player, DbItemTemplate itemTemplate)
        {
            return GiveItem(source, player, itemTemplate, true);
        }

        protected static bool GiveItem(GameLiving source, GamePlayer player, DbItemTemplate itemTemplate, bool canDrop)
        {
            DbInventoryItem item;

            if (itemTemplate is DbItemUnique)
            {
                GameServer.Database.AddObject(itemTemplate as DbItemUnique);
                item = GameInventoryItem.Create(itemTemplate as DbItemUnique);
            }
            else
                item = GameInventoryItem.Create(itemTemplate);

            if (!player.ReceiveItem(source, item))
            {
                if (canDrop)
                {
                    player.CreateItemOnTheGround(item);
                    player.Out.SendMessage(string.Format("Your backpack is full, {0} is dropped on the ground.", itemTemplate.Name), eChatType.CT_Important, eChatLoc.CL_PopupWindow);
                }
                else
                {
                    player.Out.SendMessage("Your backpack is full!", eChatType.CT_Important, eChatLoc.CL_PopupWindow);
                    return false;
                }
            }

            return true;
        }

        protected static DbItemTemplate CreateTicketTo(string destination, string ticket_Id)
        {
            DbItemTemplate ticket = GameServer.Database.FindObjectByKey<DbItemTemplate>(GameServer.Database.Escape(ticket_Id.ToLower()));

            if (ticket == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find " + destination + ", creating it ...");

                ticket = new()
                {
                    Name = "ticket to " + destination,
                    Id_nb = ticket_Id.ToLower(),
                    Model = 499,
                    Object_Type = (int) eObjectType.GenericItem,
                    Item_Type = 40,
                    IsPickable = true,
                    IsDropable = true,
                    Price = Money.GetMoney(0, 0, 0, 5, 3),
                    PackSize = 1,
                    Weight = 0
                };

                GameServer.Database.AddObject(ticket);
            }

            return ticket;
        }

        #region Custom Properties

        /// <summary>
        /// This HybridDictionary holds all the custom properties of this quest
        /// </summary>
        protected readonly HybridDictionary m_customProperties = new();

        /// <summary>
        /// This method parses the custom properties string of the m_dbQuest
        /// into the HybridDictionary for easier use and access
        /// </summary>
        public void ParseCustomProperties()
        {
            if (m_dbQuest.CustomPropertiesString == null)
                return;

            lock (m_customProperties)
            {
                m_customProperties.Clear();

                foreach(string property in Util.SplitCSV(m_dbQuest.CustomPropertiesString))
                {
                    if (property.Length > 0)
                    {
                        string[] values = property.Split('=');
                        m_customProperties[values[0]] = values[1];
                    }
                }
            }
        }

        public void SetCustomProperty(string key, string value)
        {
            if (key==null)
                throw new ArgumentNullException(nameof(key));

            if (value==null)
                throw new ArgumentNullException(nameof(value));

            //Make the string safe
            key = key.Replace(';', ',');
            key = key.Replace('=', '-');
            value = value.Replace(';', ',');
            value = value.Replace('=', '-');

            lock(m_customProperties)
            {
                m_customProperties[key]=value;
            }

            SaveCustomProperties();
        }

        protected void SaveCustomProperties()
        {
            StringBuilder builder = new();

            lock (m_customProperties)
            {
                foreach(string hKey in m_customProperties.Keys)
                {
                    builder.Append(hKey);
                    builder.Append('=');
                    builder.Append(m_customProperties[hKey]);
                    builder.Append(';');
                }
            }

            m_dbQuest.CustomPropertiesString = builder.ToString();
            SaveIntoDatabase();
        }

        public void RemoveCustomProperty(string key)
        {
            if (key==null)
                throw new ArgumentNullException(nameof(key));

            lock (m_customProperties)
            {
                m_customProperties.Remove(key);
            }

            SaveCustomProperties();
        }

        public string GetCustomProperty(string key)
        {
            return key == null ? throw new ArgumentNullException(nameof(key)) : (string) m_customProperties[key];
        }

        #endregion

        public enum eQuestCommand
        {
            NONE,
            SEARCH,
            SEARCH_START
        }
    }
}
