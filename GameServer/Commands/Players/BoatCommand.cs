using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

/// <summary>
/// command handler for /boat command
/// </summary>
[Command(
	"&boat",
	new string[] { "&boatcommand" },
	EPrivLevel.Player,
	"Boat command (use /boat for options)",
	"/boat <option>")]
public class BoatCommand : ACommandHandler, ICommandHandler
{
	/// <summary>
	/// method to handle /boat commands from a client
	/// </summary>
	/// <param name="client"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "boat"))
			return;

		try
		{
			switch (args[1])
			{
				case "summon":
					{
						if (!client.Player.IsSwimming)
						{
							// Not in water
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotInWater"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							return;
						}

						// Check to see if player has boat
						int boatFound = 0;
						GameBoat curBoat = BoatMgr.GetBoatByOwner(client.Player.InternalID);
						if (curBoat != null)
						{
							if (curBoat.OwnerID == client.Player.InternalID)
								boatFound = 1;
							else
								curBoat = null;
						}
						else
							curBoat = null;

						if (curBoat == null && boatFound != 1)
						{
							if (GameBoat.PlayerHasItem(client.Player, "scout_boat"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("scout_boat", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s scout boat";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 2648;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 500;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[8];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "warship"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("warship", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s warship";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 2647;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 400;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[32];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "galleon"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("galleon", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s galleon";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 2646;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 300;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[16];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "skiff"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("skiff", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s skiff";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 1616;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 250;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[8];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "Viking_Longship"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("Viking_Longship", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s Viking longship";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 1615;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 500;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[32];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "ps_longship"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("ps_longship", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s Longship";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 1595;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 600;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[31];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "stygian_ship"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("stygian_ship", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s Stygian ship";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 1612;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 500;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[24];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "atlantean_ship"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("atlantean_ship", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s Atlantean ship";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 1613;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 800;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[64];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else if (GameBoat.PlayerHasItem(client.Player, "British_Cog"))
							{
								GameBoat playerBoat = new GameBoat();
								DbInventoryItem item = client.Player.Inventory.GetFirstItemByID("British_Cog", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv);
								playerBoat.BoatID = System.Guid.NewGuid().ToString();
								playerBoat.Name = client.Player.Name + "'s British Cog";
								playerBoat.X = client.Player.X;
								playerBoat.Y = client.Player.Y;
								playerBoat.Z = client.Player.Z;
								playerBoat.Model = 1614;
								playerBoat.Heading = client.Player.Heading;
								playerBoat.Realm = client.Player.Realm;
								playerBoat.CurrentRegionID = client.Player.CurrentRegionID;
								playerBoat.OwnerID = client.Player.InternalID;
								playerBoat.MaxSpeedBase = 700;
								client.Player.Inventory.RemoveItem(item);
								InventoryLogging.LogInventoryAction(client.Player, "(ground)", EInventoryActionType.Other, item.Template, item.Count);
								playerBoat.Riders = new GamePlayer[33];
								BlankBrain brain = new BlankBrain();
								playerBoat.SetOwnBrain(brain);
								playerBoat = BoatMgr.CreateBoat(client.Player, playerBoat);
								if (client.Player.Guild != null)
								{
									if (client.Player.Guild.Emblem != 0)
										playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;

									playerBoat.GuildName = client.Player.Guild.Name;
								}
								playerBoat.AddToWorld();
								client.Player.MountSteed(playerBoat, true);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotOwnBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								return;
							}
							BoatMgr.SaveAllBoats();
						}
						else if (boatFound == 1)
						{
							if (client.Player.Guild != null)
							{
								if (client.Player.Guild.Emblem != 0)
									curBoat.Emblem = (ushort)client.Player.Guild.Emblem;

								curBoat.GuildName = client.Player.Guild.Name;
							}

							curBoat.X = client.Player.X;
							curBoat.Y = client.Player.Y;
							curBoat.Z = client.Player.Z;
							curBoat.Heading = client.Player.Heading;
							curBoat.Realm = client.Player.Realm;
							curBoat.CurrentRegionID = client.Player.CurrentRegionID;
							curBoat.Riders = new GamePlayer[32];
							BlankBrain brain = new BlankBrain();
							curBoat.SetOwnBrain(brain);
							curBoat.AddToWorld();
							client.Player.MountSteed(curBoat, true);
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Summoned", curBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						else
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotOwnBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						break;
					}
				case "unsummon":
					{
						GameBoat playerBoat = BoatMgr.GetBoatByOwner(client.Player.InternalID);

						if (playerBoat != null)
						{
							if (client.Player.InternalID == playerBoat.OwnerID)
							{
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Unsummoned", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								playerBoat.SaveIntoDatabase();
								playerBoat.RemoveFromWorld();
							}
						}
						else
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotOwnBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						break;
					}
				case "board":
					{
						GameBoat playerBoat = BoatMgr.GetBoatByName(client.Player.TargetObject.Name);
						if (client.Player.TargetObject == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NoBoatSelected"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							break;
						}

						if (playerBoat.MAX_PASSENGERS > 1)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.YouBoard", playerBoat.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							client.Player.MountSteed(playerBoat, true);
						}
						else
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.FullBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						break;
					}
				case "follow":
					{
						GameBoat targetBoat = BoatMgr.GetBoatByName(client.Player.TargetObject.Name);

						if (client.Player.Steed.OwnerID == client.Player.InternalID)// needs to be player on own boat
						{
							if (client.Player.TargetObject == null)
							{
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NoBoatSelected"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								break;
							}

							client.Player.Steed.Follow(targetBoat, 800, 5000);
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.MoveFollow", client.Player.TargetObject.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						else
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotOwnBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						break;
					}
				case "stopfollow":
					{
						if (client.Player.Steed.OwnerID == client.Player.InternalID)// needs to be player on own boat
						{
							client.Player.Steed.StopFollowing();
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.StopFollow"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						else
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotOwnBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						break;
					}
				case "invite":
					{
						break;
					}
				case "delete":
					{
						if (client.Player.TargetObject == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NoBoatSelected"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							break;
						}
						GameBoat playerBoat = BoatMgr.GetBoatByName(client.Player.TargetObject.Name);

						if (client.Player.InternalID == playerBoat.OwnerID)
							client.Player.Out.SendCustomDialog(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.DeleteConfirmation", playerBoat.Name), new CustomDialogResponse(BoatDeleteConfirmation));
						else
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotOwnBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);

						break;
					}
				case "boot":
					{
						GameBoat playerBoat = BoatMgr.GetBoatByOwner(client.Player.InternalID);

						if (client.Player.InternalID == playerBoat.OwnerID)
						{
							if (client.Player.TargetObject == null)
							{
								// no player selected
								break;
							}

							GamePlayer target = (client.Player.TargetObject as GamePlayer);
							if (playerBoat.RiderSlot(target) != -1)
							{
								target.DismountSteed(true);
								target.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.BootedBy", client.Player.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.BootedTarget", target.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.TargetNotInBoat", target.Name), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
							}
						}
						else
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.NotOwnBoat"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						}
						break;
					}
				default:
					{
						client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.UnknownCommand"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
						DisplayHelp(client);
					}
					break;
			}
		}
		catch (Exception)
		{
			DisplayHelp(client);
		}
	}
	public void DisplayHelp(GameClient client)
	{
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Usage"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Help.Summon"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Help.Unsummon"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Help.Follow"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Help.StopFollow"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Help.Board"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Help.Boot"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
		client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Player.Boat.Help.Delete"), EChatType.CT_Guild, EChatLoc.CL_SystemWindow);
	}

	protected void BoatDeleteConfirmation(GamePlayer player, byte response)
	{
		if (response != 0x01) return;

		GameBoat playerBoat = BoatMgr.GetBoatByOwner(player.InternalID);

		playerBoat.RemoveFromWorld();
		BoatMgr.DeleteBoat(playerBoat.Name);
	}
}