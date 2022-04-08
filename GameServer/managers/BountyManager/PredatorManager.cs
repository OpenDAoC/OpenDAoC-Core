using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Events;
using DOL.GS.ServerProperties;

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
    public List<PredatorBounty> ActivePredators;
    public List<GamePlayer> QueuedPlayers;
    
    [ScriptLoadedEvent]
    public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
    {
        //GameEventMgr.AddHandler(GameLivingEvent.Dying, GreyPlayerKilled);
        //GameEventMgr.AddHandler(GameLivingEvent.Dying, BountyKilled);
       // GameEventMgr.AddHandler(GameLivingEvent.EnemyKilled, PlayerKilled);
    }

    public PredatorManager()
    {
        ActivePredators = new List<PredatorBounty>();
        QueuedPlayers = new List<GamePlayer>();
    }

    public void FullReset()
    {
        //if anyone is currently hunting, put them back in the queue
        foreach (var active in ActivePredators)
        {
            if (!QueuedPlayers.Contains(active.Predator)) QueuedPlayers.Add(active.Predator);
        }

        ActivePredators.Clear();
        InsertQueuedPlayers();
        SetTargets();
    }

    private void InsertQueuedPlayers()
    {
        if (QueuedPlayers.Count < 1) return;

        //make a new bounty with no prey for each queued player and add them to main list
        foreach (var queuedPlayer in QueuedPlayers?.ToList())
        {
            PredatorBounty newPred = new PredatorBounty(queuedPlayer, null);
            newPred.Reward = 1000;
            AddOrOverwriteBounty(newPred);
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
     */
    private void SetTargets()
    {
        Dictionary<GamePlayer, GamePlayer> predatorMap = new Dictionary<GamePlayer, GamePlayer>();
        foreach (var bounty in ActivePredators)
        {
            predatorMap.Add(bounty.Predator, bounty.Prey);
        }
        
        List<GamePlayer> Prey = predatorMap.Values.Where(x => x != null) as List<GamePlayer>;
        List<GamePlayer> Untargetted;
        if (Prey == null)
        {
            Untargetted = new List<GamePlayer>(predatorMap.Keys);
        }
        else
        {
            Untargetted = predatorMap.Keys.Where(y => !Prey.Contains(y)) as List<GamePlayer>;    
        }

        //should return the only player who is hunted and has no target, if one exists (the left end of a broken chain - player B)
        GamePlayer PreylessHunter = Prey?.FirstOrDefault(x => predatorMap[x] == null);

        //same as above, but for the 'right' side of the chain, player D
        GamePlayer HunterlessPrey = Untargetted?.FirstOrDefault(x => predatorMap[x] != null);

        //find the starting player for our loop, the beginning of our subchain (player F)
        GamePlayer loopPlayer = Untargetted?.FirstOrDefault(x => x.Realm != PreylessHunter?.Realm);

        //no valid way to start the subchain, try and relink the existing chain if valid, or send player B into waiting room in worse case
        if (loopPlayer == null || PreylessHunter == null || HunterlessPrey == null)
        {
            if(PreylessHunter != null && HunterlessPrey != null && PreylessHunter.Realm != HunterlessPrey.Realm) AddOrOverwriteBounty(new PredatorBounty(PreylessHunter, HunterlessPrey));
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
        
    }
    

    private void AddOrOverwriteBounty(PredatorBounty bounty)
    {
        List<PredatorBounty> bountyToRemove = new List<PredatorBounty>();
        
        //find any existing predatorbounty with this player as the hunter
        foreach (var existingBounty in ActivePredators?.ToList())
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
                ActivePredators.Remove(remover);
            }
        }
        
        //insert that shiz
        ActivePredators.Add(bounty);
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