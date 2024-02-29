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
                timer.SetResponse(response); // Let the timer service invoke the callback.
            }
        }

        public class TimeoutTimer : ECSGameTimerWrapperBase
        {
            private ushort _sourceObjectId;
            private ushort _targetObjectId;
            private eLosCheckResponse _response;
            public CheckLosResponse LosCheckCallback { get; set; }

            public TimeoutTimer(GamePlayer owner, CheckLosResponse losCheckCallback, ushort sourceObjectId, ushort targetObjectId) : base(owner)
            {
                LosCheckCallback = losCheckCallback;
                _sourceObjectId = sourceObjectId;
                _targetObjectId = targetObjectId;
                Interval = ServerProperties.Properties.LOS_CHECK_TIMEOUT;
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
                LosCheckCallback(player, response, _sourceObjectId, _targetObjectId);
                return 0;
            }
        }
    }
}
