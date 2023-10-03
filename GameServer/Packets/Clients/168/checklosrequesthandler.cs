/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(PacketHandlerType.TCP, eClientPackets.CheckLOSRequest, "Handles a LoS Check Response", eClientStatus.PlayerInGame)]
	public class CheckLOSResponseHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			ushort checkerOID = packet.ReadShort();
			ushort targetOID = packet.ReadShort();
			ushort response = packet.ReadShort();
			packet.ReadShort();
			new HandleCheckAction(client.Player, checkerOID, targetOID, response).Start(1);
			// LOSResponseHandler(client.Player, checkerOID, targetOID, response);
		}

		// private void LOSResponseHandler(GamePlayer m_actionSource, int m_checkerOid, int m_targetOid, int m_response)
		// {
		// 	// Check for Old Callback first

		// 	string key = $"LOS C:0x{m_checkerOid} T:0x{m_targetOid}";

		// 	GamePlayer player = (GamePlayer)m_actionSource;

		// 	CheckLOSResponse callback = player.TempProperties.getProperty<CheckLOSResponse>(key, null);
		// 	if (callback != null)
		// 	{
		// 		callback(player, (ushort)m_response, (ushort)m_targetOid);
		// 		player.TempProperties.removeProperty(key);
		// 	}

		// 	string newkey = $"LOSMGR C:0x{m_checkerOid} T:0x{m_targetOid}";

		// 	CheckLOSMgrResponse new_callback = player.TempProperties.getProperty<CheckLOSMgrResponse>(newkey, null);

		// 	if (new_callback != null)
		// 	{
		// 		new_callback(player, (ushort)m_response, (ushort)m_checkerOid, (ushort)m_targetOid);
		// 		player.TempProperties.removeProperty(newkey);
		// 	}
		// }

		/// <summary>
		/// Handles the LOS check response
		/// </summary>
		protected class HandleCheckAction : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// The LOS source OID
			/// </summary>
			protected readonly int m_checkerOid;

			/// <summary>
			/// The request response
			/// </summary>
			protected readonly int m_response;

			/// <summary>
			/// The LOS target OID
			/// </summary>
			protected readonly int m_targetOid;

			/// <summary>
			/// Constructs a new HandleCheckAction
			/// </summary>
			/// <param name="actionSource">The player received the packet</param>
			/// <param name="checkerOid">The LOS source OID</param>
			/// <param name="targetOid">The LOS target OID</param>
			/// <param name="response">The request response</param>
			public HandleCheckAction(GamePlayer actionSource, int checkerOid, int targetOid, int response) : base(actionSource)
			{
				m_checkerOid = checkerOid;
				m_targetOid = targetOid;
				m_response = response;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				// Check for Old Callback first
				GamePlayer player = (GamePlayer) timer.Owner;

				string key = $"LOS C:0x{m_checkerOid} T:0x{m_targetOid}";

				if (player.TempProperties.RemoveAndGetProperty(key, out object callback))
				{
					if (callback is CheckLOSResponse checkLOSResponseCallback)
						checkLOSResponseCallback(player, (ushort)m_response, (ushort)m_targetOid);
				}

				string newkey = $"LOSMGR C:0x{m_checkerOid} T:0x{m_targetOid}";
				
				if (player.TempProperties.RemoveAndGetProperty(newkey, out object newCallback))
				{
					if (newCallback is CheckLOSMgrResponse checkLOSResponseNewCallback)
						checkLOSResponseNewCallback(player, (ushort)m_response, (ushort)m_checkerOid, (ushort)m_targetOid);
				}

				return 0;
			}
		}
	}
}
