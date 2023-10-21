using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Keeps;

namespace Core.GS
{
	/// <summary>
	/// Description of TeleportArea.
	/// Used to teleport players when someone enters, with Z-checks
	/// </summary>
	public class TeleportArea : Area.Circle
	{
		public override void OnPlayerEnter(GamePlayer player)
		{
			base.OnPlayerEnter(player);
			DbTeleport destination =  WorldMgr.GetTeleportLocation(player.Realm, String.Format("{0}:{1}", this.GetType(), this.Description));
			
			if (destination != null)
				OnTeleport(player, destination);
			else
				player.Out.SendMessage("This destination is not available : "+String.Format("{0}:{1}", this.GetType(), this.Description)+".", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
		
		/// <summary>
		/// Teleport the player to the designated coordinates. 
		/// </summary>
		/// <param name="player"></param>
		/// <param name="destination"></param>
		protected void OnTeleport(GamePlayer player, DbTeleport destination)
		{
			if (player.InCombat == false && GameRelic.IsPlayerCarryingRelic(player) == false)
			{
				player.LeaveHouse();
				GameLocation currentLocation = new GameLocation("TeleportStart", player.CurrentRegionID, player.X, player.Y, player.Z);
				player.MoveTo((ushort)destination.RegionID, destination.X, destination.Y, destination.Z, (ushort)destination.Heading);
				GameServer.ServerRules.OnPlayerTeleport(player, currentLocation, destination);
			}
		}
		
	}

	/// <summary>
	/// Description of TeleportArea.
	/// Used to teleport players when someone enters, Withtout Z-checks
	/// </summary>	
	public class TeleportPillarArea : TeleportArea
	{
		
		public override bool IsContaining(int x, int y, int z)
		{
			return base.IsContaining(x, y, z, false);
		}
		
		public override bool IsContaining(IPoint3D spot)
		{
			return base.IsContaining(spot, false);
		}
		
		public override bool IsContaining(int x, int y, int z, bool checkZ)
		{
			return base.IsContaining(x, y, z, false);
		}
		
		public override bool IsContaining(IPoint3D p, bool checkZ)
		{
			return base.IsContaining(p, false);
		}
		
	}
}
