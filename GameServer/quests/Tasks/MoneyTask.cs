using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS.Quests
{
    /// <summary>
    /// Declares a Money task.
    /// Bring Item A to NPC B
    /// </summary>
    public class MoneyTask : AbstractTask
    {
        // Chance of npc having task for player
        protected new const int CHANCE=35;

        public const string RECIEVER_ZONE = "recieverZone";

        private static readonly int[] MoneyReward = [25, 34, 46, 63, 84, 114, 154, 208, 282, 379, 441, 511, 592, 688, 798, 925, 1074, 1246, 1444, 1830];

        // used to generate generic item
        private static readonly string[] StrFormat = ["{0}'s {1} {2}", "{1} {2} of {0}"];
        private static readonly string[] Middle = ["Little", "Small", "Large", "Big", "Thin2"];
        private static readonly string[] TaskObjects = ["Potion", "Flask", "Necklace", "Ring", "Dyes", "Green Gem", "Note", "Scroll", "Book", "Hammer", "Crown", "Blue orb", "Red orb", "Silver orb", "Wand", "Bottle", "Gold key", "Silver key", "Mirror", "Scroll tube", "Four leaf clover", "Vine", "Silver earring", "Gold bracelet", "Gold necklace", "Amulet", "Letter"];
        private static readonly int[] ObjectModels = [99, 99, 101, 103, 229, 113, 498, 499, 500, 510, 511, 523, 524, 606, 552, 554, 583, 584, 592, 603, 607, 611, 621, 622, 623, 101, 498];

        /// <summary>
        /// Constructs a new Task
        /// </summary>
        /// <param name="taskPlayer">The player doing this task</param>
        public MoneyTask(GamePlayer taskPlayer) : base(taskPlayer)
        {
        }		

        /// <summary>
        /// Constructs a new Task from a database Object
        /// </summary>
        /// <param name="taskPlayer">The player doing the task</param>
        /// <param name="dbTask">The database object</param>
        public MoneyTask(GamePlayer taskPlayer, DbTask dbTask) : base(taskPlayer, dbTask)
        {			
        }

        public override long RewardMoney
        {
            get 
            {
                ushort Scarto = 3; // Add/Remove % to the Result
                                
                int ValueScarto = ((MoneyReward[m_taskPlayer.Level-1]/100)*Scarto);
                return Util.Random(MoneyReward[m_taskPlayer.Level-1]-ValueScarto,MoneyReward[m_taskPlayer.Level-1]+ValueScarto); 
            }
        }

        public override IList RewardItems
        {
            get {return null;}
        }		

        /// <summary>
        /// Retrieves the name of the task
        /// </summary>
        public override string Name
        {
            get { return "Money Task"; }
        }

        /// <summary>
        /// Retrieves the description
        /// </summary>
        public override string Description
        {
            get { return "Bring the "+ItemName+" to "+ReceiverName+" in " + RecieverZone; }
        }	


        /// <summary>
        /// Zone related to task stored in dbTask
        /// </summary>
        public virtual String RecieverZone
        {
            get { return GetCustomProperty(RECIEVER_ZONE);}
            set { SetCustomProperty(RECIEVER_ZONE,value);}
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

            GamePlayer player = (GamePlayer) sender;

            if (e == GamePlayerEvent.GiveItem)
            {
                GiveItemEventArgs gArgs = (GiveItemEventArgs)args;				
                GameLiving target = gArgs.Target as GameLiving;
                DbInventoryItem item = gArgs.Item;

                if(player.GameTask.ReceiverName == target.Name && item.Name == player.GameTask.ItemName)
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
        /// <param name="Name">Base Nameof the NPC</param>
        /// <param name="Level">Level of Generated Item</param>
        /// <returns>A Generated NPC Item</returns>
        public static DbInventoryItem GenerateNPCItem(string Name, int Level)
        {			
            int Id = Util.Random(0, TaskObjects.Length-1);
            int format = Util.Random(0, StrFormat.Length-1);
            int middle = Util.Random(0, Middle.Length-1);
            return GenerateItem(string.Format(StrFormat[format] ,Name,Middle[middle],TaskObjects[Id]), Level, ObjectModels[Id]);
        }		

        /// <summary>
        /// Create an Item, Search for a NPC to consign the Item and give Item to the Player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool BuildTask(GamePlayer player, GameLiving source)
        {
            if (source == null)
                return false;

            GameNPC NPC = GetRandomNPC(player);
            if(NPC == null)
            {
                player.Out.SendMessage("I have no task for you, come back some time later.",eChatType.CT_System,eChatLoc.CL_PopupWindow);
                return false;
            }
            else
            {
                DbInventoryItem TaskItems = GenerateNPCItem(NPC.Name, player.Level);
                
                player.GameTask = new MoneyTask(player);
                player.GameTask.TimeOut = DateTime.Now.AddHours(2);
                player.GameTask.ItemName = TaskItems.Name;
                player.GameTask.ReceiverName = NPC.Name;
                ((MoneyTask)player.GameTask).RecieverZone = NPC.CurrentZone.Description;
                
                player.Out.SendMessage("Bring "+TaskItems.GetName(0,false)+" to "+NPC.Name +" in "+ NPC.CurrentZone.Description, eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                //Player.Out.SendCustomDialog("", new CustomDialogResponse(TaskDialogResponse));

                player.ReceiveItem(source,TaskItems);
                return true;
            }
            
        }


        /// <summary>
        /// Find a Random NPC
        /// </summary>
        /// <param name="Player">The GamePlayer Object</param>		
        /// <returns>The GameNPC Searched</returns>
        public static GameNPC GetRandomNPC(GamePlayer Player)
        {
            return Player.CurrentZone.GetRandomNPC(new eRealm[]{eRealm.Albion,eRealm.Hibernia,eRealm.Midgard});			
        }

        public new static bool CheckAvailability(GamePlayer player, GameLiving target)
        {
            if (target==null)
                return false;

            if (target is GameTrainer || target is GameMerchant || target.Name.IndexOf("Crier")>=0) 
            {
                return AbstractTask.CheckAvailability(player,target,CHANCE);
            } 
            else 
            {
                return false;
            }

        }
    }
}
