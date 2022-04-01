namespace DOL.GS;

public class BountyPoster
{
    public GamePlayer Ganked;
    public GamePlayer Target;
    public int Reward;

    public BountyPoster(GamePlayer ganked, GamePlayer target, int reward)
    {
        Ganked = ganked;
        Target = target;
        Reward = reward;
    }
    
    public void AddReward(int contributionValue)
    {
        Reward += contributionValue;
    }
    
}