﻿using Core.GS.Enums;

namespace Core.GS.Packets.Server;

[PacketLib(1121, EClientVersion.Version1121)]
public class PacketLib1121 : PacketLib1120
{
	/// <summary>
	/// Constructs a new PacketLib for Client Version 1.121
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib1121(GameClient client)
		: base(client)
	{
	}
}