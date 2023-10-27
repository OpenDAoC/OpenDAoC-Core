﻿using Core.GS.Enums;

namespace Core.GS.Packets.Server;

[PacketLib(1117, EClientVersion.Version1117)]
public class PacketLib1117 : PacketLib1116
{
	/// <summary>
	/// Constructs a new PacketLib for Client Version 1.117
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib1117(GameClient client)
		: base(client)
	{
	}
}