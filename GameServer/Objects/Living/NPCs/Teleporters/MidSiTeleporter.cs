using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	/// <summary>
	/// Midgard SI teleporter.
	/// </summary>
	/// <author>Aredhel</author>
	public class MidSiTeleporter : GameTeleporter
	{
		/// <summary>
		/// Add equipment to the teleporter.
		/// </summary>
		/// <returns></returns>
		public override bool AddToWorld()
		{
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 983, 26);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 986, 26);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 984, 26);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 987, 26);
			template.AddNPCEquipment(eInventorySlot.Cloak, 57, 26);
			Inventory = template.CloseTemplate();

			SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			VisibleActiveWeaponSlots = 34;
			return base.AddToWorld();
		}
		
		private String[] m_destination = { 
			"Aegirhamn",
			"Bjarken",
			"Hagall",
			"Knarr" };
		
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
		protected override void OnDestinationPicked(GamePlayer player, DbTeleports destination)
		{
			// Not porting to where we already are.

			List<String> playerAreaList = new List<String>();
			foreach (AbstractArea area in player.CurrentAreas)
				playerAreaList.Add(area.Description);

			if (playerAreaList.Contains(destination.TeleportID))
				return;

			switch (destination.TeleportID.ToLower())
			{
				case "aegirhamn":
					break;
				case "bjarken":
					break;
				case "hagall":
					break;
				case "knarr":
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
		protected override void OnTeleport(GamePlayer player, DbTeleports destination)
		{
			OnTeleportSpell(player, destination);
		}
	}
}