using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	/// <summary>
	/// LootGeneratorOneTimeDrop
	/// This implementation make the loot drop only one time by player
	/// </summary>
	public class LootGeneratorOneTimeDrop : LootGeneratorBase
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		///
		/// </summary>
		protected static Dictionary<string, List<DbLootOtd>> m_mobOTDList = new Dictionary<string,List<DbLootOtd>>();

		public LootGeneratorOneTimeDrop()
		{
			PreloadLootOTDs();
		}

		public static void ReloadLootOTD()
		{
			PreloadLootOTDs();
		}

		protected static bool PreloadLootOTDs()
		{
			lock (m_mobOTDList)
			{
				m_mobOTDList.Clear();
				IList<DbLootOtd> lootOTDs;

				try
				{
					lootOTDs = GameServer.Database.SelectAllObjects<DbLootOtd>();
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
					{
						log.Error("LootGeneratorOneTimeDrop: Drops could not be loaded:", e);
					}

					return false;
				}

				if (lootOTDs != null && lootOTDs.Count > 0)
				{
					int count = 0;

					foreach (DbLootOtd l in lootOTDs)
					{
						IList<DbMob> mobs = DOLDB<DbMob>.SelectObjects(DB.Column("Name").IsEqualTo(l.MobName));

						if (mobs == null || mobs.Count == 0)
						{
							log.ErrorFormat("Can't find MobName {0} for OTD {1}", l.MobName, l.ItemTemplateID);
							continue;
						}

						DbItemTemplate item = GameServer.Database.FindObjectByKey<DbItemTemplate>(l.ItemTemplateID);

						if (item == null)
						{
							log.ErrorFormat("Can't find ItemTemplate {0} for OTD MobName {1}", l.ItemTemplateID, l.MobName);
							continue;
						}

						if (m_mobOTDList.ContainsKey(l.MobName.ToLower()))
						{
							List<DbLootOtd> drops = m_mobOTDList[l.MobName.ToLower()];

							if (drops.Contains(l) == false)
							{
								drops.Add(l);
								count++;
							}
							else
							{
								log.ErrorFormat("Same OTD ItemTemplate {0} specified multiple times for MobName {1}", l.ItemTemplateID, l.MobName);
							}
						}
						else
						{
							List<DbLootOtd> drops = new List<DbLootOtd>();
							drops.Add(l);
							m_mobOTDList.Add(l.MobName.ToLower(), drops);
							count++;
						}

					}

					log.InfoFormat("One Time Drop generator pre-loaded {0} drops.", count);
				}
			}

			return true;
		}


		/// <summary>
		/// Refresh the OTDs for this mob
		/// </summary>
		/// <param name="mob"></param>
		public override void Refresh(GameNPC mob)
		{
			if (mob == null)
				return;

			IList<DbLootOtd> otds = DOLDB<DbLootOtd>.SelectObjects(DB.Column("MobName").IsEqualTo(mob.Name));

			lock (m_mobOTDList)
			{
				m_mobOTDList.Remove(mob.Name.ToLower());
			}

			if (otds != null)
			{
				lock (m_mobOTDList)
				{
					List<DbLootOtd> newList = [.. otds];
					m_mobOTDList.Add(mob.Name.ToLower(), newList);
				}
			}
		}

		public override LootList GenerateLoot(GameNPC mob, GameObject killer)
		{
			LootList loot = base.GenerateLoot(mob, killer);
			List<DbLootOtd> lootOTDs = null;

			try
			{
				if (m_mobOTDList.ContainsKey(mob.Name.ToLower()))
				{
					lootOTDs = m_mobOTDList[mob.Name.ToLower()];
				}

				if (lootOTDs != null)
				{
					lock (mob.XpGainersLock)
					{
						foreach (GameObject gainer in mob.XPGainers.Keys)
						{
							GamePlayer player = null;

							if (gainer is GamePlayer)
							{
								player = gainer as GamePlayer;
							}
							else if (gainer is GameNPC)
							{
								IControlledBrain brain = ((GameNPC)gainer).Brain as IControlledBrain;
								if (brain != null)
								{
									player = brain.GetPlayerOwner();
								}
							}

							if (player != null)
							{
								foreach (DbLootOtd drop in lootOTDs)
								{
									if (drop.MinLevel <= player.Level)
									{
										var hasDrop = DOLDB<DbCharacterXOneTimeDrop>.SelectObject(DB.Column("CharacterID").IsEqualTo(player.QuestPlayerID).And(DB.Column("ItemTemplateID").IsEqualTo(drop.ItemTemplateID)));

										if (hasDrop == null)
										{
											DbItemTemplate item = GameServer.Database.FindObjectByKey<DbItemTemplate>(drop.ItemTemplateID);

											if (item != null)
											{
												if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(item)))
												{
													DbCharacterXOneTimeDrop charXDrop = new DbCharacterXOneTimeDrop();
													charXDrop.CharacterID = player.QuestPlayerID;
													charXDrop.ItemTemplateID = drop.ItemTemplateID;
													GameServer.Database.AddObject(charXDrop);

													player.Out.SendMessage(string.Format("You receive {0} from {1}!", item.GetName(1, false), mob.GetName(1, false)), eChatType.CT_Loot, eChatLoc.CL_SystemWindow);
													InventoryLogging.LogInventoryAction(mob, player, eInventoryActionType.Loot, item);
												}
												else
												{
													// do not drop, player will have to try again
													player.Out.SendMessage("Your inventory is full and a one time drop cannot be added!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
													log.DebugFormat("OTD Failed, Inventory full: {0} from mob {1} for player {2}.", drop.ItemTemplateID, drop.MobName, player.Name);
													break;
												}
											}
											else
											{
												log.ErrorFormat("Error trying to drop ItemTemplate {0} from {1}.  Item not found.", drop.ItemTemplateID, drop.MobName);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.Error("LootGeneratorOneTimeDrop exception for mob " + mob.Name + ":", ex);
			}

			return loot;
		}
	}
}
