using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using Microsoft.AspNetCore.Components.Server;

namespace DOL.GS;

public class ConquestManager
{
    private List<DBKeep> DBKeeps;
    private List<AbstractGameKeep> _albionKeeps;
    private List<AbstractGameKeep> _hiberniaKeeps;
    private List<AbstractGameKeep> _midgardKeeps;
    private int[] albionKeepIDs = new[] {50, 51, 52, 53, 54, 55, 56};
    private int[] midgardKeepIDs = new[] {75, 76, 77, 78, 79, 80, 81};
    private int[] hiberniaKeepIDs = new[] {100, 101, 102, 103, 104, 105, 106};

    private Dictionary<ConquestObjective, int> _albionObjectives;
    private Dictionary<ConquestObjective, int> _hiberniaObjectives;
    private Dictionary<ConquestObjective, int> _midgardObjectives;

    public ConquestObjective ActiveAlbionObjective;
    public ConquestObjective ActiveHiberniaObjective;
    public ConquestObjective ActiveMidgardObjective;

    public long LastConquestStartTime;
    public long LastConquestStopTime;

    public eRealm ActiveConquestRealm = (eRealm)Util.Random(1, 3);

    private HashSet<GamePlayer> ContributedPlayers = new HashSet<GamePlayer>();

    public int SumOfContributions
    {
        get { return AlbionContribution + HiberniaContribution + MidgardContribution; }
    }

    int HiberniaContribution = 0;
    int AlbionContribution = 0;
    int MidgardContribution = 0;

    public ConquestObjective ActiveObjective
    {
        get
        {
            switch (ActiveConquestRealm)
            {
                case eRealm.Hibernia:
                    return ActiveHiberniaObjective;
                case eRealm.Albion:
                    return ActiveAlbionObjective;
                case eRealm.Midgard:
                    return ActiveMidgardObjective;
            }

            return null;
        }
        set { }
    }

    public ConquestManager()
    {
        ResetKeeps();
        PickNewObjective();
        StartConquest();
    }

    private void ResetKeeps()
    {
        if (_albionKeeps == null) _albionKeeps = new List<AbstractGameKeep>();
        if (_hiberniaKeeps == null) _hiberniaKeeps = new List<AbstractGameKeep>();
        if (_midgardKeeps == null) _midgardKeeps = new List<AbstractGameKeep>();
        _albionKeeps.Clear();
        _hiberniaKeeps.Clear();
        _midgardKeeps.Clear();
        foreach (var keep in GameServer.KeepManager.GetAllKeeps())
        {
            if (albionKeepIDs.Contains(keep.KeepID))
                _albionKeeps.Add(keep);
            if (hiberniaKeepIDs.Contains(keep.KeepID))
                _hiberniaKeeps.Add(keep);
            if (midgardKeepIDs.Contains(keep.KeepID))
                _midgardKeeps.Add(keep);
        }
    }

    private void PickNewObjective()
    {
        if (_albionObjectives == null) _albionObjectives = new Dictionary<ConquestObjective, int>();
        if (_hiberniaObjectives == null) _hiberniaObjectives = new Dictionary<ConquestObjective, int>();
        if (_midgardObjectives == null) _midgardObjectives = new Dictionary<ConquestObjective, int>();

        _albionObjectives.Clear();
        _hiberniaObjectives.Clear();
        _midgardObjectives.Clear();

        foreach (var keep in _albionKeeps)
        {
            _albionObjectives.Add(new ConquestObjective(keep), GetConquestValue(keep));
        }

        foreach (var keep in _hiberniaKeeps)
        {
            _hiberniaObjectives.Add(new ConquestObjective(keep), GetConquestValue(keep));
        }

        foreach (var keep in _midgardKeeps)
        {
            _midgardObjectives.Add(new ConquestObjective(keep), GetConquestValue(keep));
        }
    }

    private void ResetContribution()
    {
        HiberniaContribution = 0;
        AlbionContribution = 0;
        MidgardContribution = 0;
    }

    private int GetConquestValue(AbstractGameKeep keep)
    {
        switch (keep.KeepID)
        {
            case 50: //benowyc
            case 75: //bledmeer
            case 100: //crauchon
                return 1;
            //alb
            case 52: //erasleigh
            case 53: //boldiam
            case 54: //sursbrooke
            //mid
            case 76: //nottmoor
            case 77: //hlidskialf
            case 78: //blendrake
            //hib    
            case 101: //crimthain
            case 102: //bold
            case 104: //da behn
                return 2;
            //alb
            case 51: //berkstead
            case 55: //hurbury
            case 56: //renaris
            //mid                
            case 79: //glenlock
            case 80: //fensalir
            case 81: //arvakr
            //hib
            case 103: //na nGed
            case 105: //scathaig
            case 106: //ailline
                return 3;
        }

        return 1;
    }

    public void ConquestCapture(AbstractGameKeep CapturedKeep)
    {
        BroadcastConquestMessageToRvRPlayers(
            $"{GetStringFromRealm(CapturedKeep.Realm)} has captured a conquest objective!");
        
        AddSubtotalToOverallFrom(ActiveObjective);
        
        AwardContributorsForRealm(CapturedKeep.Realm);
        RotateKeepsOnCapture(CapturedKeep);
        ResetContribution();
    }

    public void ConquestTimeout()
    {
        BroadcastConquestMessageToRvRPlayers(
            $"The Conquest has ended.");

        StopConquest();

        ActiveAlbionObjective = null;
        ActiveHiberniaObjective = null;
        ActiveMidgardObjective = null;
    }

    public void AddContributors(List<GamePlayer> contributors)
    {
        ContributedPlayers ??= new HashSet<GamePlayer>();
        foreach (var player in contributors)
        {
            ContributedPlayers.Add(player);
            Console.WriteLine($"Player {player.Name} contributed!");
        }
    }

    private void AwardContributorsForRealm(eRealm realmToAward)
    {
        //TODO: rework the reward algorithm
        /*
        foreach (var conquestObjective in GetActiveObjectives)
        {
            var contributingPlayers = conquestObjective.GetContributingPlayers().Where(x => x.Realm == realmToAward);
            foreach (var contributingPlayer in contributingPlayers)
            {
                //if player is of the correct realm, award them their realm's portion of the overall reward
                if (contributingPlayer.Realm == realmToAward)
                {
                    int realmContribution = 0;
                    if (realmToAward == eRealm.Hibernia)
                        realmContribution = HiberniaContribution;
                    if (realmToAward == eRealm.Albion)
                        realmContribution = AlbionContribution;
                    if (realmToAward == eRealm.Midgard)
                        realmContribution = MidgardContribution;

                    int totalContributions = SumOfContributions > 0 ? SumOfContributions : 1;
                    int calculatedReward = (int) Math.Round((double)totalContributions * (realmContribution / ((double)totalContributions)), 2);
                    calculatedReward = (int) Math.Round((double) calculatedReward/contributingPlayers.Count()); //divide reward by number of contributors

                    if (calculatedReward > ServerProperties.Properties.MAX_KEEP_CONQUEST_RP_REWARD)
                        calculatedReward = ServerProperties.Properties.MAX_KEEP_CONQUEST_RP_REWARD;

                    if (contributingPlayer.Realm == eRealm.Hibernia && HibStreak > 0)
                        calculatedReward += calculatedReward * (HibStreak * 10) / 100;
                    if (contributingPlayer.Realm == eRealm.Albion && AlbStreak > 0)
                        calculatedReward += calculatedReward * (AlbStreak * 10) / 100;
                    if (contributingPlayer.Realm == eRealm.Midgard && MidStreak > 0)
                        calculatedReward += calculatedReward * (MidStreak * 10) / 100;

                    contributingPlayer.GainRealmPoints(calculatedReward, false, true);
                }
            }
        }*/
    }

    private string GetStringFromRealm(eRealm realm)
    {
        switch (realm)
        {
            case eRealm.Albion:
                return "Albion";
            case eRealm.Midgard:
                return "Midgard";
            case eRealm.Hibernia:
                return "Hibernia";
            default:
                return "Undefined Realm";
        }
    }

    private void RotateKeepsOnCapture(AbstractGameKeep capturedKeep)
    {
        
        ActiveObjective.ConquestCapture();
        SetKeepForCapturedRealm(capturedKeep);
        ActiveObjective.StartConquest();
        //find next offensive target for capturing realm
    }

    public void BeginNextConquest()
    {
        //find next realm, set active objective to that realm
        if (ActiveConquestRealm == eRealm.Albion)
            ActiveConquestRealm = eRealm.Hibernia;
        else if (ActiveConquestRealm == eRealm.Hibernia)
            ActiveConquestRealm = eRealm.Midgard;
        else if (ActiveConquestRealm == eRealm.Midgard)
            ActiveConquestRealm = eRealm.Albion;

        if ((int) ActiveConquestRealm < 1 || (int) ActiveConquestRealm > 3)
            ActiveConquestRealm = (eRealm)Util.Random(1, 3);
        
        StartConquest();
    }

    public void StartConquest()
    {
        if(ActiveAlbionObjective == null) SetDefensiveKeepForRealm(eRealm.Albion);
        if(ActiveHiberniaObjective == null) SetDefensiveKeepForRealm(eRealm.Hibernia);
        if(ActiveMidgardObjective == null) SetDefensiveKeepForRealm(eRealm.Midgard);
        ActiveObjective.StartConquest();
        ResetContribution();
        LastConquestStartTime = GameLoop.GameLoopTime;
        BroadcastConquestMessageToRvRPlayers($"A new Conquest has begun!");
    }

    public void StopConquest()
    {
        LastConquestStopTime = GameLoop.GameLoopTime;
    }

    public void AddSubtotalToOverallFrom(ConquestObjective objective)
    {
        HiberniaContribution += objective.HiberniaContribution;
        AlbionContribution += objective.AlbionContribution;
        MidgardContribution += objective.MidgardContribution;
    }

    private void BroadcastConquestMessageToRvRPlayers(String message)
    {
        //notify everyone an objective was captured
        foreach (var client in WorldMgr.GetAllPlayingClients())
        {
            if ((client.Player.CurrentZone.IsRvR || client.Player.Level >= 40 ) && !client.Player.CurrentZone.IsBG)
                client.Player.Out.SendMessage(message, eChatType.CT_ScreenCenterSmaller_And_CT_System,
                    eChatLoc.CL_SystemWindow);
        }
    }

    private void SetKeepForCapturedRealm(AbstractGameKeep keep)
    {
        if (keep.Realm != keep.OriginalRealm)
        {
            Dictionary<ConquestObjective, int> keepDict = new Dictionary<ConquestObjective, int>();
            switch (keep.OriginalRealm)
            {
                case eRealm.Albion:
                    keepDict = _albionObjectives;
                    break;
                case eRealm.Hibernia:
                    keepDict = _hiberniaObjectives;
                    break;
                case eRealm.Midgard:
                    keepDict = _midgardObjectives;
                    break;
            }

            //check if all keeps of the captured tier are captured
            //e.g. if a tier2 keep is captured, check for other tier 2 keeps
            bool allKeepsOfTierAreCaptured = true;
            foreach (var conquestVal in keepDict.Values.ToImmutableSortedSet())
            {
                if (conquestVal == GetConquestValue(keep))
                {
                    foreach (var conq in keepDict.Keys.Where(x => keepDict[x] == GetConquestValue(keep)))
                    {
                        if (conq.Keep.Realm == conq.Keep.OriginalRealm)
                            allKeepsOfTierAreCaptured = false;
                    }
                }
            }
            
            int objectiveWeight = GetConquestValue(keep);
            //pick an assault target in next tier if all are captured
            if (allKeepsOfTierAreCaptured && objectiveWeight < 3) objectiveWeight++;

            switch (keep.OriginalRealm)
            {
                case eRealm.Albion:
                    List<ConquestObjective> albKeepsSort =
                        new List<ConquestObjective>(keepDict.Keys.Where(x =>
                            keepDict[x] == objectiveWeight)); //get a list of all keeps with the current weight
                    ActiveAlbionObjective =
                        albKeepsSort[Util.Random(albKeepsSort.Count() - 1)]; //pick one at random
                    break;
                case eRealm.Hibernia:
                    List<ConquestObjective> hibKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x =>
                        keepDict[x] == objectiveWeight)); //get a list of all keeps with the current weight
                    ActiveHiberniaObjective =
                        hibKeepsSort[Util.Random(hibKeepsSort.Count() - 1)]; //pick one at random
                    break;
                case eRealm.Midgard:
                    List<ConquestObjective> midKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x =>
                        keepDict[x] == objectiveWeight)); //get a list of all keeps with the current weight
                    ActiveMidgardObjective =
                        midKeepsSort[Util.Random(midKeepsSort.Count() - 1)]; //pick one at random
                    break;
            }
        }
        else
        {
            SetDefensiveKeepForRealm(keep.Realm);
        }
    }

    private void SetDefensiveKeepForRealm(eRealm realm, int minimumValue)
    {
        Dictionary<ConquestObjective, int> keepDict = new Dictionary<ConquestObjective, int>();
        switch (realm)
        {
            case eRealm.Albion:
                keepDict = _albionObjectives;
                break;
            case eRealm.Hibernia:
                keepDict = _hiberniaObjectives;
                break;
            case eRealm.Midgard:
                keepDict = _midgardObjectives;
                break;
        }

        int objectiveWeight = minimumValue;

        foreach (var objective in keepDict)
        {
            if (objective.Key.Keep.OriginalRealm != objective.Key.Keep.Realm && objective.Value > objectiveWeight)
            {
                objectiveWeight = objective.Value;
            }
        }

        switch (realm)
        {
            case eRealm.Albion:
                if (objectiveWeight == 1)
                {
                    ActiveAlbionObjective = keepDict.Keys.FirstOrDefault(x => keepDict[x] == 1);
                }
                else
                {
                    List<ConquestObjective> albKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x =>
                        keepDict[x] == objectiveWeight &&
                        x.Keep.OriginalRealm != x.Keep.Realm)); //get a list of all keeps with the current weight
                    ActiveAlbionObjective = albKeepsSort[Util.Random(albKeepsSort.Count() - 1)]; //pick one at random
                }

                break;
            case eRealm.Hibernia:
                if (objectiveWeight == 1)
                {
                    ActiveHiberniaObjective = keepDict.Keys.FirstOrDefault(x => keepDict[x] == 1);
                }
                else
                {
                    List<ConquestObjective> hibKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x =>
                        keepDict[x] == objectiveWeight &&
                        x.Keep.OriginalRealm != x.Keep.Realm)); //get a list of all keeps with the current weight
                    ActiveHiberniaObjective = hibKeepsSort[Util.Random(hibKeepsSort.Count() - 1)]; //pick one at random
                }

                break;
            case eRealm.Midgard:
                if (objectiveWeight == 1)
                {
                    ActiveMidgardObjective = keepDict.Keys.FirstOrDefault(x => keepDict[x] == 1);
                }
                else
                {
                    List<ConquestObjective> midKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x =>
                        keepDict[x] == objectiveWeight &&
                        x.Keep.OriginalRealm != x.Keep.Realm)); //get a list of all keeps with the current weight
                    ActiveMidgardObjective = midKeepsSort[Util.Random(midKeepsSort.Count() - 1)]; //pick one at random
                }

                break;
        }
    }

    private void SetDefensiveKeepForRealm(eRealm realm)
    {
        SetDefensiveKeepForRealm(realm, 1);
    }

    public IList<string> GetTextList()
    {
        List<string> temp = new List<string>();

        /*
        ConquestObjective hibObj = ActiveHiberniaObjective;
        ConquestObjective albObj = ActiveAlbionObjective;
        ConquestObjective midObj = ActiveMidgardObjective;
        ArrayList hibList = new ArrayList();
        ArrayList midList = new ArrayList();
        ArrayList albList = new ArrayList();
        hibList = hibObj.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER, hibObj.Keep.X,
            hibObj.Keep.Y, hibObj.Keep.Z, 15000, hibList, true);
        albList = albObj.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER, albObj.Keep.X,
            albObj.Keep.Y, albObj.Keep.Z, 15000, albList, true);
        midList = midObj.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER, midObj.Keep.X,
            midObj.Keep.Y, midObj.Keep.Z, 15000, midList, true);
            */


        long tasktime = 300000 - ((GameLoop.GameLoopTime - LastConquestStartTime) % 300000);
        //TimeSpan.FromMilliseconds(timeSinceTaskStart).Minutes + "m " +
        //TimeSpan.FromMilliseconds(timeSinceTaskStart).Seconds + "s
        
        temp.Add("Objective Details:");
        
        ArrayList playerCount = new ArrayList();
        playerCount = ActiveObjective.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER,
            ActiveObjective.Keep.X,
            ActiveObjective.Keep.Y, ActiveObjective.Keep.Z, 10000, playerCount, true);

        temp.Add($"{GetStringFromRealm(ActiveObjective.Keep.OriginalRealm).ToUpper()}");
        temp.Add($"{ActiveObjective.Keep.Name}");
        temp.Add($"Total Points: {ActiveObjective.TotalContribution}");
        temp.Add(
            $"Hib {Math.Round((ActiveObjective.HiberniaContribution * 100) / (double) (ActiveObjective.TotalContribution  > 0 ? ActiveObjective.TotalContribution : 1), 2)}% | " +
            $"Alb: {Math.Round((ActiveObjective.AlbionContribution  * 100)/ (double) (ActiveObjective.TotalContribution  > 0 ? ActiveObjective.TotalContribution : 1), 2) }% | " +
            $"Mid: {Math.Round((ActiveObjective.MidgardContribution * 100) / (double) (ActiveObjective.TotalContribution  > 0 ? ActiveObjective.TotalContribution : 1), 2)}%");
        temp.Add($"Players Nearby: {playerCount.Count}");
        temp.Add("");

        if (ActiveObjective.ActiveFlags)
        {
            //TODO: Add flag details here
            var locs = ActiveObjective.GetPlayerCoordsForKeep(ActiveObjective.Keep);
            temp.Add($"Capture Objectives:");
            temp.Add($"{ActiveObjective.ObjectiveOne.GetOwnerRealmName()} | {locs[0]}");
            temp.Add($"{ActiveObjective.ObjectiveTwo.GetOwnerRealmName()} | {locs[1]}");
            temp.Add($"{ActiveObjective.ObjectiveThree.GetOwnerRealmName()} | {locs[2]}");
            temp.Add($"{ActiveObjective.ObjectiveFour.GetOwnerRealmName()} | {locs[3]}");
        }
       

        temp.Add($"");
    
        //TODO: Add time until next tick here
        //45 minute window, three 15-minute sub-windows with a tick in between each
        
        /*
        long timeSinceTaskStart = GameLoop.GameLoopTime - ConquestService.ConquestManager.LastConquestStartTime;
        temp.Add("" + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION + "m Max Time Limit");
        temp.Add("" + TimeSpan.FromMilliseconds(timeSinceTaskStart).Minutes + "m " +
                 TimeSpan.FromMilliseconds(timeSinceTaskStart).Seconds + "s Since Conquest Start");
        temp.Add("");
        */

        temp.Add("");
        temp.Add("Conquest Details");
        temp.Add("Capture and hold field objectives around the keep to gain periodic realm point rewards and kill players near the keep or field objectives to contribute to the conquest.\n");
        temp.Add(
            "Capture the keep objective to gain a large immediate realm point reward, or defend the keep to earn a 10% bonus to RP gains as well as increased periodic rewards.");
        /*
        temp.Add("Killing players within the area of any conquest target will contribute towards the objective. Every 5 minutes, the global contribution will be tallied and updated.\n");
        temp.Add("The conquest target will change if any of the objectives are captured, or if the conquest time expires. " +
                 "If any of the objectives are captured, the attacking realm is immediately awarded an RP bonus based off of the total accumulated contribution.");
        */
        temp.Add("");
        //temp.Add($"Time Until Subtask Rollover: {TimeSpan.FromMilliseconds(tasktime).Minutes}m " +
                 //TimeSpan.FromMilliseconds(tasktime).Seconds + "s");

        return temp;
    }
}