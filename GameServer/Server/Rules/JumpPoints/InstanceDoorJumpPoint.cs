using Core.Database.Tables;

namespace Core.GS.Server;

/// <summary>
/// Handles doors inside of instances.
/// </summary>
public class InstanceDoorJumpPoint : IJumpPointHandler
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
        if (player.CurrentRegion is BaseInstance == false)
            return true;

        if (((BaseInstance)player.CurrentRegion).OnInstanceDoor(player, targetPoint))
            return true;
        else
            return false; //Let instance handle zoning by itself in this case...
    }
}