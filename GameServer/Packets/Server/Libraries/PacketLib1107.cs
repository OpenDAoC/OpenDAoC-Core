using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1107, GameClient.eClientVersion.Version1107)]
    public class PacketLib1107 : PacketLib1106
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.107
        /// Untested stub to allow connects
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1107(GameClient client)
            : base(client)
        {

        }
    }
}
