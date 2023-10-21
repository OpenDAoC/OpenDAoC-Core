using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS
{
	public class AlbionSiTeleporter : GameTeleporter
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

		private String[] m_destination = { 
			"Caer Gothwaite",
			"Wearyall Village",
			"Fort Gwyntell",
			"Caer Diogel" };

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
				case "caer gothwaite":
					break;
				case "wearyall village":
					break;
				case "fort gwyntell":
					break;
				case "caer diogel":
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
