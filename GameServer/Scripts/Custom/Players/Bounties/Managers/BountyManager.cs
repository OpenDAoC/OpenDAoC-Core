using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS;

public class BountyManager
{
    private const string KILLEDBY = "KilledBy";

    private static HybridDictionary ActiveBounties;
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
        ActiveBounties = new HybridDictionary();
        PlayerBounties = new List<BountyPoster>();
        ResetBounty();
    }

    public static void ResetBounty()
    {
        if (ActiveBounties == null)
            ActiveBounties = new HybridDictionary();
        ActiveBounties.Clear();
    }

    public static HybridDictionary GetActiveBounties
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

        player.TempProperties.SetProperty(KILLEDBY, killer);
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

            var gainerList = killedPlayer.XPGainers;
            foreach (System.Collections.DictionaryEntry de in gainerList)
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
        killed.TempProperties.RemoveProperty(KILLEDBY);

        BountyPoster poster = new BountyPoster(killed, killer, amount);

        ActiveBounties ??= new HybridDictionary();
        if (ActiveBounties.Count > 0)
        {
            if (ActiveBounties.Contains(killed.Realm))
            {
                var realmBounties = ActiveBounties[killed.Realm] as List<BountyPoster>;
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
                    realmBounties.Add(poster);
                }
            }
        }
        else
        {
            ActiveBounties = new HybridDictionary();
            var realmPoster = new List<BountyPoster>();
            realmPoster.Add(poster);
            ActiveBounties.Add(killed.Realm, realmPoster);
        }

        BroadcastBounty(poster);
    }

    private static void RemoveBounty(BountyPoster bountyPoster)
    {
        foreach (List<BountyPoster> bPL in ActiveBounties.Values)
        {
            foreach (BountyPoster bP in bPL)
            {
                if (bP == null) continue;
                if (bP != bountyPoster) continue;
                bPL.Remove(bP);
                return;
            }
        }
    }

    public static void CheckExpiringBounty(long tick)
    {
        long expireTime = 0;

        if (ActiveBounties.Count > 0)
        {
            var activeBounties = ActiveBounties.Values;
            foreach (List<BountyPoster> bPL in activeBounties)
            {
                var bountyList = bPL.ToList();
                foreach (BountyPoster bP in bountyList)
                {
                    if (bP == null) continue;
                    if (bP.PostedTime + bountyDuration >= expireTime && expireTime != 0) continue;
                    expireTime = bP.PostedTime + bountyDuration;
                    m_nextPosterToExpire = bP;
                    if (tick <= expireTime) continue;
                
                    GamePlayer playerToReimburse = bP.Ganked;
                
                    var posterLoyalty = LoyaltyManager.GetPlayerRealmLoyalty(playerToReimburse);

                    if (posterLoyalty.Days >= 30)
                    {
                        bountyRate = 1;
                    }

                    var reward =
                        (long) (bP.Reward * 10000 * bountyRate); // *10000 to convert to gold

                    playerToReimburse.AddMoney(reward, "You have been reimbursed {0} for your expired bounty.");

                    RemoveBounty(bP);
                    BroadcastExpiration(bP);
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
        if (ActiveBounties == null || ActiveBounties.Count == 0) return null;
        PlayerBounties.Clear();

        foreach (List<BountyPoster> bPL in ActiveBounties.Values)
        {
            foreach (BountyPoster bP in bPL)
            {
                if (bP == null) continue;
                if (bP.Target.Name != player.Name) continue;
                PlayerBounties.Add(bP);
            }
        }

        return PlayerBounties;
    }

    private static List<BountyPoster> GetAllBounties()
    {
        if (ActiveBounties == null || ActiveBounties.Count == 0) return null;
        PlayerBounties.Clear();

        foreach (List<BountyPoster> bPL in ActiveBounties.Values)
        {
            foreach (BountyPoster bP in bPL)
            {
                if (bP == null) continue;
                PlayerBounties.Add(bP);
            }
        }

        return PlayerBounties;
    }

    private static BountyPoster GetActiveBountyForPlayerForRealm(eRealm realm, GamePlayer player)
    {
        foreach (List<BountyPoster> bPL in ActiveBounties.Values)
        {
            foreach (BountyPoster bP in bPL)
            {
                if (bP == null) continue;
                if (bP.Target.Name != player.Name) continue;
                if (bP.Ganked.Realm != realm) continue;
                return bP;
            }
        }

        return ActiveBounties[realm] as BountyPoster;
    }

    private static void BroadcastBounty(BountyPoster poster)
    {
        string message = $"{poster.Ganked.Name} is offering {poster.Reward} gold for the head of {poster.Target.Name} in {poster.Target.CurrentZone.Description}";

        foreach (GamePlayer player in ClientService.GetPlayersOfRealm(poster.Ganked.Realm))
        {
            player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
        }

        GamePlayer killer = ClientService.GetPlayerByPartialName(poster.Target.Name, out _);

        if (killer == null)
            return;

        string messageToKiller = $"{poster.Ganked.Name} is offering {poster.Reward} gold for your head!";
        killer.Out.SendMessage(messageToKiller, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
        killer.Out.SendMessage($"ATTENTION!\n{messageToKiller}", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
    }

    private static void BroadcastBountyKill(BountyPoster poster)
    {
        string message = $"{poster.Target.Name} has been killed and the bounty has been paid out!";

        foreach (GamePlayer player in ClientService.GetPlayersOfRealm(poster.Ganked.Realm))
            player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
    }

    private static void BroadcastExpiration(BountyPoster poster)
    {
        string message = $"{poster.Ganked.Name}'s Bounty Hunt for {poster.Target.Name}'s head has expired.";

        foreach (GamePlayer player in ClientService.GetPlayersOfRealm(poster.Ganked.Realm))
            player.Out.SendMessage(message, eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_SystemWindow);

        GamePlayer killer = ClientService.GetPlayerByPartialName(poster.Target.Name, out _);

        if (killer == null)
            return;

        string messageToKiller = $"{poster.Ganked.Name}'s Bounty Hunt for your head has expired!";
        killer.Out.SendMessage(messageToKiller, eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_SystemWindow);
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
