using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1114, GameClient.eClientVersion.Version1114)]
    public class PacketLib1114 : PacketLib1113
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.114
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1114(GameClient client)
            : base(client)
        {

        }
    }
}
