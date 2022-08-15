using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS;

public class SubObjective
{
    private ushort FlagCaptureRadius = ServerProperties.Properties.FLAG_CAPTURE_RADIUS; //how far away can we capture flag from
    private static int FlagCaptureTime = ServerProperties.Properties.FLAG_CAPTURE_TIME; //how long to capture flag
    
    public GameStaticItemTimed FlagObject;
    private ECSGameTimer CaptureTimer = null;
    private int CaptureSeconds = FlagCaptureTime;

    private eRealm CapturingRealm;
    public eRealm OwningRealm;

    public SubObjective(int x, int y, int z, AbstractGameKeep keep)
    {
        FlagObject = new GameStaticItemTimed(45 * 60 * 1000);
        FlagObject.Model = GetModelIDForRealm(keep.Realm);
        FlagObject.X = x;
        FlagObject.Y = y;
        FlagObject.Z = z;
        FlagObject.CurrentRegion = WorldMgr.GetRegion(keep.Region);
        FlagObject.SpawnTick = GameLoop.GameLoopTime;
        FlagObject.Realm = keep.Realm;
        FlagObject.AddToWorld();

        OwningRealm = keep.Realm;
    }

    public void Cleanup()
    {
        FlagObject.Delete();
    }

    private void StartCaptureTimer(eRealm capturingRealm)
    {
        if (CaptureTimer == null)
        {
            CaptureSeconds = FlagCaptureTime;
            Console.WriteLine($"Flag Cap Time: {FlagCaptureTime} Seconds {CaptureSeconds}");
            CapturingRealm = capturingRealm;
            CaptureTimer = new ECSGameTimer(FlagObject, CaptureCallback);
            CaptureTimer.Start(1000);
        }
    }

    private void StopCaptureTimer()
    {
        CaptureTimer?.Stop();
        CaptureTimer = null;
    }
    
    private int CaptureCallback(ECSGameTimer timer)
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
        FlagObject.BroadcastUpdate();
        CaptureTimer = null;
        BroadcastCapture();
        var nearbyPlayers = FlagObject.GetPlayersInRadius(750, true).OfType<GamePlayer>()
            .Where(player => player.Realm == CapturingRealm).ToList();
        ConquestService.ConquestManager.AddContributors(nearbyPlayers);

        foreach (var player in nearbyPlayers)
        {
            player.GainRealmPoints(50, false);
        }
    }

    private void BroadcastTimeUntilCapture(int secondsLeft)
    {
        foreach (GamePlayer player in FlagObject.GetPlayersInRadius(750, false))
        {
            if(secondsLeft%5 == 0)
                player.Out.SendMessage($"{secondsLeft} seconds until capture", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage($"{secondsLeft} seconds until capture", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
    
    private void BroadcastCapture()
    {
        Parallel.ForEach(FlagObject.GetPlayersInRadius(750, false).OfType<GamePlayer>(), player =>
        {
            switch (player.Realm)
            {
                case eRealm.Albion:
                    player.Out.SendSoundEffect(2594, 0, 0, 0, 0, 0);
                    break;
                case eRealm.Midgard:
                    player.Out.SendSoundEffect(2596, 0, 0, 0, 0, 0);
                    break;
                case eRealm.Hibernia:
                    player.Out.SendSoundEffect(2595, 0, 0, 0, 0, 0);
                    break;
            }
        });
        
        Parallel.ForEach(FlagObject.GetPlayersInRadius(25000, false).OfType<GamePlayer>(), player =>
        {
            player.Out.SendMessage($"A flag has been captured!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
        });
    }
    
    private ushort GetModelIDForRealm(eRealm realm)
    {
        ushort modelID = 0;
        switch (realm)
        {
            case eRealm.Hibernia:
                modelID = 466;
                break;
            case eRealm.Albion:
                modelID = 464;
                break;
            case eRealm.Midgard:
                modelID = 465;
                break;
        }

        return modelID;
    }
    
    public void CheckNearbyPlayers()
    {
        Dictionary<eRealm, int> playersOfRealmDict = new Dictionary<eRealm, int>();
       // Console.WriteLine($"Flag Object {FlagObject} {FlagObject.CurrentZone.Description} {FlagObject.Realm} {FlagObject.CurrentRegion.Description} players nearby {FlagObject.GetPlayersInRadius(true, 1000, true)}");
        foreach (GamePlayer player in FlagObject.GetPlayersInRadius(750, true))
        {
            if (!player.IsAlive) continue;
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
        }
    }
    
    public int GetNearbyPlayerCount()
    {
        return FlagObject.GetPlayersInRadiusCount(750);
    }

    public String GetOwnerRealmName()
    {
        switch (OwningRealm)
        {
            case eRealm.Albion:
                return "Albion";
            case eRealm.Hibernia:
                return "Hibernia";
            case eRealm.Midgard:
                return "Midgard";
        }

        return null;
    }
}