using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PetWindow, "Handle Pet Window Command", eClientStatus.PlayerInGame)]
    public class PetWindowHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            byte aggroState = (byte) packet.ReadByte(); // 1-Aggressive, 2-Defensive, 3-Passive
            byte walkState = (byte) packet.ReadByte(); // 1-Follow, 2-Stay, 3-GoTarget, 4-Here
            byte command = (byte) packet.ReadByte(); // 1-Attack, 2-Release
            GamePlayer player = client.Player;

            switch (aggroState)
            {
                case 0:
                    break; // Ignore.
                case 1:
                {
                    player.CommandNpcAgressive();
                    return;
                }
                case 2:
                {
                    player.CommandNpcDefensive();
                    return;
                }
                case 3:
                {
                    player.CommandNpcPassive();
                    return;
                }
                default:
                {
                    Log.Warn($"unknown aggro state {aggroState}, player={player.Name}  version={player.Client.Version}  client type={player.Client.ClientType}");
                    break;
                }
            }

            switch (walkState)
            {
                case 0:
                    break; // Ignore.
                case 1:
                {
                    player.CommandNpcFollow();
                    break;
                }
                case 2:
                {
                    player.CommandNpcStay();
                    return;
                }
                case 3:
                {
                    player.CommandNpcGoTarget();
                    return;
                }
                case 4:
                {
                    player.CommandNpcComeHere();
                    return;
                }
                default:
                {
                    Log.Warn($"unknown walk state {walkState}, player={player.Name}  version={player.Client.Version}  client type={player.Client.ClientType}");
                    break;
                }
            }

            switch (command)
            {
                case 0:
                    break; // Ignore.
                case 1:
                {
                    player.CommandNpcAttack();
                    return;
                }
                case 2:
                {
                    player.CommandNpcRelease();
                    return;
                }
                default:
                {
                    Log.Warn($"unknown command state {command}, player={player.Name}  version={player.Client.Version}  client type={player.Client.ClientType}");
                    break;
                }
            }
        }
    }
}
