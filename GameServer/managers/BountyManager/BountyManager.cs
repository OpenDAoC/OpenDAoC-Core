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

    private static Dictionary<eRealm, List<BountyPoster>> ActiveBounties;
    private static List<BountyPoster> PlayerBounties;

    private static BountyPoster m_nextPosterToExpire;

    private static int minBountyReward;
    private static int maxBountyReward;

    private static long bountyDuration = Properties.BOUNTY_DURATION * 60000; // 60000ms = 1 minute
    private static double bountyRate = Properties.BOUNTY_PAYOUT_RATE;

    [ScriptLoadedEvent]
    public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
    {
        GameEventMgr.AddHandler(GameLivingEvent.Dying, GreyPlayerKilled);
        GameEventMgr.AddHandler(GameLivingEvent.Dying, BountyKilled);
        GameEventMgr.AddHandler(GameLivingEvent.EnemyKilled, PlayerKilled);
    }

    public BountyManager()
    {
        minBountyReward = Properties.BOUNTY_MIN_REWARD;
        maxBountyReward = Properties.BOUNTY_MAX_REWARD;
        ActiveBounties = new Dictionary<eRealm, List<BountyPoster>>();
        PlayerBounties = new List<BountyPoster>();
        ResetBounty();
    }

    public static void ResetBounty()
    {
        if (ActiveBounties == null)
            ActiveBounties = new Dictionary<eRealm, List<BountyPoster>>();
        ActiveBounties.Clear();
    }

    public static Dictionary<eRealm, List<BountyPoster>> GetActiveBounties
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

        if (eArgs?.Killer is not GamePlayer) return;

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

            // if (playersToAward.Contains(activeBounty.Ganked)) playersToAward.Remove(activeBounty.Ganked);

            var totalReward = activeBounty.Reward;

            var loyaltyHoursReward = (int)(totalReward / 50); // 50g = 1 hour of loyalty
            
            var reward = totalReward / playersToAwardCount * 10000; // *10000 as DOL is expecting the value in copper
            foreach (GamePlayer player in playersToAward)
            {
                var playerLoyalty = LoyaltyManager.GetPlayerRealmLoyalty(player);

                if (playerLoyalty.Days >= 30)
                {
                    bountyRate = 1;
                }

                if (playerLoyalty.Days < 3) continue;
                
                reward = (int)(reward * bountyRate);
                
                LoyaltyManager.LoyaltyUpdateAddHours(player, loyaltyHoursReward);
                player.AddMoney(reward , "You have been rewarded {0} extra for killing a bounty target!");
            }

            BroadcastBountyKill(activeBounty);
            RemoveBounty(activeBounty);
        }
    }

    private static void PlayerKilled(DOLEvent e, object sender, EventArgs args)
    {
        GamePlayer player = sender as GamePlayer;

        if (player == null) return;

        if (e != GameLivingEvent.EnemyKilled) return;

        EnemyKilledEventArgs eArgs = args as EnemyKilledEventArgs;

        if (eArgs?.Target is not GamePlayer) return;

        var activeBounties = GetActiveBountiesForPlayer(player);

        if (activeBounties == null || activeBounties.Count <= 0) return;
        if (player.GetConLevel(eArgs.Target as GamePlayer) != 0) return;

        foreach (BountyPoster activeBounty in activeBounties.ToList())
        {
            var playerLoyalty = LoyaltyManager.GetPlayerRealmLoyalty(player);
            
            if (playerLoyalty.Days >= 30)
            {
                bountyRate = 1;
            }
            
            int stolenReward = (int) (activeBounty.Reward - (activeBounty.Reward * bountyRate)) * 10000;
            player.AddMoney(stolenReward, "You have stolen {0} from a bounty on you!");
            activeBounty.Reward -= stolenReward;

            if (activeBounty.Reward > minBountyReward) continue;
            BroadcastExpiration(activeBounty);
            RemoveBounty(activeBounty);
        }
    }

    public static void AddBounty(GamePlayer killed, GamePlayer killer, int amount = 50)
    {
        if (amount < minBountyReward) amount = minBountyReward;

        // comment below for easier debugging
        killed.TempProperties.removeProperty(KILLEDBY);

        BountyPoster poster = new BountyPoster(killed, killer, amount);

        ActiveBounties ??= new Dictionary<eRealm, List<BountyPoster>>();
        if (ActiveBounties.Any())
        {
            if (ActiveBounties.ContainsKey(killed.Realm))
            {
                var realmBounties = ActiveBounties[killer.Realm];
                var playerBountyFound = false;
                foreach (BountyPoster bp in realmBounties)
                {
                    if (bp.Ganked != killed) continue;
                    if (bp.Target != killer)
                    {
                        killed.Out.SendMessage("You can only post one Bounty at the time!", eChatType.CT_Important,
                            eChatLoc.CL_SystemWindow);
                        return;
                    }

                    bp.Reward += amount;
                    bp.LastSeenZone = killed.CurrentZone;
                    bp.PostedTime = GameLoop.GameLoopTime;
                    playerBountyFound = true;
                }

                if (!playerBountyFound)
                {
                    ActiveBounties[killer.Realm].Add(poster);
                }
            }
        }
        else
        {
            ActiveBounties = new Dictionary<eRealm, List<BountyPoster>>();
            var realmPoster = new List<BountyPoster>();
            realmPoster.Add(poster);
            ActiveBounties.Add(killer.Realm, realmPoster);
        }

        BroadcastBounty(poster);
    }

    private static void RemoveBounty(BountyPoster bountyPoster)
    {
        foreach (var (key, e) in ActiveBounties.ToList())
        {
            foreach (BountyPoster poster in e.ToList())
            {
                if (poster == null) continue;
                if (poster != bountyPoster) continue;
                e.Remove(poster);
                return;
            }
        }
    }

    public static void CheckExpiringBounty(long tick)
    {
        long expireTime = 0;

        if (ActiveBounties.Any())
        {
            foreach (var (key, e) in ActiveBounties.ToList())
            {
                foreach (BountyPoster poster in e.ToList())
                {
                    if (poster == null) continue;
                    if (poster.PostedTime + bountyDuration >= expireTime && expireTime != 0) continue;
                    expireTime = poster.PostedTime + bountyDuration;
                    m_nextPosterToExpire = poster;
                    if (tick <= expireTime) continue;
                    
                    GamePlayer playerToReimburse = poster.Ganked;
                    
                    var posterLoyalty = LoyaltyManager.GetPlayerRealmLoyalty(playerToReimburse);

                    if (posterLoyalty.Days >= 30)
                    {
                        bountyRate = 1;
                    }

                    var reward =
                        (long) (poster.Reward * 10000 * bountyRate); // *10000 to convert to gold

                    playerToReimburse.AddMoney(reward, "You have been reimbursed {0} for your expired bounty.");

                    RemoveBounty(poster);
                    BroadcastExpiration(poster);
                    m_nextPosterToExpire = null;
                }
            }
        }
        else
        {
            m_nextPosterToExpire = null;
        }

        //debug
        // if (m_nextPosterToExpire != null)
        // {
        //     Console.WriteLine(
        //         $"bounty heartbeat {GameLoop.GameLoopTime} - next bounty to expire was posted at {m_nextPosterToExpire?.PostedTime} by {m_nextPosterToExpire?.Ganked.Name} on {m_nextPosterToExpire?.Target.Name} for {m_nextPosterToExpire?.Reward}g and will expire at {expireTime} in {(GameLoop.GameLoopTime - expireTime) / 1000}s");
        // }
        // else
        // {
        //     Console.WriteLine($"bounty heartbeat {GameLoop.GameLoopTime} - no bounties");
        // }
    }

    private static List<BountyPoster> GetActiveBountiesForPlayer(GamePlayer player)
    {
        if (ActiveBounties == null || !ActiveBounties.Any()) return null;
        PlayerBounties.Clear();
        foreach (var (key, e) in ActiveBounties?.ToList())
        {
            foreach (BountyPoster poster in e.ToList())
            {
                if (poster == null) continue;
                if (poster.Target.Name != player.Name) continue;
                PlayerBounties.Add(poster);
            }
        }

        return PlayerBounties;
    }

    private static List<BountyPoster> GetAllBounties()
    {
        if (ActiveBounties == null || !ActiveBounties.Any()) return null;
        PlayerBounties.Clear();
        foreach (var (key, e) in ActiveBounties.ToList())
        {
            foreach (BountyPoster poster in e.ToList())
            {
                if (poster == null) continue;
                PlayerBounties.Add(poster);
            }
        }

        return PlayerBounties;
    }

    private static BountyPoster GetActiveBountyForPlayerForRealm(eRealm realm, GamePlayer player)
    {
        return ActiveBounties?[realm].FirstOrDefault(x => x.Target.Name.Equals(player.Name));
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

    private static void BroadcastBountyKill(BountyPoster poster)
    {
        foreach (var client in WorldMgr.GetAllPlayingClients())
        {
            if (client.Player == null) continue;
            if (client.Player.Realm != poster.Ganked.Realm) continue;

            var message =
                $"{poster.Target.Name} has been killed and the bounty has been paid out!";

            client.Player.Out.SendMessage(message, eChatType.CT_Broadcast,
                eChatLoc.CL_SystemWindow);
        }
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
        long timeLeft;
        List<string> temp = new List<string>();
        temp.Clear();

        var activePosters = GetActiveBountiesForPlayer(player);

        if (activePosters != null)
        {
            if (activePosters.Count > 0)
            {
                temp.Add($"ATTENTION: You have {activePosters.Count} bounties on your head!");
                temp.Add("");
        
                foreach (var bounty in activePosters)
                {
                    timeLeft = bountyDuration - (GameLoop.GameLoopTime - bounty.PostedTime);
                    temp.Add(
                        $"{GlobalConstants.RealmToName(bounty.BountyRealm)} [{bounty.Reward}g - {TimeSpan.FromMilliseconds(timeLeft).Minutes}m {TimeSpan.FromMilliseconds(timeLeft).Seconds}s]");
                }
        
                temp.Add("");
            }
        }

        var count = 0;
        var bountyAvailable = false;

        var allBounties = GetAllBounties();
        if (allBounties != null)
        {
            foreach (BountyPoster bounty in allBounties)
            {
                if (bounty.BountyRealm != player.Realm) continue;
                count++;
                bountyAvailable = true;
                timeLeft = bountyDuration - (GameLoop.GameLoopTime - bounty.PostedTime);
                temp.Add(
                    $"{count} - {bounty.Target.Name} the {bounty.Target.CharacterClass.Name}, last seen in {bounty.LastSeenZone.Description} [{bounty.Reward}g - {TimeSpan.FromMilliseconds(timeLeft).Minutes}m {TimeSpan.FromMilliseconds(timeLeft).Seconds}s]");
            }
        }
        
        if (!bountyAvailable) temp.Add("Your Realm doesn't have any Bounty Hunt posted.");

        return temp;
    }
}