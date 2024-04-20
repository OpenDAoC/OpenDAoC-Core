using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1127, GameClient.eClientVersion.Version1127)]
    public class PacketLib1127 : PacketLib1126
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PacketLib1127(GameClient client) : base(client) { }

        /// 1127 login granted packet unchanged, work around for server type
        public override void SendLoginGranted(byte color)
        {
            // work around for character screen bugs when server type sent as 00 but player doesnt have a realm
            // 0x07 allows for characters in all realms
            using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.LoginGranted)))
            {
                pak.WritePascalString(m_gameClient.Account.Name);
                pak.WritePascalString(GameServer.Instance.Configuration.ServerNameShort); //server name
                pak.WriteByte(0x05); //Server ID, seems irrelevant
                var type = color == 0 ? 7 : color;
                pak.WriteByte((byte)type); // 00 normal type?, 01 mordred type, 03 gaheris type, 07 ywain type
                pak.WriteByte(0x00); // Trial switch 0x00 - subbed, 0x01 - trial acc
                SendTCP(pak);
            }
        }

        public override void SendMessage(string msg, eChatType type, eChatLoc loc)
        {
            if (m_gameClient.ClientState == GameClient.eClientState.CharScreen)
                return;

            GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.Message));
            pak.WriteByte((byte) type);

            string str;
            if (loc == eChatLoc.CL_ChatWindow)
                str = "@@";
            else if (loc == eChatLoc.CL_PopupWindow)
                str = "##";
            else
                str = "";

            pak.WriteString(str + msg);
            SendTCP(pak);
        }
    }
}
