namespace DOL.GS.PacketHandler
{
	[PacketLib(1120, GameClient.eClientVersion.Version1120)]
	public class PacketLib1120 : PacketLib1119
	{
		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.120
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1120(GameClient client)
			: base(client)
		{
		}
	}
}
