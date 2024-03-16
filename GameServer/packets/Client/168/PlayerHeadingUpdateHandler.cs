using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerHeadingUpdate, "Handles Player Heading Update (Short State)", eClientStatus.PlayerInGame)]
	public class PlayerHeadingUpdateHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			if (client?.Player == null)
				return;
			if (client.Player.ObjectState != GameObject.eObjectState.Active)
				return;

			ushort sessionId = packet.ReadShort(); // session ID
			if (client.SessionID != sessionId)
			{
//				GameServer.BanAccount(client, 120, "Hack sessionId", string.Format("Wrong sessionId:0x{0} in 0xBA packet (SessionID:{1})", sessionId, client.SessionID));
				return; // client hack
			}

			if (client.Version >= GameClient.eClientVersion.Version1127)
				packet.ReadShort(); // target

			ushort head = packet.ReadShort();
			var unk1 = (byte)packet.ReadByte(); // unknown
			var flags = (byte)packet.ReadByte();
			var steedSlot = (byte) packet.ReadByte();
			var ridingFlag = (byte)packet.ReadByte();

			client.Player.Heading = (ushort)(head & 0xFFF);
			// client.Player.PetInView = ((flags & 0x04) != 0); // TODO
			client.Player.GroundTargetInView = ((flags & 0x08) != 0);
			if(!client.Player.IsCasting)client.Player.TargetInView = ((flags & 0x10) != 0);

			byte state = 0;
			if (!client.Player.IsAlive)
				state = 5; // set dead state
			else if (client.Player.Steed != null && client.Player.Steed.ObjectState == GameObject.eObjectState.Active)
			{
				client.Player.Heading = client.Player.Steed.Heading;
				state = 6; // Set ride state
				steedSlot = (byte)client.Player.Steed.RiderSlot(client.Player); // there rider slot this player
				head = (ushort)client.Player.Steed.ObjectID; // heading = steed ID
			}
			else
			{
				if (client.Player.IsSwimming)
					state = 1;
				if (client.Player.IsClimbing)
					state = 7;
				if (client.Player.IsSitting)
					state = 4;
				if (client.Player.IsStrafing)
					state |= 8;
			}

			byte flagcontent = 0;
			if (client.Player.IsWireframe)
				flagcontent |= 0x01;
			if (client.Player.IsStealthed)
				flagcontent |= 0x02;
			if (client.Player.IsDiving)
				flagcontent |= 0x04;
			if (client.Player.IsTorchLighted)
				flagcontent |= 0x80;

			GSUDPPacketOut outpak190 = new GSUDPPacketOut(client.Out.GetPacketCode(eServerPackets.PlayerHeading));
			outpak190.WriteShort((ushort) client.SessionID);
			outpak190.WriteShort(head);
			outpak190.WriteByte(unk1); // unknown
			outpak190.WriteByte(flagcontent);
			outpak190.WriteByte(steedSlot);
			outpak190.WriteByte(ridingFlag);
			outpak190.WriteByte((byte)(client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
			outpak190.WriteByte(state);
			outpak190.WriteByte(client.Player.ManaPercent);
			outpak190.WriteByte(client.Player.EndurancePercent);

			GSUDPPacketOut outpak1124 = new GSUDPPacketOut(client.Out.GetPacketCode(eServerPackets.PlayerHeading));
			outpak1124.WriteShort((ushort)client.SessionID);
			outpak1124.WriteShort(head);
			outpak1124.WriteByte(steedSlot);
			outpak1124.WriteByte(flagcontent);
			outpak1124.WriteByte(0);
			outpak1124.WriteByte(ridingFlag);
			outpak1124.WriteByte((byte)(client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
			outpak1124.WriteByte(client.Player.ManaPercent);
			outpak1124.WriteByte(client.Player.EndurancePercent);
			outpak1124.WriteByte(0); // unknown

			GSUDPPacketOut outpak1127 = new GSUDPPacketOut(client.Out.GetPacketCode(eServerPackets.PlayerHeading));
			outpak1127.WriteShort((ushort)client.SessionID);
			outpak1127.WriteShort(0); // current target
			outpak1127.WriteShort(head);
			outpak1127.WriteByte(steedSlot);
			outpak1127.WriteByte(flagcontent);
			outpak1127.WriteByte(0);
			outpak1127.WriteByte(ridingFlag);
			outpak1127.WriteByte((byte)(client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
			outpak1127.WriteByte(client.Player.ManaPercent);
			outpak1127.WriteByte(client.Player.EndurancePercent);
			outpak1127.WriteByte(0); // unknown

			foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if(player != null && player != client.Player)
				{
					if (client.Player.IsStealthed && !player.CanDetect(client.Player))
						return;
					if (player.Client.Version >= GameClient.eClientVersion.Version1127)
						player.Out.SendUDP(outpak1127);
					else if (player.Client.Version >= GameClient.eClientVersion.Version1124)
						player.Out.SendUDP(outpak1124);
					else
						player.Out.SendUDP(outpak190);
				}
			}
		}
	}
}
