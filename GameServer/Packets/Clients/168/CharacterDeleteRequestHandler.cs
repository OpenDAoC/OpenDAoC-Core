using System;
using System.Linq;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Packets.Server;
using log4net;

namespace Core.GS.Packets.Clients;

/// <summary>
/// No longer used after version 1.104
/// </summary>
[PacketHandler(EPacketHandlerType.TCP, EClientPackets.CharacterDeleteRequest, "Handles character delete requests", EClientStatus.LoggedIn)]
public class CharacterDeleteRequestHandler : IPacketHandler
{
	private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		string charName = packet.ReadString(30);
		DbCoreCharacter[] chars = client.Account.Characters;

		var foundChar = chars?.FirstOrDefault(ch => ch.Name.Equals(charName, StringComparison.OrdinalIgnoreCase));
		if (foundChar != null)
		{
			var slot = foundChar.AccountSlot;
			CharacterCreateRequestHandler.CheckForDeletedCharacter(foundChar.AccountName, client, slot);
		}
	}
}