using System;
using System.Linq;
using DOL.Database;

namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// No longer used after version 1.104
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CharacterDeleteRequest, "Handles character delete requests", eClientStatus.LoggedIn)]
    public class CharacterDeleteRequestHandler : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            string charName = packet.ReadString(30);
            DbCoreCharacter[] chars = client.Account.Characters;
            DbCoreCharacter foundChar = chars?.FirstOrDefault(ch => ch.Name.Equals(charName, StringComparison.OrdinalIgnoreCase));

            if (foundChar == null)
                return;

            int slot = foundChar.AccountSlot;
            CharacterCreateRequestHandler.HandleDeleteCharacterRequest(foundChar.AccountName, client, slot);
        }
    }
}
