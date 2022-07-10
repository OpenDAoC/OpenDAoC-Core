/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using DOL.Events;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandler(PacketHandlerType.TCP, eClientPackets.CharacterSelectRequest, "Handles setting SessionID and the active character", eClientStatus.LoggedIn)]
    public class CharacterSelectRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client.Version >= GameClient.eClientVersion.Version1125 && client.MinorRev == "d") // 1125d support TODO this needs to be changed when a version greater than 1125d comes out
            {
                var charSelectRequest = new CharacterSelectRequestHandler1125d();
                charSelectRequest.HandlePacket(client, packet);
                return;
            }
            else if (client.Version >= GameClient.eClientVersion.Version1126)
            {
                var charSelectRequest = new CharacterSelectRequestHandler1126();
                charSelectRequest.HandlePacket(client, packet);
                return;
            }

            packet.Skip(4); // Skip the first 4 bytes
            packet.Skip(1);

            string charName = packet.ReadString(28);

            // TODO Character handling
            if (charName.Equals("noname"))
            {
                client.Out.SendSessionID();
            }
            else
            {
                // SH: Also load the player if client player is NOT null but their charnames differ!!!
                // only load player when on charscreen and player is not loaded yet
                // packet is sent on every region change (and twice after "play" was pressed)
                if (((client.Player == null && client.Account.Characters != null) || (client.Player != null && client.Player.Name.ToLower() != charName.ToLower())) && client.ClientState == GameClient.eClientState.CharScreen)
                {
                    bool charFound = false;
                    for (int i = 0; i < client.Account.Characters.Length; i++)
                    {
                        if (client.Account.Characters[i] != null
                            && client.Account.Characters[i].Name == charName)
                        {
                            charFound = true;

                            // Notify Character Selection Event, last hope to fix any bad data before Loading.
                            GameEventMgr.Notify(DatabaseEvent.CharacterSelected, new CharacterEventArgs(client.Account.Characters[i], client));
                            client.LoadPlayer(i);
                            break;
                        }
                    }

                    if (charFound == false)
                    {
                        client.Player = null;
                        client.ActiveCharIndex = -1;
                    }
                    else
                    {
                        // Log character play
                        AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterLogin, string.Empty, charName);
                    }
                }

                client.Out.SendSessionID();
            }
        }
    }

    /// <summary>
    /// 1125d + support
    /// </summary>
    public class CharacterSelectRequestHandler1125d : IPacketHandler // 1125d support TODO duplicate code from above, can be reused
    {        
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            
            byte type = (byte)packet.ReadByte(); // changed from ushort low end
                        
            packet.Skip(1); // unknown

            string charName = packet.ReadString(24); // down from 28, both need checking
                        
            //TODO Character handling 
            if (charName.Equals("noname"))
            {                
                client.Out.SendLoginGranted();
                client.Out.SendSessionID();
            }
            else
            {
                // SH: Also load the player if client player is NOT null but their charnames differ!!!
                // only load player when on charscreen and player is not loaded yet
                // packet is sent on every region change (and twice after "play" was pressed)
                if (((client.Player == null && client.Account.Characters != null) || (client.Player != null && client.Player.Name.ToLower() != charName.ToLower())) 
                    && client.ClientState == GameClient.eClientState.CharScreen)
                {
                    bool charFound = false;
                    for (int i = 0; i < client.Account.Characters.Length; i++)
                    {
                        if (client.Account.Characters[i] != null && client.Account.Characters[i].Name == charName)
                        {
                            charFound = true;
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
                    {
                        // Log character play
                        AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterLogin, "", charName);
                    }
                }
                
                // live actually sends the login granted packet, which sets the button activity states
                client.Out.SendLoginGranted();
                client.Out.SendSessionID();
            }
        }
    }

    /// <summary>
    /// 1126 support. Packet changed again from version 1125d
    /// Alot of test code i slapped together to get it to work, likely can be changed
    /// </summary>
    public class CharacterSelectRequestHandler1126 : IPacketHandler
    {        
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            byte charIndex = (byte)packet.ReadByte(); // character account location

            // some funkyness going on below here. Could use some safeguards to ensure a character is loaded correctly
            if (client.Player == null && client.Account.Characters != null && client.ClientState == GameClient.eClientState.CharScreen)
            {                
                bool charFound = false;
                string selectedChar = "";
                int realmOffset = charIndex - (client.Account.Realm * 10 - 10);
                int charSlot = (client.Account.Realm * 100) + realmOffset;
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
                {
                    // Log character play
                    AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterLogin, "", selectedChar);
                }
            }

            client.Out.SendSessionID();
        }
    }
}