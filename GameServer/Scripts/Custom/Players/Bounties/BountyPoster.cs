namespace DOL.GS;

public class BountyPoster
{
    public eRealm BountyRealm;
    public GamePlayer Ganked;
    public GamePlayer Target;
    public int Reward;
    public Zone LastSeenZone;
    public long PostedTime;

    public BountyPoster(GamePlayer ganked, GamePlayer target, int reward)
    {
        BountyRealm = ganked.Realm;
        Ganked = ganked;
        Target = target;
        LastSeenZone = target.CurrentZone;
        Reward = reward;
        PostedTime = GameLoop.GameLoopTime;
    }
    
    public void AddReward(int contributionValue)
    {
        Reward += contributionValue;
    }
    
}