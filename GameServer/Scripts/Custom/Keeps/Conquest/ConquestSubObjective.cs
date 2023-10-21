using System;
using System.Collections.Generic;
using System.Linq;
using Core.GS.Keeps;
using Core.GS.PacketHandler;

namespace Core.GS;

public class ConquestSubObjective
{
    private ushort FlagCaptureRadius = ServerProperties.Properties.FLAG_CAPTURE_RADIUS; //how far away can we capture flag from
    private static int FlagCaptureTime = ServerProperties.Properties.FLAG_CAPTURE_TIME; //how long to capture flag
    uint fullCycleTime = (uint) ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION;

    private int ObjectiveNumber = 0;
    
    public GameStaticItemTimed FlagObject;
    private EcsGameTimer CaptureTimer = null;
    private int CaptureSeconds = FlagCaptureTime;

    private HashSet<GamePlayer> RecentCaps = new HashSet<GamePlayer>();

    private ERealm CapturingRealm;
    public ERealm OwningRealm;

    public ConquestSubObjective(int x, int y, int z, AGameKeep keep, int objectiveNumber)
    {
        FlagObject = new GameStaticItemTimed(fullCycleTime * 60 * 1000);
        FlagObject.Model = GetModelIDForRealm(keep.Realm);
        FlagObject.X = x;
        FlagObject.Y = y;
        FlagObject.Z = z;
        FlagObject.CurrentRegion = WorldMgr.GetRegion(keep.Region);
        FlagObject.SpawnTick = GameLoop.GameLoopTime;
        FlagObject.Realm = keep.Realm;
        FlagObject.AddToWorld();

        OwningRealm = keep.Realm;

        ObjectiveNumber = objectiveNumber;
    }

    public void Cleanup()
    {
        FlagObject.RemoveFromWorld();
        FlagObject.Delete();
        RecentCaps.Clear();
    }

    private void StartCaptureTimer(ERealm capturingRealm)
    {
        if (CaptureTimer == null)
        {
            CaptureSeconds = FlagCaptureTime;
            CapturingRealm = capturingRealm;
            CaptureTimer = new EcsGameTimer(FlagObject, CaptureCallback);
            CaptureTimer.Start(1000);
        }
    }

    private void StopCaptureTimer()
    {
        CaptureTimer?.Stop();
        CaptureTimer = null;
    }
    
    private int CaptureCallback(EcsGameTimer timer)
    {
        if (CaptureSeconds > 0)
        {
            BroadcastTimeUntilCapture(CaptureSeconds--);
        }
        else
        {
            Capture();
            return 0;
        }

        return 1000;
    }

    private void Capture()
    {
        OwningRealm = CapturingRealm;
        FlagObject.Realm = CapturingRealm;
        FlagObject.Model = GetModelIDForRealm(CapturingRealm);
        ClientService.UpdateObjectForPlayers(FlagObject);
        CaptureTimer = null;
        BroadcastCapture();
        var nearbyPlayers = FlagObject.GetPlayersInRadius(750).Where(player => player.Realm == CapturingRealm).ToList();
        ConquestService.ConquestManager.AddContributors(nearbyPlayers);

        foreach (var player in nearbyPlayers)
        {
            if(!RecentCaps.Contains(player) && player.Realm == CapturingRealm)
                player.GainRealmPoints(50, false);
            else
                player.Out.SendMessage($"You've recently captured this flag and are awarded no realm points.", EChatType.CT_System, EChatLoc.CL_SystemWindow);

            RecentCaps.Add(player);
        }
    }

    public void ResetContributors()
    {
        RecentCaps.Clear();
    }

    private void BroadcastTimeUntilCapture(int secondsLeft)
    {
        foreach (GamePlayer player in FlagObject.GetPlayersInRadius(750))
        {
            if(secondsLeft%5 == 0)
                player.Out.SendMessage($"{secondsLeft} seconds until capture", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
            player.Out.SendMessage($"{secondsLeft} seconds until capture", EChatType.CT_System, EChatLoc.CL_SystemWindow);
        }
        
        if(secondsLeft%5 == 0)
            ClientService.UpdateObjectForPlayers(FlagObject);
    }

    private void BroadcastCapture()
    {
        foreach (GamePlayer player in FlagObject.GetPlayersInRadius(750))
        {
            switch (player.Realm)
            {
                case ERealm.Albion:
                    player.Out.SendSoundEffect(2594, 0, 0, 0, 0, 0);
                    break;
                case ERealm.Midgard:
                    player.Out.SendSoundEffect(2596, 0, 0, 0, 0, 0);
                    break;
                case ERealm.Hibernia:
                    player.Out.SendSoundEffect(2595, 0, 0, 0, 0, 0);
                    break;
            }
        }

        foreach (GamePlayer player in FlagObject.GetPlayersInRadius(50000))
        {
            if (player.Realm == CapturingRealm)
                player.Out.SendMessage($"An ally has captured flag {ObjectiveNumber}!", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
            else
                player.Out.SendMessage($"An enemy has captured flag {ObjectiveNumber}!", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
        }
    }

    private ushort GetModelIDForRealm(ERealm realm)
    {
        ushort modelID = 0;
        switch (realm)
        {
            case ERealm.Hibernia:
                modelID = 466;
                break;
            case ERealm.Albion:
                modelID = 464;
                break;
            case ERealm.Midgard:
                modelID = 465;
                break;
        }

        return modelID;
    }
    
    public void CheckNearbyPlayers()
    {
        Dictionary<ERealm, int> playersOfRealmDict = new Dictionary<ERealm, int>();
       // Console.WriteLine($"Flag Object {FlagObject} {FlagObject.CurrentZone.Description} {FlagObject.Realm} {FlagObject.CurrentRegion.Description} players nearby {FlagObject.GetPlayersInRadius(true, 1000, true)}");
       var nearbyPlayers = FlagObject.GetPlayersInRadius(750);
        foreach (GamePlayer player in nearbyPlayers)
        {
            if (!player.IsAlive || player.IsStealthed) continue;
           //Console.WriteLine($"Player near flag: {player.Name}");
            if (playersOfRealmDict.ContainsKey(player.Realm))
            {
                playersOfRealmDict[player.Realm]++;
            }
            else
            {
                playersOfRealmDict.Add(player.Realm, 1);    
            }
        }

        if (playersOfRealmDict.Keys.Count is > 1 or 0 && CaptureTimer != null)
        {
            StopCaptureTimer();
        }
        else if (playersOfRealmDict.Keys.Count == 1 && playersOfRealmDict.First().Key != OwningRealm)
        {
            StartCaptureTimer(playersOfRealmDict.First().Key);
            ConquestService.ConquestManager.AddContributors(FlagObject.GetPlayersInRadius(750).Where(x=> x.Realm == playersOfRealmDict.First().Key).ToList());
        }
    }
    
    public int GetNearbyPlayerCount()
    {
        return FlagObject.GetPlayersInRadius(750).Count;
    }

    public String GetOwnerRealmName()
    {
        switch (OwningRealm)
        {
            case ERealm.Albion:
                return "Albion";
            case ERealm.Hibernia:
                return "Hibernia";
            case ERealm.Midgard:
                return "Midgard";
        }

        return null;
    }
}
