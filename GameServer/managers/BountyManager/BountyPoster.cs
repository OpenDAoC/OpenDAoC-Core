namespace DOL.GS;

public class BountyPoster
{
    public GamePlayer Ganked;
    public GamePlayer Target;
    public int Reward;
    public Zone LastSeenZone;

    public BountyPoster(GamePlayer ganked, GamePlayer target, Zone zone, int reward)
    {
        Ganked = ganked;
        Target = target;
        LastSeenZone = zone;
        Reward = reward;
    }
    
    public void AddReward(int contributionValue)
    {
        Reward += contributionValue;
    }
    
}