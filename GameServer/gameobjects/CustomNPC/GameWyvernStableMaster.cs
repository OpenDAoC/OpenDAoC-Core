using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Events; 

namespace DOL.GS
{
	/// <summary>
	/// Stable master that sells and takes Wyvern route tickets, implementing gradual take-off.
	/// </summary>
	public class GameWyvernStableMaster : GameMerchant
	{
		/// <summary>
		/// Called when a player buys an item
		/// </summary>
		public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
		{
			int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
			int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;

			DbItemTemplate template = this.TradeItems.GetItem(pagenumber, (eMerchantWindowSlot)slotnumber);
			if (template == null) return;

			int amountToBuy = number;
			if (template.PackSize > 0)
				amountToBuy *= template.PackSize;

			if (amountToBuy <= 0) return;

			long totalValue = number * template.Price;

			GameInventoryItem item = GameInventoryItem.Create(template);

			lock (player.Inventory.Lock)
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

				string message;
				if (amountToBuy > 1)
					message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.BoughtPieces", amountToBuy, template.GetName(1, false), Money.GetString(totalValue));
				else
					message = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameMerchant.OnPlayerBuy.Bought", template.GetName(1, false), Money.GetString(totalValue));

				if (!player.RemoveMoney(totalValue, message, eChatType.CT_Merchant, eChatLoc.CL_SystemWindow))
				{
					throw new Exception("Money amount changed while adding items.");
				}
				InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, totalValue);
			}

			if (item.Name.ToUpper().Contains("TICKET TO") || item.Description.ToUpper() == "TICKET")
			{
				DbInventoryItem ticket = player.Inventory.GetFirstItemByName(item.Name, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) as DbInventoryItem;
				if (ticket != null)
					ReceiveItem(player, ticket);
			}
		}

		/// <summary>
		/// Called when the player hands the ticket to the stable master.
		/// </summary>
		public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
		{
			if (source == null || item == null) return false;

			if (source is GamePlayer)
			{
				GamePlayer player = (GamePlayer)source;

				if (item.Item_Type == 40 && (item.Name.ToUpper().StartsWith("TICKET TO") || item.Name.ToUpper().StartsWith("WYVERN TICKET TO")))
				{
                    // Korrigierte NPC-Suche (CS0411/CS1061/CS0117 behoben)
					foreach (GameNPC npc in this.GetObjectsInRadius<GameNPC>(eGameObjectType.NPC, 1500))
					{
                        // Prüft auf GameTaxiWyvern
						if (npc is GameTaxiWyvern) 
						{
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameBoatStableMaster.ReceiveItem.Departed", this.Name).Replace("Boat", "Wyvern"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
							return false;
						}
					}

					String destination = item.Name.Substring(item.Name.ToUpper().Contains("WYVERN TICKET TO") ? "WYVERN TICKET TO".Length : "TICKET TO".Length);
					PathPoint path = MovementMgr.LoadPath(item.Id_nb);
                    
					if ((path != null) && ((Math.Abs(path.X - this.X)) < 500) && ((Math.Abs(path.Y - this.Y)) < 500))
					{
						player.Inventory.RemoveCountFromStack(item, 1);
						InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.Template);

                        // Wyvern instanziieren (holt ID aus GameTaxiWyvern.cs)
						GameTaxiWyvern wyvern = new GameTaxiWyvern(); 
						
                        // DIAGNOSE: Zeigt die statische ID im Server-Log
                        Console.WriteLine($"[WYVERN-SPAWN] Statische Model-ID (aus GameTaxiWyvern.cs) zugewiesen.");

                        wyvern.Name = "Wyvern to " + destination;
						wyvern.Realm = source.Realm;
                        
						wyvern.X = path.X;
						wyvern.Y = path.Y;
						wyvern.Z = path.Z; 
						
						wyvern.CurrentRegion = CurrentRegion;
						wyvern.Heading = path.GetHeading( path.Next );
                        
						wyvern.AddToWorld(); 
						wyvern.CurrentWaypoint = path;

                        // START-SEQUENZ
						new MountHorseAction(player, wyvern).Start(400); 
						new WyvernTakeOffAction(wyvern, path.Z).Start(1000); 

                        // Nachricht senden
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameBoatStableMaster.ReceiveItem.SummonedBoat", this.Name, destination).Replace("Boat", "Wyvern"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
						return true;
					}
					else
					{
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameBoatStableMaster.ReceiveItem.UnknownWay", this.Name, destination).Replace("Boat", "Wyvern"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
				}
			}

			return base.ReceiveItem(source, item);
		}


        // --- HELPER-KLASSEN (ECSGameTimerWrapperBase) ---

        // Workaround-Klassen für Flug und Höhe beibehalten

		protected class WyvernTakeOffAction : ECSGameTimerWrapperBase
		{
			protected readonly GameNPC m_wyvern;
			protected readonly int m_targetZ;
			protected readonly int m_startZ;
			protected readonly int m_totalTicks = 100;
			protected int m_currentTick = 0;

			public WyvernTakeOffAction(GameNPC wyvern, int targetZ) : base(wyvern)
			{
				if (wyvern == null)
					throw new ArgumentNullException("wyvern");
				m_wyvern = wyvern;
				m_startZ = wyvern.Z;
				m_targetZ = targetZ + 1000; 
			}

			protected override int OnTick(ECSGameTimer timer)
			{
				m_currentTick++;
				double progress = (double)m_currentTick / m_totalTicks;

				if (progress >= 1.0)
				{
					m_wyvern.Z = m_targetZ; 
					
					new HorseRideAction(m_wyvern).Start(0); 
                    new WyvernHeightFixAction(m_wyvern).Start(100); 

					return 0;
				}

				int newZ = (int)(m_startZ + (m_targetZ - m_startZ) * progress);
				m_wyvern.Z = newZ;
				m_wyvern.X = m_wyvern.X; 
				
				return 1;
			}
		}

        /// <summary>
        /// WORKAROUND: Erzwingt die Z-Koordinate während des Fluges, da die Flug-Flags fehlen.
        /// </summary>
        protected class WyvernHeightFixAction : ECSGameTimerWrapperBase
        {
            protected readonly GameNPC m_wyvern;
            protected readonly int m_fixTicks = 3000;
            protected int m_currentFixTick = 0; 

            public WyvernHeightFixAction(GameNPC wyvern) : base(wyvern)
            {
                if (wyvern == null)
					throw new ArgumentNullException("wyvern");
				m_wyvern = wyvern;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                m_currentFixTick++;
                
                if (m_wyvern.CurrentWaypoint == null)
                {
                    return 0;
                }
                
                PathPoint currentPath = m_wyvern.CurrentWaypoint;
                int targetZ = currentPath.Z + 1000;

                m_wyvern.Z = targetZ;
                m_wyvern.X = m_wyvern.X; 

                if (m_currentFixTick > m_fixTicks) 
                {
                    return 0;
                }

                return 1;
            }
        }
        
		protected class MountHorseAction : ECSGameTimerWrapperBase
		{
			protected readonly GameNPC m_horse;

			public MountHorseAction(GamePlayer actionSource, GameNPC horse)
				: base(actionSource)
			{
				if (horse == null)
					throw new ArgumentNullException("horse");
				m_horse = horse;
			}

			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;
				player.MountSteed(m_horse, true);
				return 0;
			}
		}

		protected class HorseRideAction : ECSGameTimerWrapperBase
		{
			public HorseRideAction(GameNPC actionSource) : base(actionSource) { }

			protected override int OnTick(ECSGameTimer timer)
			{
				GameNPC horse = (GameNPC) timer.Owner;
				horse.MoveOnPath(horse.MaxSpeed); 
				return 0;
			}
		}

	}
}