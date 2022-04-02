using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS;

public class BountyManager

{
    private const string KILLEDBY = "KilledBy";

    private static List<BountyPoster> ActiveBounties;
    
    private static BountyPoster m_nextPosterToExpire;

    private static long bountyDuration = Properties.BOUNTY_DURATION * 60000; // 60000ms = 1 minute

    [ScriptLoadedEvent]
    public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
    {
        GameEventMgr.AddHandler(GameLivingEvent.Dying, GreyPlayerKilled);
        GameEventMgr.AddHandler(GameLivingEvent.Dying, BountyKilled);
    }

    public BountyManager()
    {
        ResetBounty();
    }

    public static void ResetBounty()
    {
        if (ActiveBounties == null)
            ActiveBounties = new List<BountyPoster>();
        ActiveBounties.Clear();
    }

    public static List<BountyPoster> GetActiveBounties
    {
        get { return ActiveBounties; }
        set { }
    }

    private static void GreyPlayerKilled(DOLEvent e, object sender, EventArgs args)
    {
        GamePlayer player = sender as GamePlayer;

        if (player == null) return;

        if (e != GameLivingEvent.Dying) return;

        DyingEventArgs eArgs = args as DyingEventArgs;

        if (eArgs.Killer is not GamePlayer) return;

        GamePlayer killer = eArgs.Killer as GamePlayer;

        if (player.Realm == killer?.Realm) return;

        if (!(killer?.GetConLevel(player) <= -3)) return;

        player.TempProperties.setProperty(KILLEDBY, killer);
        player.Out.SendMessage($"Use /bounty add <amount> if you want to call a bounty for {killer.Name}'s head!",
            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
    }

    private static void BountyKilled(DOLEvent e, object sender, EventArgs args)
    {
        GamePlayer killedPlayer = sender as GamePlayer;

        if (killedPlayer == null) return;

        if (e != GameLivingEvent.Dying) return;
        
        DyingEventArgs eArgs = args as DyingEventArgs;
        
        GamePlayer killerPlayer = eArgs.Killer as GamePlayer;
        
        if (killerPlayer == null) return;
        
        if (killerPlayer.Realm == killedPlayer.Realm) return;

        var activeBounties = GetActiveBountiesForPlayer(killedPlayer);
        
        if (activeBounties is null || !activeBounties.Any()) return;
        
        foreach (BountyPoster activeBounty in activeBounties.ToList())
        {
            List<GamePlayer> playersToAward = new List<GamePlayer>();

            foreach (System.Collections.DictionaryEntry de in killedPlayer.XPGainers)
            {
                GameLiving living = de.Key as GameLiving;
                GamePlayer player = living as GamePlayer;
                if (player == null) continue;
                if (player.Realm == activeBounty.BountyRealm)
                {
                    playersToAward.Add(player);
                }
            }
            
            var playersToAwardCount = playersToAward.Count;
            
            if (playersToAward.Contains(activeBounty.Ganked)) playersToAward.Remove(activeBounty.Ganked);

            var reward = (long)(activeBounty.Reward * 10000 * Properties.BOUNTY_PAYOUT_RATE ) / playersToAwardCount; // *10000 to convert to gold
            foreach (GamePlayer player in playersToAward)
            {
                player.AddMoney(reward, "You have been rewarded {0} extra for killing a bounty target!");
            }
            RemoveBounty(activeBounty);
        }
    }
    
    public static void AddBounty(GamePlayer killed, GamePlayer killer, int amount = 50)
    {
        if (amount < 50) amount = 50;

        // this is commented for debugging
        // killed.TempProperties.removeProperty(KILLEDBY);

        BountyPoster poster = new BountyPoster(killed, killer, amount);

        ActiveBounties ??= new List<BountyPoster>();

        if (ActiveBounties.Any())
        {
            var previousPoster = GetBountyPostedBy(killed);
            
            if (previousPoster.Target != killer)
            {
                killed.Out.SendMessage("You can only post one Bounty at the time!", eChatType.CT_Important,
                    eChatLoc.CL_SystemWindow);
            }
            
            //search for existing killer and increment if they exist, add them to the list if they don't
            var activePoster = GetActiveBountyForPlayerForRealm(killer, killed.Realm);
            if (activePoster != null)
            {
                activePoster.Reward += amount;
                activePoster.LastSeenZone = killed.CurrentZone;
                activePoster.PostedTime = GameLoop.GameLoopTime;
            }
            else
            {
                ActiveBounties.Add(poster);
            }
        }
        else
        {
            //add if its the first entry
            ActiveBounties.Add(poster);
        }
        BroadcastBounty(poster);

    }

    private static void RemoveBounty(BountyPoster bountyPoster)
    {
        if (ActiveBounties.Contains(bountyPoster))
        {
                ActiveBounties.Remove(bountyPoster);
                return;
        }
        Console.WriteLine("Bounty to remove not found");
    }

    public static void CheckExpiringBounty(long tick)
    {

        long expireTime = 0;
        
            if (ActiveBounties.Any())
            {
                foreach (BountyPoster poster in ActiveBounties.ToList())
                {
                    if (poster == null) continue;
                    if (poster.PostedTime + bountyDuration >= expireTime && expireTime != 0) continue;
                    expireTime = poster.PostedTime + bountyDuration;
                    m_nextPosterToExpire = poster;
                    if (tick <= expireTime) continue;
                    
                    var reward = (long) (poster.Reward * 10000 * ServerProperties.Properties.BOUNTY_PAYOUT_RATE); // *10000 to convert to gold
                    GamePlayer playerToReimburse = poster.Ganked;
                    playerToReimburse.AddMoney(reward, "You have been reimbursed {0} for your expired bounty.");
                    
                    RemoveBounty(poster);
                    BroadcastExpiration(poster);
                    m_nextPosterToExpire = null;
                }
            }
            else
            {
                m_nextPosterToExpire = null;
            }
        
        //debug
        if (m_nextPosterToExpire != null)
        {
            Console.WriteLine($"bounty heartbeat {GameLoop.GameLoopTime} - next bounty to expire was posted at {m_nextPosterToExpire?.PostedTime} by {m_nextPosterToExpire?.Ganked.Name} on {m_nextPosterToExpire?.Target.Name} for {m_nextPosterToExpire?.Reward}g and will expire at {expireTime} in {(GameLoop.GameLoopTime - expireTime)/1000}s");
        }
        else
        {
            Console.WriteLine($"bounty heartbeat {GameLoop.GameLoopTime} - no bounties");
        }

    }

    private static List<BountyPoster> GetActiveBountiesForPlayer(GamePlayer player)
    {
        return ActiveBounties.FindAll(x => x.Target.Name.Equals(player.Name));
    }

    private static BountyPoster GetActiveBountyForPlayerForRealm(GamePlayer player, eRealm realm)
    {
        return ActiveBounties.FirstOrDefault(x => x.Target.Name.Equals(player.Name) && x.BountyRealm == realm);
    }

    public static List<BountyPoster> GetAllBounties()
    {
        return ActiveBounties;
        ;
    }

    private static BountyPoster GetBountyPostedBy(GamePlayer player)
    {
        return ActiveBounties.FirstOrDefault(x => x.Ganked.Name.Equals(player.Name));
    }

    private static void BroadcastBounty(BountyPoster poster)
    {
        foreach (var client in WorldMgr.GetAllPlayingClients())
        {
            if (client.Player == null) continue;
            if (client.Player.Realm != poster.Ganked.Realm) continue;

            var message =
                $"{poster.Ganked.Name} is offering {poster.Reward} gold for the head of {poster.Target.Name} in {poster.Target.CurrentZone.Description}";

            client.Player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller,
                eChatLoc.CL_SystemWindow);
            client.Player.Out.SendMessage(message, eChatType.CT_Broadcast,
                eChatLoc.CL_SystemWindow);
        }

        var killerClient = WorldMgr.GetClientByPlayerName(poster.Target.Name, false, true);

        if (killerClient == null) return;

        var messageToKiller =
            $"{poster.Ganked.Name} is offering {poster.Reward} gold for your head!";

        killerClient.Player.Out.SendMessage(messageToKiller, eChatType.CT_ScreenCenter,
            eChatLoc.CL_SystemWindow);
        killerClient.Player.Out.SendMessage($"ATTENTION!\n{messageToKiller}", eChatType.CT_Important,
            eChatLoc.CL_SystemWindow);
    }
    private static void BroadcastExpiration(BountyPoster poster)
    {
        foreach (var client in WorldMgr.GetAllPlayingClients())
        {
            if (client.Player == null) continue;
            if (client.Player.Realm != poster.Ganked.Realm) continue;

            var message =
                $"{poster.Ganked.Name}'s Bounty Hunt for {poster.Target.Name}'s head has expired.";

            client.Player.Out.SendMessage(message, eChatType.CT_ScreenCenter_And_CT_System,
                eChatLoc.CL_SystemWindow);
        }
        
        var killerClient = WorldMgr.GetClientByPlayerName(poster.Target.Name, false, true);

        if (killerClient == null) return;

        var messageToKiller =
            $"{poster.Ganked.Name}'s Bounty Hunt for your head has expired!";

        killerClient.Player.Out.SendMessage(messageToKiller, eChatType.CT_ScreenCenter_And_CT_System,
            eChatLoc.CL_SystemWindow);

    }
    public static IList<string> GetTextList(GamePlayer player)
    {
        List<string> temp = new List<string>();
        temp.Clear();

        if (ActiveBounties == null || ActiveBounties.Count == 0)
        {
            temp.Add("Your Realm doesn't have any Bounty Hunt posted.");
            return temp;
        }

        var activePosters = GetActiveBountiesForPlayer(player);
        
        if (activePosters.Count > 0)
        {
            temp.Add($"ATTENTION: You have {activePosters.Count} bounties on your head!");
            temp.Add("");
            
            foreach (var bounty in activePosters)
            {
                temp.Add($"[{bounty.BountyRealm}] {bounty.Reward}g reward");
            }
            temp.Add("");
        }

        var count = 0;
        var bountyAvailable = false;
        foreach (BountyPoster bounty in ActiveBounties)
        {
            if (bounty.BountyRealm != player.Realm) continue;
            count++;
            bountyAvailable = true;
            var expireTime = bounty.PostedTime + bountyDuration;
            // var timeLeft = Properties.BOUNTY_DURATION - (expireTime - GameLoop.GameLoopTime);
            var timeLeft = bountyDuration - (GameLoop.GameLoopTime - bounty.PostedTime);
            temp.Add($"{count} - {bounty.Target.Name} the {bounty.Target.CharacterClass.Name}, last seen in {bounty.LastSeenZone.Description} [{bounty.Reward}g - {TimeSpan.FromMilliseconds(timeLeft).Minutes}m {TimeSpan.FromMilliseconds(timeLeft).Seconds}s]");
        }

        if (!bountyAvailable) temp.Add("Your Realm doesn't have any Bounty Hunt posted.");

        return temp;
    }
}