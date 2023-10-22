using System.Linq;
using Core.GS.Commands;
using Core.GS.Enums;
using Core.GS.Server;
using Core.GS.World;

namespace Core.GS.Scripts
{
    [Command(
        "&predator",
        EPrivLevel.Player,
        "Join the hunt or view your current prey", "/predator join", "/predator prey", "/predator abandon")]
    public class PredatorCommand : ACommandHandler, ICommandHandler
    {
        private const string KILLEDBY = "KilledBy";

        private int amount;
        private GamePlayer killerPlayer;

        private int minBountyReward = ServerProperty.BOUNTY_MIN_REWARD;
        private int maxBountyReward = ServerProperty.BOUNTY_MAX_REWARD;
        private int minLoyalty = ServerProperty.BOUNTY_MIN_LOYALTY;

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

                AArea area = client.Player.CurrentZone.GetAreasOfSpot(client.Player.X, client.Player.Y, client.Player.Z)
                    .FirstOrDefault() as AArea;

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