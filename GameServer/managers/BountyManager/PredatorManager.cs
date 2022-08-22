using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOL.Events;
using DOL.GS.API;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS;

/*
 * Predator

- Opt in at level 50 only
- Can only opt in from within an active RvR zone
- Leaving RvR zones will remove the player from the pool and put the command on 30 minute cool down
- Joining a group will remove the player from the pool and put the command on a 30 minute cooldown
- Players cannot leave the pool willingly once assigned a target (to prevent abuse)


After becoming a Predator, a player is assigned a target to kill and given their name and zone. 
Similarly, that player is also assigned as the kill target of another Predator in the system. 
If the player kills their target successfully, they are awarded a bonus of RPs and Orbs and assigned a new target.

Players can use the `/bounty predator` command to view the information on their assigned kill target. 
This information will update every 5 minutes, providing the current zone information of the target.

Predator manager holds list of current predators and their targets
Manager holds a second list of 'queued' players
When players opt in, they join the queued list. They are not considered a 'predator' until they are assigned a target, which removes them from the queued list.

Manager constructs the active list by pulling players from the queued list, assigning them a target, and adding them to the current predators list. Goal is to create a 'loop' of targets. Example:
A -> B -> C -> D -> E -> A
This ensures every player is targeted, every player has a target, and no more than one of each exist at a time.

If at any point the loop becomes broken from a player leaving RvR/dying/etc then a new loop is constructed using queued players to fill in the gap. Example:
B kills C
A -> B ->  -> D -> E -> A
Three players, F, G, and H have queued since the last update, chain is reconstructed to include them
A -> B -> F -> G -> H -> D -> E -> A

Ideally the loop and queue check/calculation would happen pretty frequently to keep queue times low. After every 30m we do a big reshuffle and refresh every target/completely reconstruct the loop to keep it fresh
 */
public class PredatorManager
{
    public static List<PredatorBounty> ActiveBounties;
    public static List<GamePlayer> QueuedPlayers;
    public static Dictionary<GamePlayer, long> DisqualifiedPlayers;
    public static List<GamePlayer> FreshKillers;
    public static Dictionary<GamePlayer, int> PlayerKillTallyDict;

    private static int minPredatorReward;
    private static int maxPredatorReward;
    private static double rewardScalar = Properties.PREDATOR_REWARD_MULTIPLIER;
    private static long OutOfBoundsTimeout = Properties.OUT_OF_BOUNDS_TIMEOUT;

    private static string TimeoutTickKey = "TimeoutStartTick";

    [ScriptLoadedEvent]
    public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
    {
        //GameEventMgr.AddHandler(GameLivingEvent.Dying, GreyPlayerKilled);
        //GameEventMgr.AddHandler(GameLivingEvent.Dying, BountyKilled);
        GameEventMgr.AddHandler(GameLivingEvent.Dying, PreyKilled);
        GameEventMgr.AddHandler(GroupEvent.MemberJoined, JoinedGroup);
        GameEventMgr.AddHandler(GamePlayerEvent.Quit, StartCooldownOnQuit);
        //GameEventMgr.AddHandler(GameLivingEvent.Moving, PreyKilled);

        //TODO add callback for player leaving RVR to DisqualifyPlayer()
        //      add callback for player joining group to DisqualifyPLayer()
        //      add callback for player logging off to DisqualifyPlayer()
        //      
    }

    static PredatorManager()
    {
        ActiveBounties = new List<PredatorBounty>();
        QueuedPlayers = new List<GamePlayer>();
        DisqualifiedPlayers = new Dictionary<GamePlayer, long>();
        FreshKillers = new List<GamePlayer>();
        PlayerKillTallyDict = new Dictionary<GamePlayer, int>();
    }

    public static void FullReset()
    {
        if (QueuedPlayers == null) QueuedPlayers = new List<GamePlayer>();
        if (ActiveBounties == null) ActiveBounties = new List<PredatorBounty>();

        //if anyone is currently hunting, put them back in the queue
        foreach (var active in ActiveBounties)
        {
            if (!QueuedPlayers.Contains(active.Predator)) QueuedPlayers.Add(active.Predator);
        }

        ActiveBounties.Clear();
        InsertQueuedPlayers();
        ConstructNewList();
        //SetTargets();
    }

    public static void QueuePlayer(GamePlayer player)
    {
        if ((QueuedPlayers != null && QueuedPlayers.Contains(player)) || PlayerIsActive(player))
        {
            player.Out.SendMessage("You are already registered in the system!", eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            return;
        }
        
        if (DisqualifiedPlayers.Keys.Contains(player))
        {
            if (DisqualifiedPlayers[player] + Properties.PREDATOR_ABUSE_TIMEOUT * 60000 >=
                GameLoop.GameLoopTime)
            {
                long timeLeft = Math.Abs(Properties.PREDATOR_ABUSE_TIMEOUT * 60000 + DisqualifiedPlayers[player] -
                                         GameLoop.GameLoopTime);
                player.Out.SendMessage("You recently abandoned the hunt. " +
                                       "Your body needs " + TimeSpan.FromMilliseconds(timeLeft).Minutes + "m "
                                       + TimeSpan.FromMilliseconds(timeLeft).Seconds + "s" +
                                       " to recover before you may rejoin.", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                return;
            }
            DisqualifiedPlayers.Remove(player);
        }

        if (!ConquestService.ConquestManager.IsPlayerNearConquest(player))
        {
            player.Out.SendMessage($"You must hunt within the active conquest zone. Use the command again when within 50,000 units of the conquest target.", eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        player.Out.SendMessage($"You tune your senses to the pulse of nature. New prey is sure to arrive soon.", eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
        QueuedPlayers.Add(player);
    }

    public static void DisqualifyPlayer(GamePlayer player)
    {
        RemoveActivePlayer(player);
        if (DisqualifiedPlayers.ContainsKey(player))
        {
            DisqualifiedPlayers[player] = GameLoop.GameLoopTime;
        }
        else
        {
            DisqualifiedPlayers.Add(player, GameLoop.GameLoopTime);
        }

        if (PlayerKillTallyDict.ContainsKey(player))
            PlayerKillTallyDict.Remove(player);

        player.Out.SendMessage($"The call of the wild leaves you. You have been removed from the hunt.",
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }

    public static void StartTimeoutCountdownFor(GamePlayer player)
    {
        if (player.PredatorTimeoutTimer.IsAlive) return;
        player.PredatorTimeoutTimer = new ECSGameTimer(player);
        player.PredatorTimeoutTimer.Properties.setProperty(TimeoutTickKey, GameLoop.GameLoopTime);
        player.PredatorTimeoutTimer.Callback = new ECSGameTimer.ECSTimerCallback(TimeoutTimerCallback);
        player.PredatorTimeoutTimer.Start(1000);
        
        player.Out.SendMessage($"You are outside of a valid hunting zone and will be removed from the pool in {Properties.OUT_OF_BOUNDS_TIMEOUT} seconds.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
    }
    
    public static void StopTimeoutCountdownFor(GamePlayer player)
    {
        player.PredatorTimeoutTimer.Stop();
        player.Out.SendMessage($"You are once again inside a valid hunting zone.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
    }

    public static void RemoveActivePlayer(GamePlayer player)
    {
        if (ActiveBounties.FirstOrDefault(x => x.Predator == player) != null)
        {
            ActiveBounties.Remove(ActiveBounties.First(x => x.Predator == player));

            PredatorBounty PreyBounty = ActiveBounties.FirstOrDefault(x => x.Prey == player);
            if (PreyBounty != null)
            {
                PreyBounty.Predator.Out.SendMessage(
                    $"Your prey has abandoned the hunt. A new target will be chosen soon.",
                    eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
                ActiveBounties.Remove(PreyBounty);
                QueuedPlayers.Add(PreyBounty.Predator);
                InsertQueuedPlayers();
                TryFillEmptyPrey();
            }
        }
    }

    public static bool PlayerIsActive(GamePlayer player)
    {
        return (ActiveBounties.FirstOrDefault(bounty => bounty.Predator == player) != null ||
                ActiveBounties.FirstOrDefault(bounty => bounty.Prey == player) != null);
    }

    public static void InsertQueuedPlayers()
    {
        if (QueuedPlayers.Count < 1) return;

        //make a new bounty with no prey for each queued player and add them to main list
        foreach (var queuedPlayer in QueuedPlayers?.ToList())
        {
            PredatorBounty newPred = new PredatorBounty(queuedPlayer, FindPreyForPlayer(queuedPlayer));
            newPred.AddReward(GetScaledReward(newPred.Prey));
            AddOrOverwriteBounty(newPred);
        }

        QueuedPlayers.Clear();
    }
    
    public static void InsertFreshKillers()
    {
        if (FreshKillers.Count < 1) return;

        //make a new bounty with no prey for each queued player and add them to main list
        foreach (var killer in FreshKillers?.ToList())
        {
            PredatorBounty newPred = new PredatorBounty(killer, FindPreyForPlayer(killer));
            newPred.AddReward(GetScaledReward(newPred.Prey));
            AddOrOverwriteBounty(newPred);
        }

        FreshKillers.Clear();
    }

    public static void TryFillEmptyPrey()
    {
        var PreylessHunters = ActiveBounties.Where(x => x.Prey == null);
        List<PredatorBounty> PredatorsToCreate = new List<PredatorBounty>();

        foreach (var preylessHunter in PreylessHunters)
        {
            PredatorBounty newPred =
                new PredatorBounty(preylessHunter.Predator, FindPreyForPlayer(preylessHunter.Predator));
            newPred.AddReward(GetScaledReward(newPred.Prey));
            PredatorsToCreate.Add(newPred);
        }

        foreach (var predatorBounty in PredatorsToCreate)
        {
            AddOrOverwriteBounty(predatorBounty);
        }
    }

    /*
     *  Manager constructs the active list by pulling players from the queued list, assigning them a target, and adding them to the current predators list. Goal is to create a 'loop' of targets. Example:
        A -> B -> C -> D -> E -> A
        This ensures every player is targeted, every player has a target, and no more than one of each exist at a time.

        If at any point the loop becomes broken from a player leaving RvR/dying/etc then a new loop is constructed using queued players to fill in the gap. Example:
        B kills C
        A -> B ->  -> D -> E -> A
        Three players, F, G, and H have queued since the last update, chain is reconstructed to include them
        A -> B -> F -> G -> H -> D -> E -> A
     
    private static void SetTargets()
    {
        Dictionary<GamePlayer, GamePlayer> predatorMap = new Dictionary<GamePlayer, GamePlayer>();
        foreach (var bounty in ActivePredators.ToArray())
        {
            predatorMap.Add(bounty.Predator, bounty.Prey);
            //Console.WriteLine($"mapping predator {bounty.Predator} prey {bounty.Prey}");
        }
        
        List<GamePlayer> Prey = predatorMap.Values.Where(x => x != null) as List<GamePlayer>;
        List<GamePlayer> Untargetted;
        //Console.WriteLine($"PreyList {Prey}");
        if (Prey == null)
        {
            Untargetted = new List<GamePlayer>(predatorMap.Keys);
        }
        else
        {
            Untargetted = predatorMap.Keys.Where(y => !Prey.Contains(y)) as List<GamePlayer>;    
        }

        //Console.WriteLine($"Untargetted {Untargetted}");
        //should return the only player who is hunted and has no target, if one exists (the left end of a broken chain - player B)
        GamePlayer PreylessHunter = Prey?.FirstOrDefault(x => predatorMap[x] == null);

        //same as above, but for the 'right' side of the chain, player D
        GamePlayer HunterlessPrey = Untargetted?.FirstOrDefault(x => predatorMap[x] != null);

        //find the starting player for our loop, the beginning of our subchain (player F)
        GamePlayer loopPlayer = Untargetted?.FirstOrDefault(x => x.Realm != PreylessHunter?.Realm);
        
        //Console.WriteLine($"PreylessHunter {PreylessHunter} HunterlessPrey {HunterlessPrey} loopPlayer {loopPlayer}");

        //no valid way to start the subchain, try and relink the existing chain if valid, or send player B into waiting room in worse case
        if (PreylessHunter == null || HunterlessPrey == null)
        {
            //if(PreylessHunter != null && HunterlessPrey != null && PreylessHunter.Realm != HunterlessPrey.Realm) AddOrOverwriteBounty(new PredatorBounty(PreylessHunter, HunterlessPrey));
            return; //bail method here, anyone without a valid bounty combo sees "waiting for a valid target" and gets picked up with next pass
        }
        
        //before we get started, hook player B to player F
        PredatorBounty subchainStart = new PredatorBounty(PreylessHunter, loopPlayer);
        AddOrOverwriteBounty(subchainStart);
        
        //loop through all untargetted players to assign prey targets
        bool moreTargets = false;
        do
        {
            //check if we have any valid prey targets (no shared realm)
            GamePlayer newPrey = Untargetted.FirstOrDefault(x => x.Realm != loopPlayer.Realm);
            
            //if we didn't find any valid targets, link current player to the right end of the chain (player D)
            if (newPrey == null)
            {
                moreTargets = false;
                PredatorBounty newPred = new PredatorBounty(loopPlayer,HunterlessPrey);
                AddOrOverwriteBounty(newPred);
            }
            else
            //if we have a valid target, set up a link between current Predator and valid Prey, 
            //then repeat this loop for the Prey to assign it a target
            {
                moreTargets = true;
                PredatorBounty newPred = new PredatorBounty(loopPlayer, newPrey);
                loopPlayer = newPrey;
                Untargetted.Remove(newPrey); //this prey is now targetted
            }
        } while (moreTargets);
        
    }*/

    private static void ConstructNewList()
    {
        Stack<GamePlayer> AlbPlayers = new Stack<GamePlayer>();
        Stack<GamePlayer> HibPlayers = new Stack<GamePlayer>();
        Stack<GamePlayer> MidPlayers = new Stack<GamePlayer>();

        //create a stack of each realms hunters
        foreach (var bounty in ActiveBounties.ToArray())
        {
            GamePlayer currentplayer = bounty.Predator;
            switch (currentplayer.Realm)
            {
                case eRealm.Albion:
                    if (!AlbPlayers.Contains(currentplayer)) AlbPlayers.Push(currentplayer);
                    //Console.WriteLine($"Mapped {currentplayer.Name} to albion {AlbPlayers.Count}");
                    break;
                case eRealm.Hibernia:
                    if (!HibPlayers.Contains(currentplayer)) HibPlayers.Push(currentplayer);
                    //Console.WriteLine($"Mapped {currentplayer.Name} to hibernia {HibPlayers.Count}");
                    break;
                case eRealm.Midgard:
                    if (!MidPlayers.Contains(currentplayer)) MidPlayers.Push(currentplayer);
                    //Console.WriteLine($"Mapped {currentplayer.Name} to midgard {MidPlayers.Count}");
                    break;
            }
        }

        List<eRealm> validRealms = new List<eRealm>();
        if (HibPlayers.Count > 0) validRealms.Add(eRealm.Hibernia);
        if (MidPlayers.Count > 0) validRealms.Add(eRealm.Midgard);
        if (AlbPlayers.Count > 0) validRealms.Add(eRealm.Albion);
        if (validRealms.Count < 2) return; //bail if not enough realms

        eRealm loopRealm = validRealms[Util.Random(validRealms.Count - 1)];

        GamePlayer LastPredator = null;

        while (validRealms.Count > 1)
        {
            if (AlbPlayers.Count == 0 && validRealms.Contains(eRealm.Albion)) validRealms.Remove(eRealm.Albion);
            if (MidPlayers.Count == 0 && validRealms.Contains(eRealm.Midgard)) validRealms.Remove(eRealm.Midgard);
            if (HibPlayers.Count == 0 && validRealms.Contains(eRealm.Hibernia)) validRealms.Remove(eRealm.Hibernia);
           // Console.WriteLine(
              //  $"ValidRealms {validRealms.Count} Hibs {HibPlayers.Count} Mids {MidPlayers.Count} Albs {AlbPlayers.Count} startrealm {loopRealm}");

            GamePlayer NextPredator = null;
            List<GamePlayer> PotentialTargets = new List<GamePlayer>();
            switch (loopRealm)
            {
                case eRealm.Albion:
                    NextPredator = AlbPlayers.Pop();
                    PotentialTargets.AddRange(HibPlayers.ToList());
                    PotentialTargets.AddRange(MidPlayers.ToList());
                    break;
                case eRealm.Hibernia:
                    NextPredator = HibPlayers.Pop();
                    PotentialTargets.AddRange(AlbPlayers.ToList());
                    PotentialTargets.AddRange(MidPlayers.ToList());
                    break;
                case eRealm.Midgard:
                    NextPredator = MidPlayers.Pop();
                    PotentialTargets.AddRange(HibPlayers.ToList());
                    PotentialTargets.AddRange(AlbPlayers.ToList());
                    break;
            }

            //Console.WriteLine($"NextPred {NextPredator} PotentialTargs {PotentialTargets?.Count}");

            LastPredator = NextPredator;
            if (PotentialTargets.Count < 1 || NextPredator == null) break;

            GamePlayer NextPrey = PotentialTargets[Util.Random(PotentialTargets.Count - 1)];
            loopRealm = NextPrey.Realm;
            //Console.WriteLine($"NextPrey {NextPrey} nextRealm {loopRealm}");

            PredatorBounty NewBounty = new PredatorBounty(NextPredator, NextPrey);
            NewBounty.AddReward(GetScaledReward(NextPrey));
            AddOrOverwriteBounty(NewBounty);
        }

        //try to find a target for the last player to be iterated on
        PredatorBounty lastBounty = new PredatorBounty(LastPredator, FindPreyForPlayer(LastPredator));
        lastBounty.AddReward(GetScaledReward(lastBounty.Prey));
        AddOrOverwriteBounty(lastBounty);
    }

    private static int GetScaledReward(GamePlayer player)
    {
        if (player == null) return 0;

        return (int) Math.Round(rewardScalar * player.RealmPointsValue);
    }

    private static GamePlayer FindPreyForPlayer(GamePlayer player)
    {
        Dictionary<GamePlayer, GamePlayer> predatorMap = new Dictionary<GamePlayer, GamePlayer>();
        foreach (var bounty in ActiveBounties.ToArray())
        {
            predatorMap.Add(bounty.Predator, bounty.Prey);
            //Console.WriteLine($"mapping predator {bounty.Predator} prey {bounty.Prey}");
        }

        List<GamePlayer> Prey = new List<GamePlayer>();
        List<GamePlayer> Untargetted = new List<GamePlayer>();

        foreach (var pred in predatorMap.Keys)
        {
            if (!predatorMap.Values.Contains(pred) && pred.Realm != player.Realm) Untargetted.Add(pred);
        }

        //Console.WriteLine($"Prey {Untargetted.FirstOrDefault()} found for predator {player}");
        return Untargetted.FirstOrDefault();
    }


    private static void AddOrOverwriteBounty(PredatorBounty bounty)
    {
        List<PredatorBounty> bountyToRemove = new List<PredatorBounty>();

        //find any existing predatorbounty with this player as the hunter
        foreach (var existingBounty in ActiveBounties?.ToList())
        {
            if (existingBounty.Predator == bounty.Predator)
            {
                bountyToRemove.Add(existingBounty);
            }
        }

        //remove everything to make sure we only have one at the end
        if (bountyToRemove.Count > 0)
        {
            foreach (var remover in bountyToRemove)
            {
                ActiveBounties.Remove(remover);
            }
        }

        //Console.WriteLine($"Adding predator {bounty.Predator} prey {bounty.Prey}");
        //insert that shiz
        ActiveBounties.Add(bounty);

        if (bounty.Predator != null && bounty.Prey != null)
        {
            bounty.Predator.Out.SendMessage($"Your primal instincts tingle. New prey has been selected.", eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
        }
    }

    public static IList<string> GetActivePrey(GamePlayer predator)
    {
        if (!PlayerIsActive(predator)) return null;

        List<string> temp = new List<string>();
        temp.Clear();

        PredatorBounty activeBounty = ActiveBounties.First(x => x.Predator == predator);

        if (activeBounty.Prey == null)
        {
            temp.Add($"Your senses sharpen, but your primal instincts do not detect any valid prey.\n" +
                     $"\n" +
                     $"Please standby and a new target will be chosen soon.");
        }
        else
        {
            GamePlayer prey = activeBounty.Prey;

            temp.Add($"Your senses sharpen, and primal instincts guide you to the location of your prey. \n" +
                     $"\n" +
                     $"Name: {prey.Name}\n" +
                     $"Race: {prey.RaceName}\n" +
                     $"Location: {prey.CurrentZone.Description}\n" +
                     $"\n The hairs on the back of your neck make you feel as though you are being watched. Beware, hunter.");
        }

        return temp;
    }
    
    public static IList<string> GetStatus()
    {
        List<string> temp = new List<string>();
        temp.Clear();
        
        temp.Add($"Active Hunters: {ActiveBounties.Count} | Queued: {QueuedPlayers.Count} | Killers: {FreshKillers.Count} \n");

        foreach (var activeBounty in ActiveBounties)
        {
            temp.Add($"Predator: {activeBounty.Predator?.Name} | Prey: {activeBounty.Prey?.Name} | Reward: {activeBounty.Reward}");
        }

        return temp;
    }

    private static void PreyKilled(DOLEvent e, object sender, EventArgs args)
    {
        GamePlayer killedPlayer = sender as GamePlayer;

        if (killedPlayer == null) return;

        if (e != GameLivingEvent.Dying) return;

        DyingEventArgs eArgs = args as DyingEventArgs;

        GamePlayer killerPlayer = eArgs.Killer as GamePlayer;

        if (killerPlayer == null) return;

        if (killerPlayer.Realm == killedPlayer.Realm) return;

        var predatorBounty = ActiveBounties.FirstOrDefault(x => x.Predator == killerPlayer);

        if (predatorBounty is null || killedPlayer != predatorBounty.Prey) return;
        
        //Console.WriteLine($"bounty {predatorBounty} pred {predatorBounty.Predator} prey {predatorBounty.Prey} ");

        ActiveBounties.Remove(predatorBounty);
        predatorBounty.Predator.Out.SendMessage($"You unleash a primal roar as the thrill of the hunt overtakes you. A feast of {predatorBounty.Reward} RPs is awarded.", eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
        BroadcastKill(predatorBounty.Prey);
        killerPlayer.GainRealmPoints(predatorBounty.Reward, false);
        FreshKillers.Add(predatorBounty.Predator);
        
        if(PlayerKillTallyDict.ContainsKey(predatorBounty.Predator))
            PlayerKillTallyDict[predatorBounty.Predator]++;
        else
            PlayerKillTallyDict.Add(predatorBounty.Predator, 1);

        //QueuePlayer(predatorBounty.Predator);
        //InsertQueuedPlayers();
        //TryFillEmptyPrey();
    }

    public static Dictionary<GamePlayer, int> GetTopKillers()
    {
        return PlayerKillTallyDict;
        
    }

    public static void BroadcastKill(GamePlayer deadGuy)
    {
        Parallel.ForEach(deadGuy.GetPlayersInRadius(10000).OfType<GamePlayer>().ToList(), player =>
        {
            player.Out.SendMessage($"A primal roar echoes nearby as a predator claims its prey.",
                eChatType.CT_ScreenCenterSmaller_And_CT_System, eChatLoc.CL_SystemWindow);
        });
    }

    private static void JoinedGroup(DOLEvent dolEvent, object sender, EventArgs arguments)
    {
        if (sender is Group g)
        {
            foreach (GamePlayer groupmate in g.GetPlayersInTheGroup())
            {
                if(PlayerIsActive(groupmate))
                    DisqualifyPlayer(groupmate);
            }
        }
    }
    
    private static void StartCooldownOnQuit(DOLEvent e, object sender, EventArgs arguments)
    {
        if(sender is GamePlayer p && PlayerIsActive(p))
            DisqualifyPlayer(p);
    }

    private static int TimeoutTimerCallback(ECSGameTimer timer)
    {
        if (timer.TimerOwner is GamePlayer pl)
        {
            if (!PredatorManager.PlayerIsActive(pl)) return 0;
            
            long TimerStartTime = timer.Properties.getProperty<long>(TimeoutTickKey);

            long secondsleft = OutOfBoundsTimeout - (GameLoop.GameLoopTime - TimerStartTime + 500) / 1000; // 500 is for rounding
            if (secondsleft > 0)
            {
                if (secondsleft == 120 || secondsleft == 90 || secondsleft == 60 || secondsleft == 30 || secondsleft == 10 || secondsleft < 5)
                {
                    pl.Out.SendMessage($"You are outside of a valid hunting zone and will be removed from the pool in {secondsleft} seconds.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }

                return 1000;
            }

            DisqualifyPlayer(pl);
            return 0;
        }

        return 0;
    }
}

public class PredatorBounty
{
    public GamePlayer Predator;
    public GamePlayer Prey;
    public int Reward;

    public PredatorBounty(GamePlayer predator, GamePlayer prey)
    {
        Predator = predator;
        Prey = prey;
    }

    public void AddReward(int contributionValue)
    {
        Reward += contributionValue;
    }
}