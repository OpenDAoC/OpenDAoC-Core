using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(1108, EClientVersion.Version1108)]
public class PacketLib1108 : PacketLib1107
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Constructs a new PacketLib for Client Version 1.108
    /// Untested stub to allow connects
    /// </summary>
    /// <param name="client">the gameclient this lib is associated with</param>
    public PacketLib1108(GameClient client)
        : base(client)
    {

    }
}