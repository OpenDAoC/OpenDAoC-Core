namespace DOL.GS.PacketHandler
{
	[PacketLib(1123, GameClient.eClientVersion.Version1123)]
	public class PacketLib1123 : PacketLib1122
	{
		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.123
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1123(GameClient client)
			: base(client)
		{
		}
	}
}
