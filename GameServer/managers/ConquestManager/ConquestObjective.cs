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
    
    public int TotalContribution => AlbionContribution + HiberniaContribution + MidgardContribution;

    private Dictionary<GamePlayer, int> PlayerToContributionDict;

    public ConquestObjective(AbstractGameKeep keep)
    {
        Keep = keep;
        PlayerToContributionDict = new Dictionary<GamePlayer, int>();
        ResetContribution();
    }

    public void Contribute(GamePlayer contributor, int contributionValue)
    {
        if (PlayerToContributionDict.Keys.Contains(contributor))
        {
            int existing = PlayerToContributionDict[contributor];
            PlayerToContributionDict[contributor] += contributionValue;
        }
        else
            PlayerToContributionDict.Add(contributor, contributionValue);

        switch (contributor.Realm)
        {
            case eRealm.Albion:
                AlbionContribution += contributionValue;
                break;
            case eRealm.Midgard:
                MidgardContribution += contributionValue;
                break;
            case eRealm.Hibernia:
                HiberniaContribution += contributionValue;
                break;
        }
    }

    public void DoRollover()
    {
        AwardContributors();
        UpdateTotalContribution();
        PlayerToContributionDict.Clear();
        ResetContribution();
    }

    public void ConquestCapture()
    {
        DoRollover();
    }

    private void AwardContributors()
    {
        foreach (var player in PlayerToContributionDict.Keys.Where(x => PlayerToContributionDict[x] > 0))
        {
            switch (player.Realm)
            {
                case eRealm.Albion:
                    int albaward = (int)Math.Round(AlbionContribution * (PlayerToContributionDict[player] / (double) AlbionContribution));
                    if (albaward > SubTickMaxReward) albaward = SubTickMaxReward;
                    player.GainRealmPoints(albaward, false, true);
                    break;
                case eRealm.Hibernia:
                    int hibaward = (int)Math.Round(HiberniaContribution * (PlayerToContributionDict[player] / (double) HiberniaContribution));
                    if (hibaward > SubTickMaxReward) hibaward = SubTickMaxReward;
                    player.GainRealmPoints(hibaward, false, true);
                    break;
                case eRealm.Midgard:
                    int midaward = (int)Math.Round(MidgardContribution * (PlayerToContributionDict[player] / (double) MidgardContribution));
                    if (midaward > SubTickMaxReward) midaward = SubTickMaxReward;
                    player.GainRealmPoints(midaward, false, true);
                    break;
            }
        }
    }

    private void UpdateTotalContribution()
    {
        ConquestService.ConquestManager.AddSubtotalToOverallFrom(this);
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