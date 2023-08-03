﻿using System;
using System.Linq;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;


namespace DOL.GS.Scripts
{
    [Command(
        "&predator",
        EPrivLevel.Player,
        "Join the hunt or view your current prey", "/predator join", "/predator prey", "/predator abandon")]
    public class PredatorCommand : AbstractCommandHandler, ICommandHandler
    {
        private const string KILLEDBY = "KilledBy";

        private int amount;
        private GamePlayer killerPlayer;

        private int minBountyReward = ServerProperties.ServerProperties.BOUNTY_MIN_REWARD;
        private int maxBountyReward = ServerProperties.ServerProperties.BOUNTY_MAX_REWARD;
        private int minLoyalty = ServerProperties.ServerProperties.BOUNTY_MIN_LOYALTY;

        public void OnCommand(GameClient client, string[] args)
        {
            
            if (IsSpammingCommand(client.Player, "Predator"))
            {
                return;
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            // todo: remove this
            if (args[1] == "prey")
            {
                //if player is in PredatorManager.ActivePlayers, view current target
                //else return message w/ error
                if (!PredatorMgr.PlayerIsActive(client.Player))
                {
                    if (PredatorMgr.QueuedPlayers.Contains(client.Player))
                    {
                        client.Out.SendMessage("You are queued to join the hunt soon!", EChatType.CT_Important,
                            EChatLoc.CL_SystemWindow);   
                    }
                    else
                    {
                        client.Out.SendMessage("You are not a part of the hunt!", EChatType.CT_Important,
                            EChatLoc.CL_SystemWindow);
                    }

                    return;
                }

                client.Out.SendCustomTextWindow("Your Prey", PredatorMgr.GetActivePrey(client.Player));
            }
            else if (args[1] == "join")
            {
                if (client.Player.Level < 50)
                {
                    client.Out.SendMessage("You must be level 50 to join the hunt!", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
                    return;
                }
                
                if (client.Player.Group != null && client.Player.Group.GetPlayersInTheGroup().Count > 0)
                {
                    client.Out.SendMessage("The mightiest predators hunt alone! Leave your group to join the hunt.", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
                    return;
                }

                AbstractArea area = client.Player.CurrentZone.GetAreasOfSpot(client.Player.X, client.Player.Y, client.Player.Z)
                    .FirstOrDefault() as AbstractArea;

                //if user is not in an RvR zone, or is in DF
                if ((!client.Player.CurrentZone.IsRvR 
                     && ( area == null || (area != null && !area.Description.Equals("Druim Ligen")))) 
                    || client.Player.CurrentZone.ID == 249)
                {
                    client.Out.SendMessage("You must be in an Old Frontiers zone to join the hunt.", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
                    return;
                }
                
                PredatorMgr.QueuePlayer(client.Player);

            }
            else if (args[1] == "reset" && client.Account.PrivLevel > 1)
            {
                PredatorMgr.FullReset();
            }
            else if (args[1] == "insert" && client.Account.PrivLevel > 1)
            {
                PredatorMgr.InsertQueuedPlayers();  
            }
            else if (args[1] == "status" && client.Account.PrivLevel > 1)
            {
                client.Out.SendCustomTextWindow("Active Hunts", PredatorMgr.GetStatus());
            }
            else if (args[1] == "abandon")
            {
                if (!PredatorMgr.PlayerIsActive(client.Player))
                {
                    client.Out.SendMessage("You are not a part of the hunt.", EChatType.CT_Important,
                        EChatLoc.CL_SystemWindow);
                    return;
                }
                PredatorMgr.DisqualifyPlayer(client.Player);
            }
            else
            {
                DisplaySyntax(client);
            }
        }
    }
}