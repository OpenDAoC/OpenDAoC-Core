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

    public int HibStreak;
    public int AlbStreak;
    public int MidStreak;

    public long LastConquestStartTime;
    public long LastConquestStopTime;

    private List<GamePlayer> ContributedPlayers = new List<GamePlayer>();

    public int SumOfContributions
    {
        get { return AlbionContribution + HiberniaContribution + MidgardContribution; }
    }

    int HiberniaContribution = 0;
    int AlbionContribution = 0;
    int MidgardContribution = 0;

    public List<ConquestObjective> GetActiveObjectives
    {
        get
        {
            var list = new List<ConquestObjective>();
            list.Add(ActiveAlbionObjective);
            list.Add(ActiveHiberniaObjective);
            list.Add(ActiveMidgardObjective);
            return list;
        }
        set { }
    }

    public bool ConquestIsActive = false;

    public ConquestManager()
    {
        ResetKeeps();
        ResetObjectives();
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

    private void ResetObjectives()
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

        foreach (var activeObjective in GetActiveObjectives)
        {
            AddSubtotalToOverallFrom(activeObjective);
        }

        CheckStreak(CapturedKeep);
        AwardContributorsForRealm(CapturedKeep.Realm);
        RotateKeepsOnCapture(CapturedKeep);
        ResetContribution();
    }

    public void ConquestTimeout()
    {
        BroadcastConquestMessageToRvRPlayers(
            $"The Conquest has ended.");

        StopConquest();
        
        ResetStreak();

        ActiveAlbionObjective = null;
        ActiveHiberniaObjective = null;
        ActiveMidgardObjective = null;
    }

    private void CheckStreak(AbstractGameKeep capturedKeep)
    {
        switch (capturedKeep.Realm)
        {
            case eRealm.Albion:
                AlbStreak++;
                HibStreak = 0;
                MidStreak = 0;
                break;
            case eRealm.Hibernia:
                HibStreak++;
                AlbStreak = 0;
                MidStreak = 0;
                break;
            case eRealm.Midgard:
                MidStreak++;
                HibStreak = 0;
                AlbStreak = 0;
                break;
        }
    }

    private void ResetStreak()
    {
        AlbStreak = 0;
        HibStreak = 0;
        MidStreak = 0;
    }

    private void AwardContributorsForRealm(eRealm realmToAward)
    {
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
        }
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
        foreach (var activeObjective in GetActiveObjectives)
        {
            activeObjective.ConquestCapture();
        }
        
        for (int i = 1; i < 4; i++)
        {
            if ((eRealm) i == capturedKeep.OriginalRealm)
            {
                SetKeepForCapturedRealm(capturedKeep);
            }
            else
            {
                SetDefensiveKeepForRealm((eRealm) i);
            }
        }
    }

    public void StartConquest()
    {
        if(ActiveAlbionObjective == null) SetDefensiveKeepForRealm(eRealm.Albion);
        if(ActiveHiberniaObjective == null) SetDefensiveKeepForRealm(eRealm.Hibernia);
        if(ActiveMidgardObjective == null) SetDefensiveKeepForRealm(eRealm.Midgard);
        ResetContribution();
        LastConquestStartTime = GameLoop.GameLoopTime;
        ConquestIsActive = true;
        BroadcastConquestMessageToRvRPlayers($"A new Conquest has begun!");
    }

    public void StopConquest()
    {
        LastConquestStopTime = GameLoop.GameLoopTime;
        ConquestIsActive = false;
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
        
        if (ConquestIsActive)
        {
            temp.Add("Objective Details:");
            foreach (var activeObjective in GetActiveObjectives)
            {
                ArrayList playerCount = new ArrayList();
                playerCount = activeObjective.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER,
                    activeObjective.Keep.X,
                    activeObjective.Keep.Y, activeObjective.Keep.Z, 15000, playerCount, true);

                temp.Add($"{GetStringFromRealm(activeObjective.Keep.OriginalRealm).ToUpper()}");
                temp.Add($"{activeObjective.Keep.Name}");
                temp.Add($"Total Contribution: {activeObjective.TotalContribution}");
                temp.Add(
                    $"Hib {Math.Round((activeObjective.HiberniaContribution * 100) / (double) (activeObjective.TotalContribution  > 0 ? activeObjective.TotalContribution : 1), 2)}% | " +
                    $"Alb: {Math.Round((activeObjective.AlbionContribution  * 100)/ (double) (activeObjective.TotalContribution  > 0 ? activeObjective.TotalContribution : 1), 2) }% | " +
                    $"Mid: {Math.Round((activeObjective.MidgardContribution * 100) / (double) (activeObjective.TotalContribution  > 0 ? activeObjective.TotalContribution : 1), 2)}%");
                temp.Add($"Players Nearby: {playerCount.Count}");
                temp.Add("");
            }
            
            temp.Add($"Objective Capture Reward: {SumOfContributions}");
            temp.Add($"Hibernia: {Math.Round(HiberniaContribution * 100 / (double) (SumOfContributions > 0 ? SumOfContributions : 1), 2) }%");
            temp.Add($"Albion: {Math.Round(AlbionContribution * 100/ (double) (SumOfContributions > 0 ? SumOfContributions : 1), 2) }%");
            temp.Add($"Midgard: {Math.Round(MidgardContribution * 100 / (double) (SumOfContributions > 0 ? SumOfContributions : 1), 2) }%");

            temp.Add($"");
        
            long timeSinceTaskStart = GameLoop.GameLoopTime - ConquestService.ConquestManager.LastConquestStartTime;
            temp.Add("" + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION + "m Max Time Limit");
            temp.Add("" + TimeSpan.FromMilliseconds(timeSinceTaskStart).Minutes + "m " +
                     TimeSpan.FromMilliseconds(timeSinceTaskStart).Seconds + "s Since Conquest Start");
            temp.Add("");
        }
        else
        {
            temp.Add("No Conquest currently active.");
            long nextStartTime = (LastConquestStartTime + (ServerProperties.Properties.CONQUEST_CYCLE_TIMER * 60000 )) - GameLoop.GameLoopTime;
            temp.Add("Next Conquest will start in " + TimeSpan.FromMilliseconds(nextStartTime).Hours + "h " 
                     + TimeSpan.FromMilliseconds(nextStartTime).Minutes + "m " +
                     TimeSpan.FromMilliseconds(nextStartTime).Seconds + "s");
            temp.Add("");
        }
        

        

        //capture streak info
        int streak = 0;
        String streakingRealm = "";

        if (AlbStreak > 0)
        {
            streak = AlbStreak;
            streakingRealm = GetStringFromRealm(eRealm.Albion);
        }
        else if (HibStreak > 0)
        {
            streak = HibStreak;
            streakingRealm = GetStringFromRealm(eRealm.Hibernia);
        }
        else if (MidStreak > 0)
        {
            streak = MidStreak;
            streakingRealm = GetStringFromRealm(eRealm.Midgard);
        }

        double tmpStreak = (double) (streak * 10);

        temp.Add($"Current Capture Streak: {streak} Realm: {(streakingRealm.Equals("") ? "None" : streakingRealm)}");
        temp.Add(
            $"{(tmpStreak >= 0 ? tmpStreak : 0)}% reward from task contributions. Capture a task objective to claim the streak for your own realm, or build your realm's current streak if active.");
        
        temp.Add("");
        temp.Add("Conquest Details");
        temp.Add(
            "Killing players within the area of any conquest target will contribute towards the objective. Every 5 minutes, the global contribution will be tallied and updated.\n");
        temp.Add("The conquest target will change if any of the objectives are captured, or if the conquest time expires. " +
                 "If any of the objectives are captured, the attacking realm is immediately awarded an RP bonus based off of the total accumulated contribution.");
        
        temp.Add("");
        //temp.Add($"Time Until Subtask Rollover: {TimeSpan.FromMilliseconds(tasktime).Minutes}m " +
                 //TimeSpan.FromMilliseconds(tasktime).Seconds + "s");

        return temp;
    }
}