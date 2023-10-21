namespace DOL.GS.PacketHandler
{
	[PacketLib(1118, GameClient.eClientVersion.Version1118)]
	public class PacketLib1118 : PacketLib1117
	{
		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.118
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1118(GameClient client)
			: base(client)
		{
		}
	}
}
