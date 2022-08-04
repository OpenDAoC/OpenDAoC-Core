using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.GS.Keeps;

namespace DOL.GS;

public class ConquestObjective
{
    public AbstractGameKeep Keep;
    public int AlbionContribution;
    public int MidgardContribution;
    public int HiberniaContribution;

    private int SubTickMaxReward = ServerProperties.Properties.MAX_SUBTASK_RP_REWARD; //maximum of 1000 rps awarded every interval (5 minutes atm)

    public long LastRolloverTick = 0;
    public long StartTick;

    public int TotalContribution => AlbionContribution + HiberniaContribution + MidgardContribution;

    private Dictionary<GamePlayer, int> PlayerToContributionDict;

    public GameStaticItemTimed FlagOne;
    public GameStaticItemTimed FlagTwo;
    public GameStaticItemTimed FlagThree;
    public GameStaticItemTimed FlagFour;

    public ConquestObjective(AbstractGameKeep keep)
    {
        Keep = keep;

        InitializeFlags(keep);
        
        PlayerToContributionDict = new Dictionary<GamePlayer, int>();
        StartTick = GameLoop.GameLoopTime;
        LastRolloverTick = StartTick;
        ResetContribution();
    }

    private void InitializeFlags(AbstractGameKeep keep)
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

        var locs = GetFlagLocsForKeep(keep);

        FlagOne = new GameStaticItemTimed(45 * 60 * 1000);
        FlagOne.Model = modelID;
        FlagOne.X = locs[0].X;
        FlagOne.Y = locs[0].Y;
        FlagOne.Z = locs[0].Z;
        FlagOne.CurrentRegion = WorldMgr.GetRegion(keep.Region);
        FlagOne.SpawnTick = GameLoop.GameLoopTime;
        FlagOne.AddToWorld();
        
        FlagTwo = new GameStaticItemTimed(45 * 60 * 1000);
        FlagTwo.Model = modelID;
        FlagTwo.X = locs[1].X;
        FlagTwo.Y = locs[1].Y;
        FlagTwo.Z = locs[1].Z;
        FlagTwo.CurrentRegion = FlagOne.CurrentRegion;
        FlagTwo.SpawnTick = GameLoop.GameLoopTime;
        FlagTwo.AddToWorld();
        
        FlagThree = new GameStaticItemTimed(45 * 60 * 1000);
        FlagThree.Model = modelID;
        FlagThree.X = locs[2].X;
        FlagThree.Y = locs[2].Y;
        FlagThree.Z = locs[2].Z;
        FlagThree.CurrentRegion = FlagOne.CurrentRegion;
        FlagThree.SpawnTick = GameLoop.GameLoopTime;
        FlagThree.AddToWorld();
        
        FlagFour = new GameStaticItemTimed(45 * 60 * 1000);
        FlagFour.Model = modelID;
        FlagFour.X = locs[3].X;
        FlagFour.Y = locs[3].Y;
        FlagFour.Z = locs[3].Z;
        FlagFour.CurrentRegion = FlagOne.CurrentRegion;
        FlagFour.SpawnTick = GameLoop.GameLoopTime;
        FlagFour.AddToWorld();
    }

    private List<Point3D> GetFlagLocsForKeep(AbstractGameKeep keep)
    {
        List<Point3D> flagLocs = new List<Point3D>();

        switch (keep.Name.ToLower())
        {
            case "bledmeer faste":
                flagLocs.Add(new Point3D(632183, 585097, 5400)); //near hmg
                flagLocs.Add(new Point3D(629067, 631012, 5355)); //near amg
                flagLocs.Add(new Point3D(648889, 596948, 5511)); //near the keep
                flagLocs.Add(new Point3D(636055, 601363, 5448)); //crossroads
                break;
            case "dun crauchon":
                flagLocs.Add(new Point3D(447813, 341818, 3457)); //near amg
                flagLocs.Add(new Point3D(447201, 297120, 4254)); //near mmg
                flagLocs.Add(new Point3D(434208, 317595, 3033)); //near the keep
                flagLocs.Add(new Point3D(432212, 345540, 2841)); //near briefine
                break;
            case "caer benowyc":
                flagLocs.Add(new Point3D(608127, 320648, 3864)); //near hmg
                flagLocs.Add(new Point3D(653422, 322966, 4335)); //near mmg
                flagLocs.Add(new Point3D(645242, 344503, 4325)); //near the keep
                flagLocs.Add(new Point3D(624610, 345270, 4290)); //southern crossroads
                break;
            default:
                flagLocs.Add(new Point3D(0, 0, 0)); //near hmg
                flagLocs.Add(new Point3D(0, 0, 0)); //near mmg
                flagLocs.Add(new Point3D(0, 0, 0)); //near the keep
                flagLocs.Add(new Point3D(0, 0, 0)); //southern crossroads
                break;
                
        }
        
        return flagLocs;
    }

    public void DoPeriodicReward()
    {
        AwardContributors();
        PlayerToContributionDict.Clear();
        ResetContribution();
        LastRolloverTick = GameLoop.GameLoopTime;
    }

    public void ConquestCapture()
    {
        //DoPeriodicReward();
        //TODO: make a capture reward here
    }

    private void AwardContributors()
    {
        //TODO: Redo the award algo
        /*
        ConquestManager conqMan = ConquestService.ConquestManager;
        foreach (var player in PlayerToContributionDict.Keys.Where(x => PlayerToContributionDict[x] > 0))
        {
            switch (player.Realm)
            {
                case eRealm.Albion:
                    int albaward = (int)Math.Round(AlbionContribution * (PlayerToContributionDict[player] / (double) AlbionContribution));
                    if (albaward > SubTickMaxReward) albaward = SubTickMaxReward;
                    if (conqMan.AlbStreak > 0) albaward *= conqMan.AlbStreak;
                    player.GainRealmPoints(albaward, false, true);
                    break;
                case eRealm.Hibernia:
                    int hibaward = (int)Math.Round(HiberniaContribution * (PlayerToContributionDict[player] / (double) HiberniaContribution));
                    if (hibaward > SubTickMaxReward) hibaward = SubTickMaxReward;
                    if (conqMan.HibStreak > 0) hibaward *= conqMan.HibStreak;
                    player.GainRealmPoints(hibaward, false, true);
                    break;
                case eRealm.Midgard:
                    int midaward = (int)Math.Round(MidgardContribution * (PlayerToContributionDict[player] / (double) MidgardContribution));
                    if (midaward > SubTickMaxReward) midaward = SubTickMaxReward;
                    if (conqMan.MidStreak > 0) midaward *= conqMan.MidStreak;
                    player.GainRealmPoints(midaward, false, true);
                    break;
            }
        }*/
    }

    public List<GamePlayer> GetContributingPlayers()
    {
        return PlayerToContributionDict.Keys.ToList();
    }

    public int GetPlayerContributionValue(GamePlayer player)
    {
        if (!PlayerToContributionDict.ContainsKey(player))
            return 0;
        
        return PlayerToContributionDict[player];
    }

    private void ResetContribution()
    {
        AlbionContribution = 0;
        HiberniaContribution = 0;
        MidgardContribution = 0;
    }
}