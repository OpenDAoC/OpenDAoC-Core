using DOL.Database;

namespace DOL.GS.Commands
{
    [Cmd("&hidegraves",
        ePrivLevel.Player,
        "Toggle the visibility of gravestones from other players",
        "/hidegraves")]
    public class HideGraves : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            if (IsSpammingCommand(player, "hidegraves"))
                return;

            DbCoreCharacter dbCharacter = player.DBCharacter;
            bool wasHidden = dbCharacter.HideGraves;
            dbCharacter.HideGraves = !wasHidden;

            foreach (GameStaticItem item in player.GetItemsInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (item is not GameGravestone gravestone)
                    continue;

                bool isVisible = gravestone.IsVisibleTo(player);

                if (wasHidden)
                {
                    if (isVisible)
                        ClientService.CreateObjectForPlayer(player, gravestone);
                }
                else
                {
                    if (!isVisible)
                        client.Out.SendObjectRemove(gravestone);
                }
            }

            string status = wasHidden ? "visible" : "hidden";
            DisplayMessage(client, $"Gravestones from other players are now {status}.");
        }
    }
}
