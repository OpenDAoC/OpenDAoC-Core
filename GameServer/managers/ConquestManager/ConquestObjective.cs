using System;
using System.Collections.Generic;
using System.Linq;
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

    public ConquestObjective(AbstractGameKeep keep)
    {
        Keep = keep;
        PlayerToContributionDict = new Dictionary<GamePlayer, int>();
        StartTick = GameLoop.GameLoopTime;
        LastRolloverTick = StartTick;
        ResetContribution();
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
        DoPeriodicReward();
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