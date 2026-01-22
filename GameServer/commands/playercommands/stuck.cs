using System.Numerics;

namespace DOL.GS.Commands
{
    [CmdAttribute("&stuck",
        ePrivLevel.Player,
        "Move the player to the last recorded safe position",
        "/stuck")]
    public class StuckCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "stuck"))
                return;

            GamePlayer player = client.Player;
            PlayerMovementComponent movementComponent = player.movementComponent;

            // Early exit if the currently set safe position cannot be used.
            if (!player.movementComponent.TryGetSafePosition(out Vector3 _))
            {
                DisplayMessage(client, "No safe position could be found. Please use your bind stone instead.");
                return;
            }

            movementComponent.UseSafePosition = true; // Will be reset if the quit timer is interrupted.

            if (!player.Quit(false))
                return;
        }
    }
}
