using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.PacketHandler;

namespace Core.GS
{
	public class HiberniaSiTeleporter : GameTeleporter
	{
		/// <summary>
		/// Add equipment to the teleporter.
		/// </summary>
		/// <returns></returns>
		public override bool AddToWorld()
		{
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(EInventorySlot.TorsoArmor, 1008, 0);
			template.AddNPCEquipment(EInventorySlot.HandsArmor, 396, 0);
			template.AddNPCEquipment(EInventorySlot.FeetArmor, 402, 0);
			template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 468);
			Inventory = template.CloseTemplate();

			SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			VisibleActiveWeaponSlots = 34;
			return base.AddToWorld();
		}
		
		private String[] m_destination = { 
			"Grove of Domnann",
			"Droighaid",
			"Aalid Feie",
			"Necht" };
		
		/// <summary>
		/// Display the teleport indicator around this teleporters feet
		/// </summary>
		public override bool ShowTeleporterIndicator
		{
			get
			{
				return true;
			}
		}
		
		/// <summary>
		/// Player right-clicked the teleporter.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

			List<String> playerAreaList = new List<String>();
			foreach (AbstractArea area in player.CurrentAreas)
				playerAreaList.Add(area.Description);

			SayTo(player, "Greetings. Where can I send you?");
			foreach (String destination in m_destination)
				if (!playerAreaList.Contains(destination))
					player.Out.SendMessage(String.Format("[{0}]", destination),
						EChatType.CT_Say, EChatLoc.CL_PopupWindow);

			return true;
		}

		/// <summary>
		/// Player has picked a destination.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="destination"></param>
		protected override void OnDestinationPicked(GamePlayer player, DbTeleport destination)
		{
			// Not porting to where we already are.

			List<String> playerAreaList = new List<String>();
			foreach (AbstractArea area in player.CurrentAreas)
				playerAreaList.Add(area.Description);

			if (playerAreaList.Contains(destination.TeleportID))
				return;

			switch (destination.TeleportID.ToLower())
			{
				case "grove of domnann":
					break;
				case "droighaid":
					break;
				case "aalid feie":
					break;
				case "necht":
					break;
				default:
					return;
			}

			SayTo(player, "Have a safe journey!");
			base.OnDestinationPicked(player, destination);
		}

		/// <summary>
		/// Teleport the player to the designated coordinates.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="destination"></param>
		protected override void OnTeleport(GamePlayer player, DbTeleport destination)
		{
			OnTeleportSpell(player, destination);
		}
	}
}
