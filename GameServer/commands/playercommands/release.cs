namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&release", new string[] { "&rel" },
        ePrivLevel.Player,
        "When you are dead you can '/release'. This will bring you back to your bindpoint!",
        "/release")]
    public class ReleaseCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player.CurrentRegion.IsRvR && !client.Player.CurrentRegion.IsDungeon)
            {
                client.Player.Release(eReleaseType.RvR, false);
                return;
            }

            if (args.Length > 1 && args[1].ToLower() == "city")
            {
                client.Player.Release(eReleaseType.City, false);
                    return;
            }

            if (args.Length > 1 && args[1].ToLower() == "house")
            {
                client.Player.Release(eReleaseType.House, false);
                return;
            }

            client.Player.Release(eReleaseType.Normal, false);
        }
    }
}
