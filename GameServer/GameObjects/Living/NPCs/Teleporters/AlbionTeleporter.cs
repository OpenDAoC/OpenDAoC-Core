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
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	/// <summary>
	/// Albion teleporter.
	/// </summary>
	/// <author>Aredhel</author>
	public class AlbionTeleporter : GameTeleporter
	{
		/// <summary>
		/// Add equipment to the teleporter.
		/// </summary>
		/// <returns></returns>
		public override bool AddToWorld()
		{
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(EInventorySlot.Cloak, 57, 66);
			template.AddNPCEquipment(EInventorySlot.TorsoArmor, 1005, 86);
			template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 6);
			template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 6);
			template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 6);
			template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 6);
			template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166);
			Inventory = template.CloseTemplate();

			SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			return base.AddToWorld();
		}

		/// <summary>
		/// Player right-clicked the teleporter.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player) || GameRelic.IsPlayerCarryingRelic(player)) return false;

			TurnTo(player, 10000);
			
			SayTo(player, "Greetings, " + player.Name +
			              " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
			              "[Castle Sauvage] in Camelot Hills or \n[Snowdonia Fortress] in Black Mtns. North\n" +
			              "[Avalon Marsh] wharf\n" +
			              "[Gothwaite Harbor] in the [Shrouded Isles]\n" +
			              "[Camelot] our glorious capital\n" +
			              "[Entrance] to the areas of [housing]\n\n" +
			              "Or one of the many [towns] throughout Albion");
			
			return true;
		}

		/// <summary>
		/// Player has picked a subselection.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="subSelection"></param>
		protected override void OnSubSelectionPicked(GamePlayer player, DbTeleport subSelection)
		{
			switch (subSelection.TeleportID.ToLower())
			{
				case "shrouded isles":
					{
						String reply = String.Format("The isles of Avalon are an excellent choice. {0} {1}",
							"Would you prefer [Gothwaite] or perhaps one of the outlying towns",
							"like [Wearyall Village], Fort [Gwyntell], or [Caer Diogel]?");
						SayTo(player, reply);
						break;
					}
				
				case "housing":
					{
						SayTo(player,
							"I can send you to your [personal] or [guild] house. If you do not have a personal house, I can teleport you to the housing [entrance] or your housing [hearth] bindstone.");
						return;
					}
				
				case "towns":
				{
					SayTo(player, "I can send you to:\n" +
					              "[Cotswold Village]\n" +
					              "[Prydwen Keep]\n" +
					              "[Caer Ulfwych]\n" +
					              "[Campacorentin Station]\n" +
					              "[Adribard's Retreat]\n" +
					              "[Yarley's Farm]");
					return;
				}
			}
			base.OnSubSelectionPicked(player, subSelection);
		}

		/// <summary>
		/// Player has picked a destination.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="destination"></param>
		protected override void OnDestinationPicked(GamePlayer player, DbTeleport destination)
		{
			
			Region region = WorldMgr.GetRegion((ushort) destination.RegionID);

			if (region == null || region.IsDisabled)
			{
				player.Out.SendMessage("This destination is not available.", eChatType.CT_System,
					eChatLoc.CL_SystemWindow);
				return;
			}
			
			Say("I'm now teleporting you to " + destination.TeleportID + ".");
			OnTeleportSpell(player, destination);
		}
	}
}
