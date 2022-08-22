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
    public long LastConquestWindowStart;
    
    private int _captureAward = ServerProperties.Properties.CONQUEST_CAPTURE_AWARD;

    public eRealm ActiveConquestRealm = (eRealm)Util.Random(1, 3);

    private HashSet<GamePlayer> ContributedPlayers = new HashSet<GamePlayer>();
    private HashSet<GamePlayer> ActiveDefenders = new HashSet<GamePlayer>();

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

        AwardContributorsForRealm(CapturedKeep.Realm, true);
        RotateKeepsOnCapture(CapturedKeep);
    }
    
    public void ConquestSubCapture(AbstractGameKeep CapturedKeep)
    {
        BroadcastConquestMessageToRvRPlayers(
            $"{GetStringFromRealm(CapturedKeep.Realm)} has captured a conquest sub-objective!");

        AwardContributorsForRealm(CapturedKeep.Realm, false);
        SetKeepForCapturedRealm(CapturedKeep);
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
    
    public void ResetConquestWindow()
    {
        LastConquestWindowStart = GameLoop.GameLoopTime;
        ResetContributors();
        ActiveObjective.ResetConquestWindow();
    }

    public void AddContributor(GamePlayer player)
    {
        ContributedPlayers.Add(player);
    }

    public void AddContributors(List<GamePlayer> contributors)
    {
        ContributedPlayers ??= new HashSet<GamePlayer>();
        foreach (var player in contributors)
        {
           AddContributor(player);
        }
    }

    private void ResetContributors()
    {
        ContributedPlayers?.Clear();
        ActiveDefenders?.Clear();
    }

    public List<GamePlayer> GetContributors()
    {
        return ContributedPlayers.ToList();
    }
    
    public void AddDefenders(List<GamePlayer> contributors)
    {
        ActiveDefenders ??= new HashSet<GamePlayer>();
        foreach (var player in contributors)
        {
           AddDefender(player);
        }
    }

    public void AddDefender(GamePlayer player)
    {
        ActiveDefenders.Add(player);
        AddContributor(player);
    }

    private void ResetDefenders()
    {
        ActiveDefenders?.Clear();
    }

    public List<GamePlayer> GetDefenders()
    {
        return ActiveDefenders.ToList();
    }

    private void AwardContributorsForRealm(eRealm realmToAward, bool primaryObjective)
    {
        foreach (var player in ContributedPlayers?.ToList()?.Where(player => player.Realm == realmToAward))
        {
            int awardBase = _captureAward;
            if (!primaryObjective) awardBase /= 2;
            double flagMod = 1 + 0.25 * ActiveObjective.GetNumFlagsOwnedByRealm(player.Realm);
            player.GainRealmPoints((long)(awardBase/2 * flagMod), false);
            AtlasROGManager.GenerateOrbAmount(player, (int)(awardBase * flagMod));
        }
    }
    
    //defenders gain 1%-5% of the RPs from all kills made by other defenders
    public void AwardDefenders(int playerRpValue, GamePlayer source)
    {
        foreach (var player in ActiveDefenders.ToList())
        {
            if (player == source) continue; //don't double award the killer
            
            var loyalDays = LoyaltyManager.GetPlayerRealmLoyalty(player).Days;
            if (loyalDays > 30) loyalDays = 30;
            
            double RPFraction = 0.05 * (loyalDays / 30.0);
            if (RPFraction < 0.01) RPFraction = 0.01;
            
            player.Out.SendMessage($"You have been awarded for helping to defend your keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.GainRealmPoints((long)(playerRpValue * RPFraction), false);
        }
    }

    public bool IsPlayerNearConquest(GamePlayer player)
    {
        bool nearby = player.CurrentRegion.ID == ActiveObjective.Keep.CurrentRegion.ID;

        if (!nearby) return nearby; //bail early to skip the GetAreas call if unneeded
        
        AbstractArea area = player.CurrentZone.GetAreasOfSpot(player.X, player.Y, player.Z)
            .FirstOrDefault() as AbstractArea;

        if (((!player.CurrentZone.IsRvR && area is not {Description: "Druim Ligen"}) || player.CurrentZone.ID == 249))
            nearby = false;

        return nearby;
    }
    
    public bool IsPlayerNearFlag(GamePlayer player)
    {
        bool nearby = false;

        if (ActiveObjective.ObjectiveOne.FlagObject.GetDistance(player) <= 750)
            nearby = true;
        
        if (ActiveObjective.ObjectiveTwo.FlagObject.GetDistance(player) <= 750)
            nearby = true;
        
        if (ActiveObjective.ObjectiveThree.FlagObject.GetDistance(player) <= 750)
            nearby = true;
        
        if (ActiveObjective.ObjectiveFour.FlagObject.GetDistance(player) <= 750)
            nearby = true;

        return nearby;
    }
    
    public bool IsPlayerInConquestZone(GamePlayer player)
    {
        if (ActiveAlbionObjective == null || ActiveHiberniaObjective == null || ActiveMidgardObjective == null) return false;
        
        bool nearby = player.GetDistance(new Point2D(ActiveObjective.Keep.X, ActiveObjective.Keep.Y)) <= 50000;

        foreach (var secondaryObjective in GetSecondaryObjectives())
        {
            if (player.GetDistance(new Point2D(secondaryObjective.Keep.X, secondaryObjective.Keep.Y)) <= 10000)
                nearby = true;
        }

        if (ActiveObjective.ObjectiveOne.FlagObject.GetDistance(player) <= 750)
            nearby = true;
        
        if (ActiveObjective.ObjectiveTwo.FlagObject.GetDistance(player) <= 750)
            nearby = true;
        
        if (ActiveObjective.ObjectiveThree.FlagObject.GetDistance(player) <= 750)
            nearby = true;
        
        if (ActiveObjective.ObjectiveFour.FlagObject.GetDistance(player) <= 750)
            nearby = true;

        return nearby;
    }

    public bool IsValidDefender(GamePlayer player)
    {
        return player.GetDistance(new Point2D(ActiveObjective.Keep.X, ActiveObjective.Keep.Y)) <= 2000 && player.Realm == ActiveObjective.Keep.Realm;
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
        ActiveAlbionObjective = null;
        ActiveHiberniaObjective = null;
        ActiveMidgardObjective = null;
        //find next realm, set active objective to that realm
        if (ActiveConquestRealm == eRealm.Albion)
        {
            ActiveConquestRealm = eRealm.Hibernia;
        }
        else if (ActiveConquestRealm == eRealm.Hibernia)
        {
            ActiveConquestRealm = eRealm.Midgard;
        }
        else if (ActiveConquestRealm == eRealm.Midgard)
        {
            ActiveConquestRealm = eRealm.Albion;
        }

        if ((int) ActiveConquestRealm < 1 || (int) ActiveConquestRealm > 3)
            ActiveConquestRealm = (eRealm)Util.Random(1, 3);
        
        PredatorManager.PlayerKillTallyDict.Clear();
        
        StartConquest();
    }

    public void StartConquest()
    {
        SetDefensiveKeepForRealm(eRealm.Albion);
        SetDefensiveKeepForRealm(eRealm.Hibernia);
        SetDefensiveKeepForRealm(eRealm.Midgard);

        ActiveObjective.StartConquest();
        LastConquestStartTime = GameLoop.GameLoopTime;
        BroadcastConquestMessageToRvRPlayers($"A new Conquest has begun!");
    }

    public void StopConquest()
    {
        LastConquestStopTime = GameLoop.GameLoopTime;
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
        if (keep.Realm != keep.OriginalRealm && ((ConquestService.IsOverHalfwayDone() && keep.CurrentRegion.ID == ActiveObjective.Keep.CurrentRegion.ID) || keep.CurrentRegion.ID != ActiveObjective.Keep.CurrentRegion.ID))
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
                            keepDict[x] == objectiveWeight && x.Keep.Realm != keep.Realm)); //get a list of all keeps with the current weight
                    if (albKeepsSort.Count < 1)
                        albKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x => keepDict[x] == objectiveWeight)); 
                    
                    ActiveAlbionObjective =
                        albKeepsSort[Util.Random(albKeepsSort.Count() - 1)]; //pick one at random
                    break;
                case eRealm.Hibernia:
                    List<ConquestObjective> hibKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x =>
                        keepDict[x] == objectiveWeight && x.Keep.Realm != keep.Realm)); //get a list of all keeps with the current weight
                    if (hibKeepsSort.Count < 1)
                        hibKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x => keepDict[x] == objectiveWeight)); 
                    
                    ActiveHiberniaObjective =
                        hibKeepsSort[Util.Random(hibKeepsSort.Count() - 1)]; //pick one at random
                    break;
                case eRealm.Midgard:
                    List<ConquestObjective> midKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x =>
                        keepDict[x] == objectiveWeight && x.Keep.Realm != keep.Realm)); //get a list of all keeps with the current weight
                    if (midKeepsSort.Count < 1)
                        midKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x => keepDict[x] == objectiveWeight)); 
                    
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

    public List<ConquestObjective> GetSecondaryObjectives()
    {
        var secondaries = new List<ConquestObjective>();
        switch (ActiveConquestRealm)
        {
            case eRealm.Albion:
                secondaries.Add(ActiveHiberniaObjective);
                secondaries.Add(ActiveMidgardObjective);
                break;
            case eRealm.Hibernia:
                secondaries.Add(ActiveAlbionObjective);
                secondaries.Add(ActiveMidgardObjective);
                break;
            case eRealm.Midgard:
                secondaries.Add(ActiveAlbionObjective);
                secondaries.Add(ActiveHiberniaObjective);
                break;
        }

        return secondaries;
    }

    public IList<string> GetTextList(GamePlayer player)
    {
        List<string> temp = new List<string>();
        
        //TimeSpan.FromMilliseconds(timeSinceTaskStart).Minutes + "m " +
        //TimeSpan.FromMilliseconds(timeSinceTaskStart).Seconds + "s

        ArrayList playerCount = new ArrayList();
        playerCount = ActiveObjective.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER,
            ActiveObjective.Keep.X,
            ActiveObjective.Keep.Y, ActiveObjective.Keep.Z, 10000, playerCount, true);

        temp.Add($"{GetStringFromRealm(ActiveObjective.Keep.OriginalRealm).ToUpper()} - {ActiveObjective.Keep.CurrentZone.Description}");
        temp.Add($"{ActiveObjective.Keep.Name} | Owner: {GetStringFromRealm(ActiveObjective.Keep.Realm)}");
        temp.Add($"Players Nearby: {playerCount.Count}");
        temp.Add("");
        var secondaries = GetSecondaryObjectives();
        temp.Add("Secondary Objectives:");
        temp.Add($"{GetStringFromRealm(secondaries[0].Keep.OriginalRealm)} - {secondaries[0].Keep.Name}");
        temp.Add($"{GetStringFromRealm(secondaries[1].Keep.OriginalRealm)} - {secondaries[1].Keep.Name}");
        temp.Add("");

        if (ActiveObjective.ActiveFlags)
        {
            var locs = ActiveObjective.GetPlayerCoordsForKeep(ActiveObjective.Keep);
            temp.Add($"Capture Points | /faceflag [1 | 2 | 3 | 4]");
            temp.Add($"Owner | Nearby |   /loc   | Description");
            temp.Add($"1 | {ActiveObjective.ObjectiveOne.GetOwnerRealmName()} | {ActiveObjective.ObjectiveOne.GetNearbyPlayerCount()} | {locs[0]}");
            temp.Add($"2 | {ActiveObjective.ObjectiveTwo.GetOwnerRealmName()} | {ActiveObjective.ObjectiveTwo.GetNearbyPlayerCount()} | {locs[1]}");
            temp.Add($"3 | {ActiveObjective.ObjectiveThree.GetOwnerRealmName()} | {ActiveObjective.ObjectiveThree.GetNearbyPlayerCount()} | {locs[2]}");
            temp.Add($"4 | {ActiveObjective.ObjectiveFour.GetOwnerRealmName()} | {ActiveObjective.ObjectiveFour.GetNearbyPlayerCount()} | {locs[3]}");
        }
        
        //45 minute window, three 15-minute sub-windows with a tick in between each
        long timeUntilReset = ConquestService.GetTicksUntilContributionReset();
        long timeUntilAward = ConquestService.GetTicksUntilNextAward();

        long timeSinceTaskStart = GameLoop.GameLoopTime - ConquestService.ConquestManager.LastConquestStartTime;
        temp.Add("" );
        temp.Add("" + TimeSpan.FromMilliseconds(timeSinceTaskStart).Hours + "h " +
                 TimeSpan.FromMilliseconds(timeSinceTaskStart).Minutes + "m " +
                 TimeSpan.FromMilliseconds(timeSinceTaskStart).Seconds + "s elapsed | " + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION + "m Max");
        temp.Add("" + TimeSpan.FromMilliseconds(timeUntilReset).Minutes + "m " +
                   TimeSpan.FromMilliseconds(timeUntilReset).Seconds + "s contribution reset");
        temp.Add("" + TimeSpan.FromMilliseconds(timeUntilAward).Minutes + "m " +
                 TimeSpan.FromMilliseconds(timeUntilAward).Seconds + "s next award");
        temp.Add(ContributedPlayers.Contains(player) ? "Contribution: Qualified" : "Contribution: Not Yet Qualified");
        temp.Add("");
        //temp.Add($"Time Until Subtask Rollover: {TimeSpan.FromMilliseconds(tasktime).Minutes}m " +
        //TimeSpan.FromMilliseconds(tasktime).Seconds + "s");

        var killers = PredatorManager.GetTopKillers();
        var topNum = killers.Count;
        if (topNum > 5) topNum = 5;
        temp.Add("Predator Leaderboard:");
        if (topNum > 0)
        {
            var topKills = killers.OrderByDescending(x => x.Value);
            var enumer = topKills.GetEnumerator();
            int output = 0;
                     
            while(enumer.MoveNext() && output < topNum)
            {
                output++; 
                temp.Add($"{output} | {enumer.Current.Key.Name} | {enumer.Current.Value} kills");
            }
        }
        else
        {
            temp.Add($"--- No prey has yet been killed ---");
        }
        temp.Add($"--- Join the hunt with /predator ---");
        temp.Add("");
        temp.Add("Conquest Details:");
        temp.Add("Capture and hold field objectives around the keep to gain periodic realm point rewards and kill players near the keep or field objectives to contribute to the conquest.\n");
        temp.Add(
            "Capture the keep objective to gain an immediate orb and RP reward, or defend the keep to earn a 10% bonus to RP gains as well as increased periodic rewards.");
        /*
        temp.Add("Killing players within the area of any conquest target will contribute towards the objective. Every 5 minutes, the global contribution will be tallied and updated.\n");
        temp.Add("The conquest target will change if any of the objectives are captured, or if the conquest time expires. " +
                 "If any of the objectives are captured, the attacking realm is immediately awarded an RP bonus based off of the total accumulated contribution.");
        */

        temp.Add("");

        return temp;
    }
}