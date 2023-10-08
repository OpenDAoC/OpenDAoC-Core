namespace DOL.GS.Commands;

[Command(
    "&release", new string[] { "&rel" },
    EPrivLevel.Player,
    "When you are dead you can '/release'. This will bring you back to your bindpoint!",
    "/release")]
public class ReleaseCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (client.Player.CurrentRegion.IsRvR && !client.Player.CurrentRegion.IsDungeon)
        {
            client.Player.Release(EReleaseType.RvR, false);
            return;
        }

        if (args.Length > 1 && args[1].ToLower() == "city")
        {
            client.Player.Release(EReleaseType.City, false);
                return;
        }

        if (args.Length > 1 && args[1].ToLower() == "house")
        {
            client.Player.Release(EReleaseType.House, false);
            return;
        }

        client.Player.Release(EReleaseType.Normal, false);
    }
}