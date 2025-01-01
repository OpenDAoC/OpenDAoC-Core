using System.Collections.Generic;
using System.Threading;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CheckLosRequest, "Handles a LoS Check Response", eClientStatus.PlayerInGame)]
    public class CheckLosResponseHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            ushort checkerObjectId = packet.ReadShort();
            ushort targetObjectId = packet.ReadShort();

            if (client.Player.LosCheckTimers.TryGetValue((checkerObjectId, targetObjectId), out TimeoutTimer timer))
            {
                eLosCheckResponse response = (packet.ReadShort() & 0x100) == 0x100 ? eLosCheckResponse.TRUE : eLosCheckResponse.FALSE;
                packet.ReadShort();
                timer.SetResponse(response); // Let the timer service invoke the callbacks.
            }
        }

        public class TimeoutTimer : ECSGameTimerWrapperBase
        {
            private ushort _sourceObjectId;
            private ushort _targetObjectId;
            private eLosCheckResponse _response;
            private CheckLosResponse _firstCallback;
            private readonly Lock _lock = new();
            public List<CheckLosResponse> Callbacks { get; private set; }

            public TimeoutTimer(GamePlayer owner, CheckLosResponse callback, ushort sourceObjectId, ushort targetObjectId) : base(owner)
            {
                _firstCallback = callback;
                _sourceObjectId = sourceObjectId;
                _targetObjectId = targetObjectId;
                Interval = ServerProperties.Properties.LOS_CHECK_TIMEOUT;
            }

            public bool TryAddCallback(CheckLosResponse callback)
            {
                // For performance reasons, we only instantiate `Callbacks` if we need to handle more than one callback.
                // Most of the time, there will be only one.
                // If `Callbacks` isn't null but is empty, then it means the timer already ticked.
                lock (_lock)
                {
                    if (Callbacks == null)
                    {
                        Callbacks ??= [];
                        Callbacks.Add(callback);
                    }
                    else if (Callbacks.Count != 0)
                        Callbacks.Add(callback);
                    else
                        return false;

                    return true;
                }
            }

            public void SetResponse(eLosCheckResponse response)
            {
                _response = response;
                NextTick = 0;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                GamePlayer player = Owner as GamePlayer;
                player.LosCheckTimers.TryRemove((_sourceObjectId, _targetObjectId), out _);
                eLosCheckResponse response = _response is eLosCheckResponse.NONE ? eLosCheckResponse.TIMEOUT : _response; // `_response` can be modified when this is being called.
                _firstCallback(player, response, _sourceObjectId, _targetObjectId);

                lock (_lock)
                {
                    if (Callbacks != null)
                    {
                        foreach (CheckLosResponse callback in Callbacks)
                            callback(player, response, _sourceObjectId, _targetObjectId);

                        Callbacks.Clear();
                    }
                }

                return 0;
            }
        }
    }
}
