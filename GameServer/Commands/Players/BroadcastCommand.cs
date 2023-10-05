using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands;

[Command(
     "&broadcast",
     new string[] { "&b" },
     ePrivLevel.Player,
     "Broadcast something to other players in the same zone",
     "/b <message>")]
public class BroadcastCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        const string BROAD_TICK = "Broad_Tick";

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

    private void Broadcast(GamePlayer player, string message)
    {
        foreach (GamePlayer otherPlayer in GetTargets(player))
            otherPlayer.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Broadcast.Message", player.Name, message), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
    }

    private List<GamePlayer> GetTargets(GamePlayer player)
    {
        List<GamePlayer> players = new();
        EBroadcastType type = (EBroadcastType) ServerProperties.Properties.BROADCAST_TYPE;

        switch (type)
        {
            case EBroadcastType.Area:
            {
                bool found = false;

                foreach (AbstractArea area in player.CurrentAreas)
                {
                    if (area.CanBroadcast)
                    {
                        found = true;

                        foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(player.CurrentRegion))
                        {
                            if (otherPlayer.CurrentAreas.Contains(area) && GameServer.ServerRules.IsAllowedToUnderstand(otherPlayer, player))
                                players.Add(otherPlayer);
                        }
                    }
                }

                if (!found)
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.Broadcast.NoHere"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                break;
            }
            case EBroadcastType.Realm:
            {
                players = ClientService.GetPlayersForRealmWideChatMessage(player);
                break;
            }
            case EBroadcastType.Region:
            {
                players = ClientService.GetPlayersOfRegion(player.CurrentRegion);
                break;
            }
            case EBroadcastType.Server:
            {
                players = ClientService.GetPlayers();
                break;
            }
            case EBroadcastType.Visible:
            {
                players = player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);
                break;
            }
            case EBroadcastType.Zone:
            {
                players = ClientService.GetPlayersOfZone(player.CurrentZone);
                break;
            }
        }

        return players;
    }
}