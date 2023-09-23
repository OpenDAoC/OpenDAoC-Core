using DOL.GS.PacketHandler;
using DOL.GS.Scripts.discord;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
         "&region",
         new string[] { "&reg" },
         ePrivLevel.Player,
         "Broadcast something to other players in the same region",
         "/region <message>")]
    public class RegionCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const string BROAD_TICK = "Broad_Tick";

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Broadcast.NoText"));
                return;
            }

            if (client.Player.IsMuted)
            {
                client.Player.Out.SendMessage("You have been muted. You cannot broadcast.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                return;
            }

            string message = string.Join(" ", args, 1, args.Length - 1);
            long BroadTick = client.Player.TempProperties.GetProperty<long>(BROAD_TICK);

            if (BroadTick > 0 && BroadTick - client.Player.CurrentRegion.Time <= 0)
                client.Player.TempProperties.RemoveProperty(BROAD_TICK);

            long changeTime = client.Player.CurrentRegion.Time - BroadTick;

            if (changeTime < 800 && BroadTick > 0)
            {
                client.Player.Out.SendMessage("Slow down! Think before you say each word!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                client.Player.TempProperties.SetProperty(BROAD_TICK, client.Player.CurrentRegion.Time);
                return;
            }

            Broadcast(client.Player, message);
            client.Player.TempProperties.SetProperty(BROAD_TICK, client.Player.CurrentRegion.Time);
        }

        private static void Broadcast(GamePlayer player, string message)
        {
            foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(player.CurrentRegion))
            {
                if (GameServer.ServerRules.IsAllowedToUnderstand(otherPlayer, player))
                    otherPlayer.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Region.Message", player.Name, message), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }

            if (Properties.DISCORD_ACTIVE)
                WebhookMessage.LogChatMessage(player, eChatType.CT_Broadcast, message);
        }
    }
}
