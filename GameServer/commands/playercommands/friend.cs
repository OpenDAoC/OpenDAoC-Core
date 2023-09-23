using System;
using System.Linq;
using DOL.GS.Friends;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&friend",
        ePrivLevel.Player,
        "Adds/Removes a player to/from your friendlist!",
        "/friend <playerName>")]
    public class FriendCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                client.Player.SendFriendsListSnapshot();
                return;
            }
            else if (args.Length == 2 && args[1] == "window")
            {
                client.Player.SendFriendsListSocial();
                return;
            }
            
            string name = string.Join(" ", args, 1, args.Length - 1);

            // attempt to remove from friends list now to avoid being unable to do so because of a guessed name from an online player
            if (RemoveFriend(name, client.Player))
                return;

            GamePlayer otherPlayer = ClientService.GetPlayerByPartialName(name, out ClientService.PlayerGuessResult result);

            // abort if the returned player is from a hostile realm
            if (result is ClientService.PlayerGuessResult.FOUND_PARTIAL or ClientService.PlayerGuessResult.FOUND_EXACT)
            {
                name = otherPlayer.Name;

                if (!GameServer.ServerRules.IsSameRealm(otherPlayer, client.Player, true))
                    result = ClientService.PlayerGuessResult.NOT_FOUND;
            }

            switch (result)
            {
                case ClientService.PlayerGuessResult.NOT_FOUND:
                {
                    DisplayMessage(client, "No players with that name, or you cannot add this player.");
                    return;
                }
                case ClientService.PlayerGuessResult.FOUND_MULTIPLE:
                {
                    DisplayMessage(client, "Character name is not unique.");
                    return;
                }
                case ClientService.PlayerGuessResult.FOUND_EXACT:
                {
                    if (IsAddingSelf(otherPlayer, client.Player))
                        return;

                    AddFriend(name, client.Player);
                    return;
                }
                case ClientService.PlayerGuessResult.FOUND_PARTIAL:
                {
                    if (IsAddingSelf(otherPlayer, client.Player))
                        return;

                    if (IsNameInFriendsList(name, client))
                    {
                        DisplayMessage(client, $"Type the full name to remove {name} from your list.");
                        return;
                    }

                    AddFriend(name, client.Player);
                    return;
                }
            }
        }

        private bool IsAddingSelf(GamePlayer playerToAdd, GamePlayer user)
        {
            if (playerToAdd == user)
            {
                DisplayMessage(user.Client, "You can't add yourself!");
                return true;
            }

            return false;
        }

        private bool IsNameInFriendsList(string name, GameClient client, StringComparer comparer = null)
        {
            return client.Player.GetFriends().Contains(name, comparer);
        }

        private bool RemoveFriend(string name, GamePlayer user)
        {
            if (IsNameInFriendsList(name, user.Client, StringComparer.OrdinalIgnoreCase) && user.RemoveFriend(name))
            {
                DisplayMessage(user.Client, $"{name} was removed from your friend list!");
                return true;
            }

            return false;
        }

        private void AddFriend(string name, GamePlayer user)
        {
            if (user.AddFriend(name))
                DisplayMessage(user.Client, $"{name} was added from your friend list!");
        }
    }
}
