namespace DOL.GS;

public class BountyPoster
{
    public GamePlayer Ganked;
    public GamePlayer Target;
    public int Reward;
    public Zone LastSeenZone;
    public long PostedTime;

    public BountyPoster(GamePlayer ganked, GamePlayer target, Zone zone, int reward)
    {
        Ganked = ganked;
        Target = target;
        LastSeenZone = zone;
        Reward = reward;
        PostedTime = GameLoop.GameLoopTime;
    }
    
    public void AddReward(int contributionValue)
    {
        Reward += contributionValue;
    }
    
}