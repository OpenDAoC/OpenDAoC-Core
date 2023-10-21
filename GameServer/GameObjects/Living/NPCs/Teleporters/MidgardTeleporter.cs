using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.PacketHandler;

namespace Core.GS
{
	public class MidgardTeleporter : GameTeleporter
	{
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
			              "[Svasud Faste] in Mularn or \n[Vindsaul Faste] in West Svealand\n" +
			              "Beaches of [Gotar] near Nailiten\n" +
			              "[Aegirhamn] in the [Shrouded Isles]\n" +
			              "Our glorious city of [Jordheim]\n" +
			              "[Entrance] to the areas of [housing]\n\n" +
			              "Or one of the many [towns] throughout Midgard");
			
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
						String reply = String.Format("The isles of Aegir are an excellent choice. {0} {1}",
						                             "Would you prefer the city of [Aegirhamn] or perhaps one of the outlying towns",
						                             "like [Bjarken], [Hagall], or [Knarr]?");
						SayTo(player, reply);
						return;
					}
				case "housing":
					{
						SayTo(player,
							"I can send you to your [personal] or [guild] house. If you do not have a personal house, I can teleport you to the housing [entrance] or your housing [hearth] bindstone.");
						return;
					}
				
				case "towns":
				{
					SayTo(player,
						"I can send you to:\n" +
						"[Mularn]\n" +
						"[Fort Veldon]\n" +
						"[Audliten]\n" +
						"[Huginfell]\n" +
						"[Fort Atla]\n" +
						"[West Skona]");
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
				player.Out.SendMessage("This destination is not available.", EChatType.CT_System,
					EChatLoc.CL_SystemWindow);
				return;
			}
			
			Say("I'm now teleporting you to " + destination.TeleportID + ".");
			OnTeleportSpell(player, destination);
		}
	}
}
