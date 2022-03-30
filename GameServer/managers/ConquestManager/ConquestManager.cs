using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Keeps;

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

    public long LastTaskRolloverTick;
    
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

    public ConquestManager()
    {
        ResetKeeps();
        ResetObjectives();
        RotateKeeps();
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
            if(albionKeepIDs.Contains(keep.KeepID))
                _albionKeeps.Add(keep);
            if(hiberniaKeepIDs.Contains(keep.KeepID))
                _hiberniaKeeps.Add(keep);
            if(midgardKeepIDs.Contains(keep.KeepID))
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

    private int GetConquestValue(AbstractGameKeep keep)
    {
        switch (keep.KeepID)
        {
            case 50: //benowyc
            case 75: //bledmeer
            case 100: //crauchon
                return 1;
            case 52: //erasleigh
            case 53: //boldiam
            case 54: //sursbrooke
            case 76: //nottmoor
            case 77: //hlidskialf
            case 78: //blendrake
            case 101: //crimthain
            case 102: //bold
            case 104: //da behn
                return 2;
            case 51: //berkstead
            case 55: //hurbury
            case 56: //renaris
            case 79: //glenlock
            case 80: //fensalir
            case 81: //arvakr
            case 103: //na nGed
            case 105: //scathaig
            case 106: //ailline
                return 3;
        }

        return 1;
    }

    public void RotateKeeps()
    {
        SetKeepForRealm(eRealm.Albion);
        SetKeepForRealm(eRealm.Hibernia);
        SetKeepForRealm(eRealm.Midgard);
        LastTaskRolloverTick = GameLoop.GameLoopTime;
    }

    private void SetKeepForRealm(eRealm realm)
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

        int objectiveWeight = 1;

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
                List<ConquestObjective> albKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x => keepDict[x] == objectiveWeight)); //get a list of all keeps with the current weight
                ActiveAlbionObjective = albKeepsSort[Util.Random(albKeepsSort.Count()-1)]; //pick one at random
                Console.WriteLine($"New albion objective: {ActiveAlbionObjective.Keep.Name} weight {keepDict[ActiveAlbionObjective]}");
                break;
            case eRealm.Hibernia:
                List<ConquestObjective> hibKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x => keepDict[x] == objectiveWeight)); //get a list of all keeps with the current weight
                ActiveHiberniaObjective = hibKeepsSort[Util.Random(hibKeepsSort.Count()-1)]; //pick one at random
                Console.WriteLine($"New hibernia objective: {ActiveHiberniaObjective.Keep.Name} weight {keepDict[ActiveHiberniaObjective]}");
                break;
            case eRealm.Midgard:
                List<ConquestObjective> midKeepsSort = new List<ConquestObjective>(keepDict.Keys.Where(x => keepDict[x] == objectiveWeight)); //get a list of all keeps with the current weight
                ActiveMidgardObjective = midKeepsSort[Util.Random(midKeepsSort.Count()-1)]; //pick one at random
                Console.WriteLine($"New midgard objective: {ActiveMidgardObjective.Keep.Name} weight {keepDict[ActiveMidgardObjective]}");
                break;
        }
    }


}