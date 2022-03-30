using System.Collections.Generic;
using System.Linq;
using DOL.GS;
using DOL.GS.Keeps;

namespace DOL.GS;

public class ConquestObjective
{
    public AbstractGameKeep Keep;
    public int TotalContribution;
    private Dictionary<GamePlayer, int> PlayerToContributionDict;

    public ConquestObjective(AbstractGameKeep keep)
    {
        Keep = keep;
        PlayerToContributionDict = new Dictionary<GamePlayer, int>();
        TotalContribution = 0;
    }

    public void Contribute(GamePlayer contributor, int contributionValue)
    {
        if (PlayerToContributionDict.Keys.Contains(contributor))
            PlayerToContributionDict[contributor] += contributionValue;
        else
            PlayerToContributionDict.Add(contributor, contributionValue);
        
        
    }
}