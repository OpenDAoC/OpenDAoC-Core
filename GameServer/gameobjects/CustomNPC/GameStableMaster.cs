
using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// Stable master that sells and takes horse route tickes
	/// </summary>
	[NpcGuild("Stable Master", ERealm.None)]
	public class GameStableMaster : GameMerchant
	{
		/// <summary>
		/// Constructs a new stable master
		/// </summary>
		public GameStableMaster()
		{
		}

		/// <summary>
		/// Called when a player buys an item
		/// </summary>
		/// <param name="player">The player making the purchase</param>
		/// <param name="item_slot">slot of the item to be bought</param>
		/// <param name="number">Number to be bought</param>
		public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
		{
			//Get the template
			int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
			int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

			DbItemTemplates template = this.TradeItems.GetItem(pagenumber, (eMerchantWindowSlot)slotnumber);
			if (template == null) return;

			//Calculate the amout of items
			int amountToBuy = number;
			if (template.PackSize > 0)
				amountToBuy *= template.PackSize;

			if (amountToBuy <= 0) return;

			//Calculate the value of items
			long totalValue = number * template.Price;

			GameInventoryItem item = GameInventoryItem.Create(template);

			lock (player.Inventory)
			{

				if (player.GetCurrentMoney() < totalValue)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.YouNeed", Money.GetString(totalValue)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}

				if (!player.Inventory.AddTemplate(item, amountToBuy, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.NotInventorySpace"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
				InventoryLogging.LogInventoryAction(this, player, eInventoryActionType.Merchant, template, amountToBuy);
				//Generate the buy message
				string message;
				if (amountToBuy > 1)
					message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.BoughtPieces", amountToBuy, template.GetName(1, false), Money.GetString(totalValue));
				else
					message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.Bought", template.GetName(1, false), Money.GetString(totalValue));

				// Check if player has enough money and subtract the money
				if (!player.RemoveMoney(totalValue, message, eChatType.CT_Merchant, eChatLoc.CL_SystemWindow))
				{
					throw new Exception("Money amount changed while adding items.");
				}
				InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, totalValue);
			}

			if (item.Name.ToUpper().Contains("TICKET TO") || item.Description.ToUpper() == "TICKET")
			{
				// Give the ticket to the merchant
				InventoryItem ticket = player.Inventory.GetFirstItemByName(item.Name, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) as InventoryItem;
				if (ticket != null)
					ReceiveItem(player, ticket);
			}
		}

		/// <summary>
		/// Called when the living is about to get an item from someone
		/// else
		/// </summary>
		/// <param name="source">Source from where to get the item</param>
		/// <param name="item">Item to get</param>
		/// <returns>true if the item was successfully received</returns>
		public override bool ReceiveItem(GameLiving source, InventoryItem item)
		{
			if (source == null || item == null) return false;
			
			if (this.DataQuestList.Count > 0)
			{
				foreach (DataQuest quest in DataQuestList)
				{
					quest.Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));
				}
			}

			if (source is GamePlayer)
			{
				GamePlayer player = (GamePlayer)source;

				if (item.Item_Type == 40 && isItemInMerchantList(item))
				{
					PathPointUtil path = MovementMgr.LoadPath(item.Id_nb);

					if ((path != null) && ((Math.Abs(path.X - this.X)) < 500) && ((Math.Abs(path.Y - this.Y)) < 500))
					{
						player.Inventory.RemoveCountFromStack(item, 1);
                        InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.Template);

						GameTaxi mount;
						
						// item.Color of ticket is used for npctemplate. defaults to standard horse if item.color is 0
						// item.Color(313)=hib horse, item.Color(312)=mid horse, item.Color(311)=alb horse
						if (item.Color > 0)
						{
							mount = new GameTaxi(NpcTemplateMgr.GetTemplate(item.Color));
						}
						else
						{
                            mount = new GameTaxi();

                            foreach (GameNPC npc in GetNPCsInRadius(400))
                            { 
                                if (npc.Name == LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameStableMaster.ReceiveItem.HorseName"))
                                {
                                    mount.Model = npc.Model;
                                    mount.Name = npc.Name;
                                    break;
                                }
                            }
						}
						
						switch ((ERace)player.Race)
						{
							case ERace.Lurikeen:
								mount.Size = 38;
								break;
							case ERace.Kobold:
								mount.Size = 38;
								break;
							case ERace.Dwarf:
								mount.Size = 42;
								break;
							case ERace.Inconnu:
								mount.Size = 45;
								break;
							case ERace.Frostalf:
								mount.Size = 48;
								break;
							case ERace.Shar:
								mount.Size = 48;
								break;
							case ERace.Briton:
								mount.Size = 50;
								break;
							case ERace.Saracen:
								mount.Size = 48;
								break;
							case ERace.Celt:
								mount.Size = 50;
								break;
							case ERace.Valkyn:
								mount.Size = 52;
								break;
							case ERace.Avalonian:
								mount.Size = 52;
								break;
							case ERace.Highlander:
								mount.Size = 55;
								break;
							case ERace.Norseman:
								mount.Size = 50;
								break;
							case ERace.Elf:
								mount.Size = 52;
								break;
							case ERace.Sylvan:
								mount.Size = 55;
								break;
							case ERace.Firbolg:
								mount.Size = 62;
								break;
							case ERace.HalfOgre:
								mount.Size = 62;
								break;
							case ERace.AlbionMinotaur:
								mount.Size = 65;
								break;
							case ERace.MidgardMinotaur:
								mount.Size = 65;
								break;
							case ERace.HiberniaMinotaur:
								mount.Size = 65;
								break;
							case ERace.Troll:
								mount.Size = 67;
								break;
							default:
								mount.Size = 50;
								break;
						}

						mount.Realm = source.Realm;
						mount.X = path.X;
						mount.Y = path.Y;
						mount.Z = path.Z;
						mount.CurrentRegion = CurrentRegion;
						mount.Heading = path.GetHeading( path.Next );
						mount.FixedSpeed = true;
						mount.MaxSpeedBase = 1500;
						mount.AddToWorld();
						mount.CurrentWaypoint = path;
						//GameEventMgr.AddHandler(mount, GameNPCEvent.PathMoveEnds, new DOLEventHandler(OnHorseAtPathEnd));
						new MountHorseAction(player, mount).Start(400);
						new HorseRideAction(mount).Start(4000);
						return true;
					}
				}
				else
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameStableMaster.ReceiveItem.UnknownWay"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
			return false;
		}

		private bool isItemInMerchantList(InventoryItem item)
		{
			if (m_tradeItems != null)
			{
				foreach (DictionaryEntry de in m_tradeItems.GetAllItems())
				{
					DbItemTemplates compareItem = de.Value as DbItemTemplates;
					if (compareItem != null)
					{
						if (compareItem.Id_nb == item.Id_nb)
						{
							return true;
						}
					}
				}
			}
			return false;
		}


		private void SendReply(GamePlayer target, string msg)
		{
			target.Out.SendMessage(
				msg,
				eChatType.CT_System, eChatLoc.CL_PopupWindow);
		}

		/// <summary>
		/// Handles 'horse route end' events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="o"></param>
		/// <param name="args"></param>
		public void OnHorseAtPathEnd(CoreEvent e, object o, EventArgs args)
		{
			if (!(o is GameNPC)) return;
			GameNPC npc = (GameNPC)o;

			GameEventMgr.RemoveHandler(npc, GameNpcEvent.PathMoveEnds, new CoreEventHandler(OnHorseAtPathEnd));
			npc.StopMoving();
			npc.RemoveFromWorld();
		}

		/// <summary>
		/// Handles delayed player mount on horse
		/// </summary>
		protected class MountHorseAction : RegionAction
		{
			/// <summary>
			/// The target horse
			/// </summary>
			protected readonly GameNPC m_horse;

			/// <summary>
			/// Constructs a new MountHorseAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="horse">The target horse</param>
			public MountHorseAction(GamePlayer actionSource, GameNPC horse)
				: base(actionSource)
			{
				if (horse == null)
					throw new ArgumentNullException("horse");
				m_horse = horse;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer)m_actionSource;
				player.MountSteed(m_horse, true);
				return 0;
			}
		}

		/// <summary>
		/// Handles delayed horse ride actions
		/// </summary>
		protected class HorseRideAction : RegionAction
		{
			/// <summary>
			/// Constructs a new HorseStartAction
			/// </summary>
			/// <param name="actionSource"></param>
			public HorseRideAction(GameNPC actionSource)
				: base(actionSource)
			{
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GameNPC horse = (GameNPC)m_actionSource;
				horse.MoveOnPath(horse.MaxSpeed);
				return 0;
			}
		}
	}
}