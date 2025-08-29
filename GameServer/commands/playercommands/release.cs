using System;

namespace DOL.GS.Commands
{
    [Cmd(
        "&release", ["&rel"],
        ePrivLevel.Player,
        "When you are dead you can '/release'. This will bring you back to your bindpoint!",
        "/release")]
    public class ReleaseCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length <= 1)
            {
                client.Player.Release(eReleaseType.Normal, false);
                return;
            }

            string toArgument = args[1];

            if (toArgument.Equals("city", StringComparison.OrdinalIgnoreCase))
            {
                client.Player.Release(eReleaseType.City, false);
                return;
            }

            if (toArgument.Equals("house", StringComparison.OrdinalIgnoreCase))
            {
                client.Player.Release(eReleaseType.House, false);
                return;
            }

            if (toArgument.Equals("bind", StringComparison.OrdinalIgnoreCase))
            {
                client.Player.Release(eReleaseType.Bind, false);
                return;
            }
        }
    }
}
