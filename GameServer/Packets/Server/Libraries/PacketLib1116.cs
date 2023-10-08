namespace DOL.GS.PacketHandler
{
	[PacketLib(1116, GameClient.eClientVersion.Version1116)]
	public class PacketLib1116 : PacketLib1115
	{
		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.116
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1116(GameClient client)
			: base(client)
		{
		}
	}
}
