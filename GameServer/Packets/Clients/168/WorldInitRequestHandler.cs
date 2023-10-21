using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.GS.ECS;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
    [PacketHandler(EPacketHandlerType.TCP, EClientPackets.WorldInitRequest, "Handles world init replies", EClientStatus.LoggedIn)]
    public class WorldInitRequestHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GsPacketIn packet)
        {
            // Instantiate 'GamePlayer'. Previous versions are handled in 'CharacterSelectRequestHandler'.
            if (client.Version >= GameClient.eClientVersion.Version1124)
                HandlePacket1124(client, packet);

            if (client.Player == null)
                return;

            client.UdpConfirm = false;
            new WorldInitAction(client.Player).Start(1);
        }

        private static void HandlePacket1124(GameClient client, GsPacketIn packet)
        {
            byte charIndex = (byte)packet.ReadByte(); // character account location

            // some funkyness going on below here. Could use some safeguards to ensure a character is loaded correctly
            if (client.Player == null && client.Account.Characters != null && client.ClientState == GameClient.eClientState.CharScreen)
            {
                bool charFound = false;
                string selectedChar = "";
                int realmOffset = charIndex - (client.Account.Realm * 10 - 10);
                int charSlot = client.Account.Realm * 100 + realmOffset;

                for (int i = 0; i < client.Account.Characters.Length; i++)
                {
                    if (client.Account.Characters[i] != null && client.Account.Characters[i].AccountSlot == charSlot)
                    {
                        charFound = true;
                        selectedChar = client.Account.Characters[i].Name;
                        client.LoadPlayer(i);
                        break;
                    }
                }

                if (!charFound)
                {
                    client.Player = null;
                    client.ActiveCharIndex = -1;
                }
                else
                    AuditMgr.AddAuditEntry(client, EAuditType.Character, EAuditSubType.CharacterLogin, "", selectedChar);
            }
        }

        /// <summary>
        /// Handles player world init requests
        /// </summary>
        protected class WorldInitAction : AuxEcsGameTimerWrapperBase
        {
            /// <summary>
            /// Constructs a new WorldInitAction
            /// </summary>
            /// <param name="actionSource">The action source</param>
            public WorldInitAction(GamePlayer actionSource) : base(actionSource) { }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override int OnTick(AuxEcsGameTimer timer)
            {
                GamePlayer player = (GamePlayer) timer.Owner;

                //check emblems at world load before any updates
                if (player.Inventory != null) 
                {
                    lock (player.Inventory)
                    {
                        GuildUtil playerGuild = player.Guild;
                        foreach (DbInventoryItem myitem in player.Inventory.AllItems)
                        {
                            if (myitem != null && myitem.Emblem != 0)
                            {
                                if (playerGuild == null || myitem.Emblem != playerGuild.Emblem)
                                {
                                    myitem.Emblem = 0;
                                }
                                if (player.Level < 20)
                                {
                                    if (player.CraftingPrimarySkill == ECraftingSkill.NoCrafting)
                                    {
                                        myitem.Emblem = 0;
                                    }
                                    else
                                    {
                                        if (player.GetCraftingSkillValue(player.CraftingPrimarySkill) < 400)
                                        {
                                            myitem.Emblem = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                player.Client.ClientState = GameClient.eClientState.WorldEnter;
                // 0x88 - Position
                // 0x6D - FriendList
                // 0x15 - Encumberance update
                // 0x1E - Speed update
                // 0xDD - Shared Buffs update
                // 0x1E - Speed update
                // 0x05 - Health, Sit update
                // 0xAA - Inventory Update
                // 0xAA - Inventory Update /Vault ?
                // 0xBE - 01 various tabs update (skills/spells...)
                // 0xBE - 08 various tabs update (skills/spells...)
                // 0xBE - 02 spells
                // 0xBE - 03 various tabs update (skills/spells...)
                // 0x52 - Money update
                // 0x53 - stats update
                // 0xD7 - Self Buffs update
                // 0xBE - 05, various tabs ...
                // 0x2B - Quest list
                // 0x05 - health again...?
                // 0x39 - XP update?
                // 0x15 - Encumberance update
                // 0xBE - 06, group
                // 0xE4 - ??  (0 0 5 0 0 0 0 0)
                // 0xBE - 0 1 0 0
                // 0x89 - Debug mode
                // 0x1E - Speed again!?
                // 0x25 - model change

                //Get the objectID for this player
                //IMPORTANT ... this is needed BEFORE
                //sending Packet 0x88!!!

                if (!player.AddToWorld())
                {
                    log.ErrorFormat("Failed to add player to the region! {0}", player.ToString());
                    player.Client?.Out.SendPlayerQuit(true);
                    player.Client?.Player.SaveIntoDatabase();
                    player.Client?.Player.Quit(true);
                    player.Client?.Disconnect();

                    return 0;
                }

                // this is bind stuff
                // make sure that players doesnt start dead when coming in
                // thats important since if client moves the player it requests player creation
                if (player.Health <= 0)
                {
                    player.Health = player.MaxHealth;
                }

                player.Out.SendPlayerPositionAndObjectID();
                player.Out.SendUpdateMaxSpeed();
                //TODO 0xDD - Conc Buffs // 0 0 0 0
                //Now find the friends that are online
                player.Out.SendUpdateMaxSpeed(); // Speed after conc buffs
                player.Out.SendStatusUpdate();
                player.Out.SendInventoryItemsUpdate(EInventoryWindowType.Equipment, player.Inventory.EquippedItems);
                player.Out.SendInventoryItemsUpdate(EInventoryWindowType.Inventory, player.Inventory.GetItemRange(EInventorySlot.FirstBackpack, EInventorySlot.LastBagHorse));
                player.Out.SendUpdatePlayerSkills();   //TODO Insert 0xBE - 08 Various in SendUpdatePlayerSkills() before send spells
                player.Out.SendUpdateCraftingSkills(); // ^
                player.Out.SendUpdatePlayer();
                player.Out.SendUpdateMoney();
                player.Out.SendCharStatsUpdate();
                player.Out.SendCharResistsUpdate();
                int effectsCount = 0;
                player.Out.SendUpdateIcons(null, ref effectsCount);
                player.Out.SendUpdateWeaponAndArmorStats();
                player.Out.SendQuestListUpdate();
                player.Out.SendStatusUpdate();
                player.Out.SendUpdatePoints();
                player.Out.SendConcentrationList();
                // Visual 0x4C - Color Name style (0 0 5 0 0 0 0 0) for RvR or (0 0 5 1 0 0 0 0) for PvP
                // 0xBE - 0 1 0 0
                //used only on PvP, sets THIS players ID for nearest friend/enemy buttons and "friendly" name colors
                //if (GameServer.ServerRules.GetColorHandling(player.Client) == 1) // PvP
                player.Out.SendObjectGuildID(player, player.Guild);
                player.Out.SendDebugMode(player.TempProperties.GetProperty(GamePlayer.DEBUG_MODE_PROPERTY, false));
                player.Out.SendUpdateMaxSpeed(); // Speed in debug mode ?
                                                 //WARNING: This would change problems if a scripter changed the values for plvl
                                                 //GSMessages.SendDebugMode(client,client.Account.PrivLevel>1);
                player.UpdateEncumberance(); // Update encumberance on init.

                // Don't unstealth GMs.
                if (player.Client.Account.PrivLevel > 1)
                    player.GMStealthed = player.IsStealthed;
                else
                    player.Stealth(false);

                player.Out.SendSetControlledHorse(player);
                //check item at world load
                if (log.IsDebugEnabled)
                    log.DebugFormat("Client {0}({1} PID:{2} OID:{3}) entering Region {4}(ID:{5})", player.Client.Account.Name, player.Name, player.Client.SessionID, player.ObjectID, player.CurrentRegion.Description, player.CurrentRegionID);
                return 0;
            }
        }
    }
}
