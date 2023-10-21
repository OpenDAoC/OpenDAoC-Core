using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;

namespace Core.GS
{
	/// <summary>
	/// Simple Teleporter.
	/// This teleporter uses the npc guild name to determine available teleport locations in the Teleport table
	/// PackageID is used for the text displayed to the player
	/// 
	/// Example:
	/// Add this npc to the world and set guild name to 'My Teleports'
	/// Go to a location you want to teleport too and use the command /teleport 'location name' 'My Teleports'
	/// 
	/// You can whisper refresh to this teleporter to reload the teleport locations
	/// </summary>
	public class SimpleTeleporter : GameTeleporter
	{
		protected override string Type
		{
			get
			{
				return GuildName;
			}
		}

		private List<DbTeleport> m_destinations = new List<DbTeleport>();

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

			if (player.InCombat)
				return false;

			if (GameServer.ServerRules.IsSameRealm(this, player, true) == false && player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
				return false;

			if ((GuildName == null || GuildName.Length == 0) && player.Client.Account.PrivLevel > (int)EPrivLevel.Player)
			{
				SayTo(player, "I have not been set up properly, I need a guild name in order to work.");
				SayTo(player, "You can set what I say to players by setting the packageid with /mob package \"Some Text\"");
				return true;
			}

			LoadDestinations();

			if (string.IsNullOrEmpty(PackageID) == false)
			{
				SayTo(player, PackageID);
			}
			else
			{
				SayTo(player, $"Hello {player.PlayerClass.Name}, choose a destination:");
			}

			int numDestinations = 0;
			foreach (DbTeleport destination in m_destinations)
			{
				player.Out.SendMessage(String.Format("[{0}]", destination.TeleportID), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
				numDestinations++;
			}

			if (numDestinations == 0 && player.Client.Account.PrivLevel > (int)EPrivLevel.Player)
			{
				SayTo(player, "I have not been set up properly, I need teleport locations.  Do /teleport add \"Destination Name\" \"" + GuildName + "\"");
			}

			return true;
		}

		/// <summary>
		/// Use the NPC Guild Name to find all the valid destinations for this teleporter
		/// </summary>
		protected void LoadDestinations()
		{
			if (m_destinations.Count > 0 || GuildName == null || GuildName.Length == 0)
				return;

			m_destinations.AddRange(CoreDb<DbTeleport>.SelectObjects(DB.Column("Type").IsEqualTo(GuildName)));
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
			GamePlayer player = source as GamePlayer;

			if (player == null)
				return false;

			if (player.InCombat)
				return false;

			if (player.Client.Account.PrivLevel > 1 && text.ToLower() == "refresh")
			{
				m_destinations.Clear();
				return false;
			}

			if (GameServer.ServerRules.IsSameRealm(this, player, true) == false && player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
				return false;

			DbTeleport destination = null;

			foreach (DbTeleport t in m_destinations)
			{
				if (t.TeleportID == text)
				{
					destination = t;
					break;
				}
			}

			if (destination != null)
			{
				OnDestinationPicked(player, destination);
			}

			return false;
		}


		/// <summary>
		/// Player has picked a destination.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="destination"></param>
		protected override void OnDestinationPicked(GamePlayer player, DbTeleport destination)
		{
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
