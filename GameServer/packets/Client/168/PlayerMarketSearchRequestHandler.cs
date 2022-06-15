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
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Housing;
using System.Text;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.MarketSearchRequest, "Handles player market search", eClientStatus.PlayerInGame)]
	public class PlayerMarketSearchRequestHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			if (client == null || client.Player == null)
				return;

			if ((client.Player.TargetObject is IGameInventoryObject) == false)
				return;

			for (int i = 0; i < packet.ToArray().Length; i++)
			{
				Console.WriteLine((i + 1) + ") " + packet.ToArray()[i].ToString("X") + " ");
			}
			var searchOffset = packet.ReadByte();
			packet.Skip(3); // 4 bytes unused

			MarketSearch.SearchData search = new MarketSearch.SearchData();
			//{
			//	name = packet.ReadString(64),
			//	packet.Skip(1),
			//	slot = (int)packet.ReadByte(),
			//	skill = (int)packet.ReadInt(),
			//	resist = (int)packet.ReadInt(),
			//	bonus = (int)packet.ReadInt(),
			//	hp = (int)packet.ReadInt(),
			//	power = (int)packet.ReadInt(),
			//	proc = (int)packet.ReadInt(),
			//	qtyMin = (int)packet.ReadInt(),
			//	qtyMax = (int)packet.ReadInt(),
			//	levelMin = (int)packet.ReadInt(),
			//	levelMax = (int)packet.ReadInt(),
			//	priceMin = (int)packet.ReadInt(),
			//	priceMax = (int)packet.ReadInt(),
			//	visual = (int)packet.ReadInt(),
			//	page = (byte)packet.ReadByte()
			//};

			search.name = packet.ReadString(searchOffset);
			//packet.Skip(1);
			search.slot = (int)packet.ReadByte();
			//search.skill = (int)packet.ReadInt();
			//search.resist = (int)packet.ReadInt();
			//search.bonus = (int)packet.ReadInt();
			var bonus1 = packet.ReadByte();
			var bonus1b = packet.ReadByte();
			search.bonus1 = bonus1b * 256 + bonus1;

			var bonus1Value = (int)packet.ReadByte();
			var bonus1bValue = (int)packet.ReadByte();
			search.bonus1Value = bonus1bValue * 256 + bonus1Value;

			var bonus2 = (short)packet.ReadByte();
			var bonus2b = packet.ReadByte();
			search.bonus2 = bonus2b * 256 + bonus2;

			var bonus2Value = (short)packet.ReadByte();
			var bonus2bValue = (int)packet.ReadByte();
			search.bonus2Value = bonus2bValue * 256 + bonus2Value;

			var bonus3 = (short)packet.ReadByte();
			var bonus3b = packet.ReadByte();
			search.bonus3 = bonus3b * 256 + bonus3;

			var bonus3Value = (short)packet.ReadByte();
			var bonus3bValue = (int)packet.ReadByte();
			search.bonus2Value = bonus2bValue * 256 + bonus2Value;

			search.proc = (int)packet.ReadByte();
			packet.Skip(1);
			//short unk2 = (short)packet.ReadShort();
			search.armorType = (byte)packet.ReadByte();
			search.damageType = (byte)packet.ReadByte(); // 1=crush, 2=slash, 3=thrust
			search.levelMin = (byte)packet.ReadByte();
			search.levelMax = (byte)packet.ReadByte();
			search.qtyMin = (byte)packet.ReadByte();

            var priceMin1 = packet.ReadByte();
            var priceMin1b = packet.ReadByte();
            priceMin1b = priceMin1b != 0 ? priceMin1b : 1;
            var priceMin1c = packet.ReadByte();
            priceMin1c = priceMin1c != 0 ? priceMin1c : 1;
            var priceMin1d = packet.ReadByte();
            priceMin1d = priceMin1d != 0 ? priceMin1d : 1;
            priceMin1d = priceMin1b == 1 && priceMin1c == 1 && priceMin1d == 1 ? 0 : priceMin1d;
            search.priceMin = (uint)(priceMin1b * priceMin1c * priceMin1d * 256 + priceMin1);

            var priceMax1 = packet.ReadByte();
            var priceMax1b = packet.ReadByte();
            priceMax1b = priceMax1b != 0 ? priceMax1b : 1;
            var priceMax1c = packet.ReadByte();
            priceMax1c = priceMax1c != 0 ? priceMax1c : 1;
            var priceMax1d = packet.ReadByte();
            priceMax1d = priceMax1d != 0 ? priceMax1d : 1;
			priceMax1d = priceMax1b == 1 && priceMax1c == 1 && priceMax1d == 1 ? 0 : priceMax1d;
			search.priceMax = (uint)(priceMax1b * priceMax1c * priceMax1d * 256 + priceMax1);


            search.playerCrafted = (byte)packet.ReadByte(); // 1 = show only Player crafted, 0 = all
			search.visual = (int)packet.ReadByte();
			search.page = (byte)packet.ReadByte();


			//search.hp = (int)packet.ReadInt();
			//search.power = (int)packet.ReadInt();
					
			//search.qtyMax = (int)packet.ReadInt();
			
				
			


			//byte unk1 = (byte)packet.ReadByte();
			////short unk2 = (short)packet.ReadShort();

			//// Dunnerholl 2009-07-28 Version 1.98 introduced new options to Market search. 12 Bytes were added, but only 7 are in usage so far in my findings.
			//// update this, when packets change and keep in mind, that this code reflects only the 1.98 changes
			//search.armorType = search.page; // page is now used for the armorType (still has to be logged, i just checked that 2 means leather, 0 = standard
			
			//byte unk3 = (byte)packet.ReadByte();
			//byte unk4 = (byte)packet.ReadByte();
			//byte unk5 = (byte)packet.ReadByte();
			
			////packet.Skip(3); // 3 bytes unused
			//search.page = (byte)packet.ReadByte(); // page is now sent here
			//byte unk6 = (byte)packet.ReadByte();
			//byte unk7 = (byte)packet.ReadByte();
			//byte unk8 = (byte)packet.ReadByte();

			search.clientVersion = client.Version.ToString();
			Console.WriteLine(search);
			(client.Player.TargetObject as IGameInventoryObject).SearchInventory(client.Player, search);
		}
	}
}