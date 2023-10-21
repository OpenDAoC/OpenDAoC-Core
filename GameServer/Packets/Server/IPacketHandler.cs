namespace Core.GS.Packets;

/// <summary>
/// The interface for all received packets
/// </summary>
public interface IPacketHandler
{
	/// <summary>
	/// Handles every received packet
	/// </summary>
	/// <param name="client">The client that sent the packet</param>
	/// <param name="packet">The received packet data</param>
	/// <returns></returns>
	void HandlePacket(GameClient client, GsPacketIn packet);
}