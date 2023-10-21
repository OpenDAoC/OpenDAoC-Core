using Core.Database;
using Core.Database.Tables;
using Core.GS.PacketHandler;
using Core.GS.Quests;

namespace Core.GS.ServerRules
{
	/// <summary>
	/// Handles task dungeon jump points
	/// </summary>
	public class TaskDungeonJumpPoint : IJumpPointHandler
	{
		/// <summary>
		/// Decides whether player can jump to the target point.
		/// All messages with reasons must be sent here.
		/// Can change destination too.
		/// </summary>
		/// <param name="targetPoint">The jump destination</param>
		/// <param name="player">The jumping player</param>
		/// <returns>True if allowed</returns>
        public bool IsAllowedToJump(DbZonePoint targetPoint, GamePlayer player)
        {
            //Handles zoning INTO an instance.
            GameLocation loc = null;

            //First, we try the groups mission.
            if (player.Group != null)
            {
                GroupUtil grp = player.Group;
                if (grp.Mission != null && grp.Mission is TaskDungeonMission)
                {
                    //Attempt to get the instance entrance location...
                    TaskDungeonMission task = (TaskDungeonMission)grp.Mission;
                    loc = task.TaskRegion.InstanceEntranceLocation;
                }
            }
            else if (player.Mission != null && player.Mission is TaskDungeonMission)
            {
                //Then, try personal missions...
                TaskDungeonMission task = (TaskDungeonMission)player.Mission;
                loc = task.TaskRegion.InstanceEntranceLocation;
            }

            if (loc != null)
            {
                targetPoint.TargetX = loc.X;
                targetPoint.TargetY = loc.Y;
                targetPoint.TargetZ = loc.Z;
                targetPoint.TargetRegion = loc.RegionID;
                targetPoint.TargetHeading = loc.Heading;
                return true;
            }

            player.Out.SendMessage("You need to have a proper mission before entering this area!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return false;
        }
	}
}