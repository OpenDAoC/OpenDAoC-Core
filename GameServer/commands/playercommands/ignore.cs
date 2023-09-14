namespace DOL.GS.Commands
{
    /// <summary>
    /// Command handler for the /ignore command
    /// </summary>
    [CmdAttribute(
        "&ignore",
        ePrivLevel.Player,
        "Adds/Removes a player to/from your Ignorelist!",
        "/ignore <playerName>")]
    public class IgnoreCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        /// <summary>
        /// Method to handle the command and any arguments
        /// </summary>
        /// <param name="client"></param>
        /// <param name="args"></param>
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                string[] ignores = client.Player.SerializedIgnoreList;
                client.Out.SendCustomTextWindow("Ignore List (snapshot)", ignores);
                return;
            }

            string name = string.Join(" ", args, 1, args.Length - 1);
            GamePlayer otherPlayer = ClientService.GetPlayerByPartialName(name, out ClientService.PlayerGuessResult result);

            if (result == ClientService.PlayerGuessResult.NOT_FOUND)
            {
                name = args[1];

                if (client.Player.IgnoreList.Contains(name))
                {
                    client.Player.ModifyIgnoreList(name, true);
                    return;
                }
                else
                {
                    // nothing found
                    DisplayMessage(client, "No players online with that name.");
                    return;
                }
            }

            switch (result)
            {
                case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                {
                    DisplayMessage(client, "Character name is not unique.");
                    break;
                }
                case ClientService.PlayerGuessResult.FOUND_EXACT:
                case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                {
                    if (otherPlayer == client.Player)
                    {
                        DisplayMessage(client, "You can't add yourself!");
                        return;
                    }

                    name = otherPlayer.Name;

                    if (client.Player.IgnoreList.Contains(name))
                        client.Player.ModifyIgnoreList(name, true);
                    else
                        client.Player.ModifyIgnoreList(name, false);

                    break;
                }
            }
        }
    }
}
