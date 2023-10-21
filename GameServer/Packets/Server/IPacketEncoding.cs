using Core.GS.Enums;

namespace Core.GS.PacketHandler
{
	public interface IPacketEncoding
	{
		EEncryptionState EncryptionState { get; set; }
		byte[] DecryptPacket(byte[] content, int offset, bool udpPacket);
		byte[] EncryptPacket(byte[] content, int offset, bool udpPacket);
		byte[] SBox { get; set; }
	}
}
