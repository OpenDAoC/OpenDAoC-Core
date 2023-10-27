﻿using Core.GS.Enums;

namespace Core.GS.Packets.Server;

[PacketLib(1128, EClientVersion.Version1128)]
public class PacketLib1128 : PacketLib1127
{
    public PacketLib1128(GameClient client) : base(client) { }

    public override void SendVersionAndCryptKey()
    {
        //Construct the new packet
        using (var pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.CryptKey)))
        {
            pak.WriteIntLowEndian(0); // Disable encryption (1110+ always encrypts).
            pak.WriteString($"{(int) m_gameClient.Version / 1000}.{(int) m_gameClient.Version - 1000}", 5); // Version.
            pak.WriteByte(0x00); // Revision.
            pak.WriteByte(0x00); // Always 0?
            pak.WriteByte(0x00); // Build number.
            pak.WriteByte(0x00); // Build number.
            SendTCP(pak);
            m_gameClient.PacketProcessor.ProcessTcpQueue();
        }
    }
}