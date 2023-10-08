namespace DOL.GS.PacketHandler
{
	[PacketLib(1122, GameClient.eClientVersion.Version1122)]
	public class PacketLib1122 : PacketLib1121
	{
		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.122
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1122(GameClient client)
			: base(client)
		{
		}
	}
}
