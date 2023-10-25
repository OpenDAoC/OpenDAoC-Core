using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(1114, EClientVersion.Version1114)]
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