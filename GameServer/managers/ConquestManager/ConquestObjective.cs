using System.Collections.Generic;
using DOL.GS;
using DOL.GS.Keeps;

namespace DOL.GS;

public class ConquestObjective
{
    public AbstractGameKeep Keep;
    public int TotalRPReward;
    private Dictionary<GamePlayer, int> PlayerToContributionDict;

    public ConquestObjective(AbstractGameKeep keep)
    {
        Keep = keep;
        PlayerToContributionDict = new Dictionary<GamePlayer, int>();
        TotalRPReward = 0;
    }
}