using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DOL.Database;
using DOL.GS;
using DOL.GS.Keeps;

namespace DOL.GS;

public class SubObjective
{
    private ushort FlagCaptureRadius = ServerProperties.Properties.FLAG_CAPTURE_RADIUS; //maximum of 1000 rps awarded every interval (5 minutes atm)
    
    public GameStaticItemTimed FlagObject;
    private ECSGameTimer CaptureTimer = null;
    private int CaptureSeconds = 10;

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
            CaptureSeconds = 10;
            CapturingRealm = capturingRealm;
            CaptureTimer = new ECSGameTimer(FlagObject, CaptureCallback);
            CaptureTimer.Start(1000);
            Console.WriteLine($"Start timer");
        }
    }

    private void StopCaptureTimer()
    {
        CaptureTimer?.Stop();
        CaptureTimer = null;
        Console.WriteLine($"Stop timer");
    }
    
    private int CaptureCallback(ECSGameTimer timer)
    {
        if (CaptureSeconds > 0)
        {
            CaptureSeconds -= 1;
            Console.WriteLine($"Decrement to {CaptureSeconds}");
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
        FlagObject.Model = GetModelIDForRealm(FlagObject.Realm);
        FlagObject.BroadcastUpdate();
        CaptureTimer = null;
        ConquestService.ConquestManager.AddContributors(FlagObject.GetPlayersInRadius(750, true).OfType<GamePlayer>().Where(player => player.Realm == CapturingRealm).ToList());
        Console.WriteLine($"Flag captured for realm {OwningRealm}");
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
        else if (playersOfRealmDict.Keys.Count > 0 && playersOfRealmDict.First().Key != OwningRealm)
        {
            StartCaptureTimer(playersOfRealmDict.First().Key);
        }
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