using System.Reflection;
using DOL.Logging;
using DOL.Timing;
using ECS.Debug;

namespace DOL.GS.PacketHandler
{
    public interface IPacketHandler
    {
        void HandlePacket(GameClient client, GSPacketIn packet);
    }

    public abstract class PacketHandler : IPacketHandler
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            long startTick = MonotonicTime.NowMs;
            HandlePacketInternal(client, packet);
            long stopTick = MonotonicTime.NowMs;

            if (log.IsWarnEnabled)
            {
                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                {
                    string context = GetLogContext(client, packet);
                    string contextMsg = string.IsNullOrEmpty(context) ? string.Empty : $" Context: {context}";
                    log.Warn($"Long {nameof(PacketHandler)}.{nameof(HandlePacket)} ({(eClientPackets)packet.Code}) for {client.Player?.Name}({client.Player?.ObjectID}){contextMsg} Time: {stopTick - startTick}ms");
                }
            }
        }

        protected abstract void HandlePacketInternal(GameClient client, GSPacketIn packet);

        protected virtual string GetLogContext(GameClient client, GSPacketIn packet)
        {
            return null;
        }
    }
}
