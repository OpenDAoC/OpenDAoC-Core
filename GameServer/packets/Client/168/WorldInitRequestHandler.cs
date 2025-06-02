using System.Reflection;
using System.Threading.Tasks;
using DOL.Database;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.WorldInitRequest, "Handles world init replies", eClientStatus.LoggedIn)]
    public class WorldInitRequestHandler : IPacketHandler
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client.ClientState is not GameClient.eClientState.CharScreen and not GameClient.eClientState.Playing)
                return;

            // Only load the player if we're coming from the char screen. This will also update the internal cache of the player's skills.
            bool loadPlayer = client.Account.Characters != null && client.ClientState is GameClient.eClientState.CharScreen;

            if (loadPlayer)
                TimerService.ScheduleTimerAfterTask<object>(HandlePacketInternal(client, packet), Continuation, default, null);
            else
                Continuation(default);

            static async Task HandlePacketInternal(GameClient client, GSPacketIn packet)
            {
                byte charIndex = (byte) packet.ReadByte();
                bool charFound = false;
                string selectedChar = string.Empty;
                int realmOffset = charIndex - (client.Account.Realm * 10 - 10);
                int charSlot = client.Account.Realm * 100 + realmOffset;

                for (int i = 0; i < client.Account.Characters.Length; i++)
                {
                    if (client.Account.Characters[i] != null && client.Account.Characters[i].AccountSlot == charSlot)
                    {
                        charFound = true;
                        selectedChar = client.Account.Characters[i].Name;
                        await client.LoadPlayer(i);
                        break;
                    }
                }

                if (!charFound)
                    client.ActiveCharIndex = -1;
                else
                    AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterLogin, "", selectedChar);
            }

            bool Continuation(object unused)
            {
                GamePlayer player = client.Player;

                if (player == null || client.Player.ObjectState is not GameObject.eObjectState.Inactive)
                    return false;

                client.ClientState = GameClient.eClientState.WorldEnter;

                // Check emblems at world load before any updates.
                if (player.Inventory != null)
                {
                    lock (player.Inventory.Lock)
                    {
                        Guild playerGuild = player.Guild;

                        foreach (DbInventoryItem item in player.Inventory.AllItems)
                        {
                            if (item != null && item.Emblem != 0)
                            {
                                if (playerGuild == null || item.Emblem != playerGuild.Emblem)
                                    item.Emblem = 0;
                                if (player.Level < 20)
                                {
                                    if (player.CraftingPrimarySkill == eCraftingSkill.NoCrafting)
                                        item.Emblem = 0;
                                    else
                                    {
                                        if (player.GetCraftingSkillValue(player.CraftingPrimarySkill) < 400)
                                            item.Emblem = 0;
                                    }
                                }
                            }
                        }
                    }
                }

                player.SwitchQuiver((eActiveQuiverSlot) (player.DBCharacter.ActiveWeaponSlot & 0xF0), false);
                player.SwitchWeapon((eActiveWeaponSlot) (player.DBCharacter.ActiveWeaponSlot & 0x0F));

                // 0x88 - Position
                // 0x6D - FriendList
                // 0x15 - Encumbrance update
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
                // 0x15 - Encumbrance update
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
                    if (player.Client != null)
                    {
                        player.Client.Out.SendPlayerQuit(true);
                        player.Client.Player.SaveIntoDatabase();
                        player.Client.Player.Quit(true);
                        player.Client.Disconnect();
                    }

                    return false;
                }

                // this is bind stuff
                // make sure that players don't start dead when coming in
                // that's important since if client moves the player it requests player creation
                if (player.Health <= 0)
                    player.Health = player.MaxHealth;

                player.Out.SendPlayerPositionAndObjectID();
                player.Out.SendUpdateMaxSpeed();
                //TODO 0xDD - Conc Buffs // 0 0 0 0
                //Now find the friends that are online
                player.Out.SendUpdateMaxSpeed(); // Speed after conc buffs
                player.Out.SendStatusUpdate();
                player.Out.SendInventoryItemsUpdate(eInventoryWindowType.Equipment, player.Inventory.EquippedItems);
                player.Out.SendInventoryItemsUpdate(eInventoryWindowType.Inventory, player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBagHorse));
                player.Out.SendUpdatePlayerSkills(loadPlayer);   //TODO Insert 0xBE - 08 Various in SendUpdatePlayerSkills() before send spells
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
                player.Out.SendDebugMode(player.TempProperties.GetProperty<bool>(GamePlayer.DEBUG_MODE_PROPERTY));
                player.Out.SendUpdateMaxSpeed(); // Speed in debug mode ?
                                                    //WARNING: This would change problems if a scripter changed the values for plvl
                                                    //GSMessages.SendDebugMode(client,client.Account.PrivLevel>1);
                player.UpdateEncumbrance(); // Update encumbrance on init.

                // Don't unstealth GMs.
                if (player.Client.Account.PrivLevel > 1)
                    player.GMStealthed = player.IsStealthed;
                else
                    player.Stealth(false);

                player.Out.SendSetControlledHorse(player);

                if (log.IsDebugEnabled)
                    log.DebugFormat($"Client {player.Client.Account.Name}({player.Name} PID:{player.Client.SessionID} OID:{player.ObjectID}) entering Region {player.CurrentRegion.Description}(ID:{player.CurrentRegionID})");

                return true;
            }
        }
    }
}
