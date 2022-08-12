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
        ushort modelID = 0;
        switch (keep.Realm)
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
        FlagObject = new GameStaticItemTimed(45 * 60 * 1000);
        FlagObject.Model = modelID;
        FlagObject.X = x;
        FlagObject.Y = y;
        FlagObject.Z = z;
        FlagObject.CurrentRegion = WorldMgr.GetRegion(keep.Region);
        FlagObject.SpawnTick = GameLoop.GameLoopTime;
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
        }
    }

    private void StopCaptureTimer()
    {
        CaptureTimer?.Stop();
    }
    
    private int CaptureCallback(ECSGameTimer timer)
    {
        if (CaptureSeconds > 0)
        {
            CaptureSeconds -= 1;
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
    }
    
    public void CheckNearbyPlayers()
    {
        var playersNearFlag = FlagObject.GetPlayersInRadius(FlagCaptureRadius);
        Dictionary<eRealm, int> playersOfRealmDict = new Dictionary<eRealm, int>();
        foreach (GamePlayer player in playersNearFlag)
        {
            if (playersOfRealmDict.ContainsKey(player.Realm))
            {
                playersOfRealmDict[player.Realm]++;
            }
            else
            {
                playersOfRealmDict.Add(player.Realm, 1);    
            }
        }

        if (playersOfRealmDict.Keys.Count > 1)
        {
            StopCaptureTimer();
        }
        else if (playersOfRealmDict.Keys.Count > 0)
        {
            StartCaptureTimer(playersOfRealmDict.First().Key);
        }
    }
}