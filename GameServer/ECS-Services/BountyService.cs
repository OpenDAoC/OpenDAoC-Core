using System;
using System.Linq;
using ECS.Debug;

namespace DOL.GS;

public class BountyService
{
    private const string ServiceName = "Bounty Service";

    public static BountyManager BountyManager;
    
    private static BountyPoster m_nextPosterToExpire;
    
    static BountyService()
    {
        EntityManager.AddService(typeof(ConquestService));
        BountyManager = new BountyManager();
    }

    public static void Tick(long tick)
    {
        Diagnostics.StartPerfCounter(ServiceName);

        long bountyDuration = ServerProperties.Properties.BOUNTY_DURATION * 60000; //multiply by 60000 to accomodate for minute input

        long expireTime = 0;
        
        var allBounties = BountyManager.GetAllBounties();

        if (allBounties.Any())
        {
            foreach (BountyPoster poster in allBounties)
            {
                if (poster.PostedTime + bountyDuration >= expireTime && expireTime != 0) continue;
                expireTime = poster.PostedTime + bountyDuration;
                m_nextPosterToExpire = poster;
                if (poster.PostedTime + bountyDuration < tick)
                {
                    BountyManager.RemoveBounty(poster);
                }
            }
        }
        else
        {
            m_nextPosterToExpire = null;
        }

        Console.WriteLine($"bounty heartbeat {GameLoop.GameLoopTime} - next bounty to expire is from {m_nextPosterToExpire?.Ganked.Name} on {m_nextPosterToExpire?.Target.Name} for {m_nextPosterToExpire?.Reward}g in {GameLoop.GameLoopTime - (m_nextPosterToExpire?.PostedTime + ServerProperties.Properties.BOUNTY_DURATION * 10000)}");
        
        Diagnostics.StopPerfCounter(ServiceName);
    }
}