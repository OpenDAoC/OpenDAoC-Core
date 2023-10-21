using System.Reflection;
using DOL.GS.PlayerTitles;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1113, GameClient.eClientVersion.Version1113)]
    public class PacketLib1113 : PacketLib1112
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.112
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1113(GameClient client)
            : base(client)
        {

        }

        public override void SendPlayerTitles()
        {
            var titles = m_gameClient.Player.Titles;

            using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.DetailWindow)))
            {

                pak.WriteByte(1); // new in 1.75
                pak.WriteByte(0); // new in 1.81
                pak.WritePascalString("Player Statistics"); //window caption

                byte line = 1;
                foreach (string str in m_gameClient.Player.FormatStatistics())
                {
                    pak.WriteByte(line++);
                    pak.WritePascalString(str);
                }

                pak.WriteByte(200);
                long titlesCountPos = pak.Position;
                pak.WriteByte((byte)titles.Count);
                line = 0;

                foreach (IPlayerTitle title in titles)
                {
                    pak.WriteByte(line++);
                    pak.WritePascalString(title.GetDescription(m_gameClient.Player));
                }

                long titlesLen = (pak.Position - titlesCountPos - 1); // include titles count
                if (titlesLen > byte.MaxValue)
                    log.WarnFormat("Titles block is too long! {0} (player: {1})", titlesLen, m_gameClient.Player);

                //Trailing Zero!
                pak.WriteByte(0);
                SendTCP(pak);
            }
        }
    }
}
