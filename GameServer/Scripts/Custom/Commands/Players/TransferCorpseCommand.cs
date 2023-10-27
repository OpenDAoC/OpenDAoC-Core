﻿using Core.GS.Commands;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Keeps;
using Core.GS.Server;
using Core.GS.World;

namespace Core.GS.Scripts.Custom;

[Command(
    "&transfercorpse",
    new string[] {"&tc"},
    EPrivLevel.Player, // Set to player.
    "/transfercorpse <Keep name> ie: /transfercorpse dun crauchon")]
public class TransferCorpseCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (!ServerProperty.ENABLE_CORPSESUMONNER)
        {
            client.Player.Out.SendMessage("This command is currently disable!", EChatType.CT_System,
                EChatLoc.CL_ChatWindow);
            return;
        }

        if (IsSpammingCommand(client.Player, "transfercorpse"))
            return;
        if (args.Length < 3)
        {
            DisplaySyntax(client);
            return;
        }

        string keepname = "";
        if (args.Length == 3)
        {
            keepname = args[1] + " " + args[2];
        }
        else if (args.Length == 4)
        {
            keepname = args[1] + " " + args[2] + " " + args[3];
        }
        else
        {
            DisplaySyntax(client);
            return;
        }

        if (client.Player.IsAlive)
        {
            client.Player.Out.SendMessage(
                "You must be dead to use this command, and you must be same region as your keep!",
                EChatType.CT_System, EChatLoc.CL_ChatWindow);
            return;
        }

        if (!client.Player.CurrentZone.IsOF)
        {
            client.Player.Out.SendMessage("You must be dead in frontiers to use this command!", EChatType.CT_System,
                EChatLoc.CL_ChatWindow);
            return;
        }

        if (!client.Player.LastDeathPvP)
        {
            client.Player.Out.SendMessage("You must be dead for your realm to use this command!",
                EChatType.CT_System, EChatLoc.CL_ChatWindow);
            return;
        }

        if (client.Player.WasMovedByCorpseSummoner)
        {
            client.Player.Out.SendMessage("You cannot use this command more than one time by death!",
                EChatType.CT_System, EChatLoc.CL_ChatWindow);
            return;
        }

        AGameKeep keep = GameServer.KeepManager.GetKeepByShortName(keepname);

        if (keep == null)
        {
            client.Player.Out.SendMessage("You must provide a valid keep name!", EChatType.CT_System,
                EChatLoc.CL_ChatWindow);
            return;
        }

        if (keep.Realm != client.Player.Realm)
        {
            client.Player.Out.SendMessage("Your realm must own this keep for being able to use this functions!",
                EChatType.CT_System, EChatLoc.CL_ChatWindow);
            return;
        }

        foreach (GameKeepGuard guard in keep.Guards.Values)
        {
            if (guard is GuardCorpseSummoner)
            {
                if (guard.CurrentZone != null && guard.CurrentZone.ID == client.Player.CurrentZone.ID)
                {
                    if (!guard.IsAlive || guard.ObjectState != GameObject.eObjectState.Active || guard.IsRespawning)
                    {
                        client.Player.Out.SendMessage(
                            "The Corpse Summoner of this keep is actually dead or inactive!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);
                        break;
                    }
                    else
                    {
                        Point3D targetPoint;
                        targetPoint = new Point3D(guard.GetPointFromHeading((ushort) Util.Random(4096), 50),
                            guard.Z);
                        client.Player.WasMovedByCorpseSummoner = true;
                        client.Player.MoveTo(guard.CurrentRegionID, targetPoint.X, targetPoint.Y, targetPoint.Z,
                            client.Player.Heading);
                        break;
                    }
                }
                else
                {
                    client.Player.Out.SendMessage("You need to be in the same zone than the requested keep!",
                        EChatType.CT_System, EChatLoc.CL_ChatWindow);
                    break;
                }
            }
        }
    }
}