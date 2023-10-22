using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(1107, EClientVersion.Version1107)]
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