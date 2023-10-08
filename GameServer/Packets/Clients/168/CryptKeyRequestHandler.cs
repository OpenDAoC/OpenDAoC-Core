using DOL.GS.ServerProperties;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.CryptKeyRequest, "Handles crypt key requests", EClientStatus.None)]
	public class CryptKeyRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			// for 1.115c+ The First client packet Changes.
			if (client.Version < GameClient.eClientVersion.Version1115)
			{
				int rc4 = packet.ReadByte();
				byte clientType = (byte)packet.ReadByte();
				client.ClientType = (GameClient.eClientType)(clientType & 0x0F);
				client.ClientAddons = (GameClient.eClientAddons)(clientType & 0xF0);
				client.MajorBuild = (byte)packet.ReadByte();
				client.MinorBuild = (byte)packet.ReadByte();
				client.MinorRev = packet.ReadString(1);
				if (rc4 == 1)
				{
					//DOLConsole.Log("SBox=\n");
					//DOLConsole.LogDump(client.PacketProcessor.Encoding.SBox);
					packet.Read(client.PacketProcessor.Encoding.SBox, 0, 256);
					client.PacketProcessor.Encoding.EncryptionState = EEncryptionState.PseudoRC4Encrypted;
					//DOLConsole.WriteLine(client.Socket.RemoteEndPoint.ToString()+": SBox set!");
					//DOLConsole.Log("SBox=\n");
					//DOLConsole.LogDump(((PacketEncoding168)client.PacketProcessor.Encoding).SBox);
				}
				else
				{
				  //Send the crypt key to the client
					client.Out.SendVersionAndCryptKey();
				}
			}
			else
			{
				// if the DataSize is above 7 then the RC4 key is bundled
				if (packet.DataSize > 7)
				{
					if (Properties.CLIENT_ENABLE_ENCRYPTION_RC4)
					{
						var length = packet.ReadIntLowEndian();
						var key = new byte[length];
						packet.Read(key, 0, (int) length);
						client.PacketProcessor.Encoding.SBox = key;
						client.PacketProcessor.Encoding.EncryptionState = EEncryptionState.PseudoRC4Encrypted;
					}
					return;
				}

				// register client type
				byte clientType = (byte)packet.ReadByte();
				client.ClientType = (GameClient.eClientType)(clientType & 0x0F);
				client.ClientAddons = (GameClient.eClientAddons)(clientType & 0xF0);
				// the next 4 bytes are the game.dll version but not in string form
				// ie: 01 01 19 61 = 1.125a
				// this version is handled elsewhere before being sent here.
				packet.Skip(3); // skip the numbers in the version
				client.MinorRev = packet.ReadString(1); // get the minor revision letter // 1125d support
				packet.Skip(2); // build


				//Send the crypt key to the client
				client.Out.SendVersionAndCryptKey();
			}
		}
	}
}