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

using log4net;
using System;
using System.Reflection;

namespace DOL.GS.PacketHandler
{
	[PacketLib(1127, GameClient.eClientVersion.Version1127)]
	public class PacketLib1127 : PacketLib1126
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.127
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1127(GameClient client)
			: base(client)
		{
		}

		long m_lastPacketSendTick = 0;
		long m_packetInterval = 500; //.5s
		int m_numPacketsSent = 0;
		int m_packetCap = 10; // packets sent every packetInterval

		public override void SendMessage(string msg, eChatType type, eChatLoc loc)
		{
			if (m_gameClient.ClientState == GameClient.eClientState.CharScreen)
				return;

			GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.Message));
			pak.WriteByte((byte) type);

			string str;
			if (loc == eChatLoc.CL_ChatWindow)
				str = "@@";
			else if (loc == eChatLoc.CL_PopupWindow)
				str = "##";
			else
				str = "";

			if (m_lastPacketSendTick + m_packetInterval < GameLoop.GameLoopTime)
			{
				m_numPacketsSent = 0;
			}

			//rate limit spell and damage messages
			//if (type == eChatType.CT_Spell || type == eChatType.CT_Damaged)
			//{
			//	if (m_numPacketsSent < m_packetCap)
			//	{
			//		pak.WriteString(str + msg);
			//		SendTCP(pak);
			//		m_numPacketsSent++;
			//		m_lastPacketSendTick = GameLoop.GameLoopTime;
			//	}				
			//} else
   //         {
				pak.WriteString(str + msg);
				SendTCP(pak);
			//}
			
		}
	}
}