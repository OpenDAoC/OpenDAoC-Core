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

        FlagOne = new GameStaticItemTimed(45 * 60 * 1000);
        FlagOne.Model = modelID;
        FlagOne.X = keep.X + 5000;
        FlagOne.Y = keep.Y + 5000;
        
        FlagTwo = new GameStaticItemTimed(45 * 60 * 1000);
        FlagTwo.Model = modelID;
        FlagTwo.X = keep.X - 5000;
        FlagTwo.Y = keep.Y + 5000;
        
        FlagThree = new GameStaticItemTimed(45 * 60 * 1000);
        FlagThree.Model = modelID;
        FlagThree.X = keep.X + 5000;
        FlagThree.Y = keep.Y - 5000;
        
        FlagFour = new GameStaticItemTimed(45 * 60 * 1000);
        FlagFour.Model = modelID;
        FlagFour.X = keep.X - 5000;
        FlagFour.Y = keep.Y - 5000;
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