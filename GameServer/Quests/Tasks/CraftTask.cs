using System;
using System.Collections;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;

namespace Core.GS.Quests
{
	/// <summary>
	/// Declares a Craft task.
	/// craft Item for NPC
	/// </summary>
	public class CraftTask : ATask
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public const string RECIEVER_ZONE = "recieverZone";

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
			get { return "Craft the " + ItemName + " for " + ReceiverName + " in " + RecieverZone; }
		}


		/// <summary>
		/// Zone related to task stored in dbTask
		/// </summary>
		public virtual String RecieverZone
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
		public override void Notify(CoreEvent e, object sender, EventArgs args)
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

				if (player.Task.ReceiverName == target.Name && item.Name == player.Task.ItemName)
				{
					player.Inventory.RemoveItem(item);
                    InventoryLogging.LogInventoryAction(player, target, EInventoryActionType.Quest, item.Template, item.Count);
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

			var craftitem = CoreDb<DbCraftedItem>.SelectObjects(DB.Column("CraftingSkillType").IsEqualTo((int)player.CraftingPrimarySkill)
				.And(DB.Column("CraftingLevel").IsGreatherThan(lowLevel).And(DB.Column("CraftingLevel").IsLessThan(highLevel))));
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

			GameNpc NPC = GetRandomNPC(player);
			if (NPC == null)
			{
				player.Out.SendMessage("I have no task for you, come back some time later.", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				return false;
			}

			DbItemTemplate taskItem = GenerateNPCItem(player);

			if (taskItem == null)
			{
				player.Out.SendMessage("I can't think of anything for you to make, perhaps you should ask again.", EChatType.CT_System, EChatLoc.CL_PopupWindow);
				log.ErrorFormat("Craft task item is null for player {0} at level {1}.", player.Name, player.Level);
				return false;
			}

			var craftTask = new CraftTask(player)
								{
									TimeOut = DateTime.Now.AddHours(2),
									ItemName = taskItem.Name,
									ReceiverName = NPC.Name,
									RecieverZone = NPC.CurrentZone.Description
								};

			craftTask.SetRewardMoney((long)(taskItem.Price * RewardMoneyRatio));

			player.Task = craftTask;

			player.Out.SendMessage("Craft " + taskItem.GetName(0, false) + " for " + NPC.Name + " in " + NPC.CurrentZone.Description, EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			return true;

		}

		/// <summary>
		/// Find a Random NPC
		/// </summary>
		/// <param name="Player">The GamePlayer Object</param>		
		/// <returns>The GameNPC Searched</returns>
		public static GameNpc GetRandomNPC(GamePlayer Player)
		{
			return Player.CurrentZone.GetRandomNPC(new ERealm[] { ERealm.Albion, ERealm.Hibernia, ERealm.Midgard });
		}

		public new static bool CheckAvailability(GamePlayer player, GameLiving target)
		{
			if (target == null)
				return false;

			if (target is CraftMasterNpc)
			{
				if (((target as CraftMasterNpc).TheCraftingSkill == player.CraftingPrimarySkill))
					return ATask.CheckAvailability(player, target, CHANCE);
			}
			return false;//else return false
		}
	}
}