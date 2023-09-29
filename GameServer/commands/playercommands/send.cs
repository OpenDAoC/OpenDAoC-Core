namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&send",
        new [] { "&tell", "&t" },
        ePrivLevel.Player,
        // Displays next to the command when '/cmd' is entered
        "Sends a private message to the target player.",
        "PLCommands.SendMessage.Syntax.Send")]
    public class SendCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 3)
            {
                // Message: '/send <targetName> <message>' - Sends a private message to the target player.
                ChatUtil.SendSystemMessage(client, "PLCommands.SendMessage.Syntax.Send", null);
                return;
            }

            if (IsSpammingCommand(client.Player, "send", 500))
            {
                // Message: "Slow down, you're typing too fast--make the moment last."
                ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.SlowDown", null);
                return;
            }

            string targetName = args[1];
            var name = !string.IsNullOrWhiteSpace(targetName) && char.IsLower(targetName, 0) ? targetName.Replace(targetName[0],char.ToUpper(targetName[0])) : targetName; // If first character in args[1] is lowercase, replace with uppercase character
            string message = string.Join(" ", args, 2, args.Length - 2);
            ClientService.PlayerGuessResult result;
            GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(targetName, out result);

            if (targetPlayer != null && !GameServer.ServerRules.IsAllowedToUnderstand(client.Player, targetPlayer))
                targetPlayer = null;

            if (targetPlayer == null)
            {
                // Message: "{0} is not in the game, or is a member of another realm."
                ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
                return;
            }

            // prevent to send an anon GM a message to find him - but send the message to the GM - thx to Sumy
            if (targetPlayer.IsAnonymous && targetPlayer.Client.Account.PrivLevel > (uint) ePrivLevel.Player && targetPlayer != client.Player)
            {
                if (client.Account.PrivLevel == (uint) ePrivLevel.Player)
                {
                    // Message: "{0} is not in the game, or is a member of another realm."
                    ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.OfflineOtherRealm", name);
                    // Message: {0} tried to send you a message: "{1}"
                    ChatUtil.SendSendMessage(targetPlayer, "Social.ReceiveMessage.Staff.TriedToSend", client.Player.Name, message);
                }

                if (client.Account.PrivLevel > (uint) ePrivLevel.Player)
                {
                    // Let staff ignore anon state for other staff members
                    // Message: You send, "{0}" to {1} [ANON].
                    ChatUtil.SendSendMessage(client, "Social.SendMessage.Staff.YouSendAnon", message, targetPlayer.Name);
                    // Message: {0} [TEAM] sends, "{1}"
                    ChatUtil.SendGMMessage(targetPlayer, "Social.ReceiveMessage.Staff.SendsToYou", client.Player.Name, message);
                }

                return;
            }

            switch (result)
            {
                case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                {
                    // Message: "{0} is not a unique character name."
                    ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.NameNotUnique", name);
                    return;
                }
                case ClientService.PlayerGuessResult.FOUND_EXACT:
                case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                {
                    if (targetPlayer.Client == client)
                    {
                        // Message: "You can't message yourself!"
                        ChatUtil.SendSystemMessage(client, "Social.SendMessage.Err.CantMsgYourself", null);
                    }
                    else
                    {
                        // Send the message
                        client.Player.SendPrivateMessage(targetPlayer, message);
                    }

                    return;
                }
            }
        }
    }
}
