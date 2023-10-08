using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1106, GameClient.eClientVersion.Version1106)]
    public class PacketLib1106 : PacketLib1105
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.106
        /// Untested stub to allow connects
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1106(GameClient client)
            : base(client)
        {

        }
    }
}
