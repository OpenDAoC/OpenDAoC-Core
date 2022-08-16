using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS;

public class ConquestObjective
{
    public AbstractGameKeep Keep;
    public int AlbionContribution;
    public int MidgardContribution;
    public int HiberniaContribution;
    public long LastRolloverTick = 0;
    public long StartTick;

    public int TotalContribution => AlbionContribution + HiberniaContribution + MidgardContribution;
    
    private int _realmPointTickAward = ServerProperties.Properties.SUBTICK_RP_AWARD;

    public SubObjective ObjectiveOne;
    public SubObjective ObjectiveTwo;
    public SubObjective ObjectiveThree;
    public SubObjective ObjectiveFour;

    public bool ActiveFlags => ObjectiveOne != null && ObjectiveTwo != null && ObjectiveThree != null && ObjectiveFour != null;

    public ConquestObjective(AbstractGameKeep keep)
    {
        Keep = keep;
        ResetContribution();
    }

    public void StartConquest()
    {
        InitializeFlags(Keep);
        StartTick = GameLoop.GameLoopTime;
        LastRolloverTick = StartTick;
        ResetContribution();
    }

    private void InitializeFlags(AbstractGameKeep keep)
    {
        var locs = GetFlagLocsForKeep(keep);
        ObjectiveOne = new SubObjective(locs[0].X, locs[0].Y, locs[0].Z, keep);
        ObjectiveTwo = new SubObjective(locs[1].X, locs[1].Y, locs[1].Z, keep);
        ObjectiveThree = new SubObjective(locs[2].X, locs[2].Y, locs[2].Z, keep);
        ObjectiveFour = new SubObjective(locs[3].X, locs[3].Y, locs[3].Z, keep);
    }

    private List<Point3D> GetFlagLocsForKeep(AbstractGameKeep keep)
    {
        List<Point3D> flagLocs = new List<Point3D>();

        switch (keep.Name.ToLower())
        {
            //mid
            case "bledmeer faste":
                flagLocs.Add(new Point3D(638715, 586232, 5775)); //near hmg *
                flagLocs.Add(new Point3D(629230, 633131, 5352)); //near amg *
                flagLocs.Add(new Point3D(653793, 622482, 7714)); //near jamtland*
                flagLocs.Add(new Point3D(651145, 603241, 7985)); //small fort *
                break;
            case "hlidskialf faste":
            case "nottmoor faste":
            case "blendrake faste":
            case "glenlock faste":
                flagLocs.Add(new Point3D(718166, 653303, 6374)); //near dodens 
                flagLocs.Add(new Point3D(694299, 616589, 4897)); //northern alcove
                flagLocs.Add(new Point3D(668904, 645171, 6930)); //western tree hill
                flagLocs.Add(new Point3D(693936, 661756, 4050)); //south of lakes
                break;
            case "arvakr faste":
                flagLocs.Add(new Point3D(670138, 682754, 5728)); //northwestern valley
                flagLocs.Add(new Point3D(708251, 690899, 8381)); //northeastern glen
                flagLocs.Add(new Point3D(713566, 714754, 9744)); //hilltop camp
                flagLocs.Add(new Point3D(679291, 718035, 5708)); //northern lakeside
                break;
            case "fensalir faste":
                flagLocs.Add(new Point3D(727721, 657606, 5938)); //West of Fensalir
                flagLocs.Add(new Point3D(740088, 625366, 12385)); //Northwestern Hill
                flagLocs.Add(new Point3D(756846, 636505, 5073)); //Forest Glen
                flagLocs.Add(new Point3D(759143, 654099, 5960)); //Inner Valley
                break;
            //hib
            case "dun crauchon":
                flagLocs.Add(new Point3D(439958, 290374, 7457)); //clifftop overlook
                flagLocs.Add(new Point3D(432504, 309447, 4968)); //clifftop valley
                flagLocs.Add(new Point3D(440610, 337069, 3960)); //crossroad tower
                flagLocs.Add(new Point3D(424956, 337286, 5808)); //hilltop fortress
                break;
            case "dun bolg":
            case "dun nged":
            case "dun da behnn":
            case "dun crimthain":
                flagLocs.Add(new Point3D(420626, 407617, 4248)); //between nged and behn
                flagLocs.Add(new Point3D(434289, 383867, 3080)); //evern overlook
                flagLocs.Add(new Point3D(418960, 382760, 1946)); //stone gates
                flagLocs.Add(new Point3D(386754, 377633, 3616)); //western tower
                break;
            case "dun ailinne":
                flagLocs.Add(new Point3D(336874, 392157, 3263)); //near ligen
                flagLocs.Add(new Point3D(336031, 372887, 3126)); //claurican fields
                flagLocs.Add(new Point3D(377827, 406480, 6854)); //ailinne plateau
                flagLocs.Add(new Point3D(372816, 370323, 3535)); //roadside pillar
                break;
            case "dun scathaig":
                flagLocs.Add(new Point3D(424947, 425528, 1687)); //three huts
                flagLocs.Add(new Point3D(444481, 461523, 3896)); //betwixt dead trees
                flagLocs.Add(new Point3D(390131, 455845, 3727)); //western valley
                flagLocs.Add(new Point3D(397177, 437251, 1792)); //faeghoul alley
                break;
            
            //albion
            case "caer benowyc":
                flagLocs.Add(new Point3D(604900, 332807, 4200)); //near hmg
                flagLocs.Add(new Point3D(658249, 334508, 4312)); //near mmg
                flagLocs.Add(new Point3D(635787, 330176, 5194)); //hilltop tower
                flagLocs.Add(new Point3D(623330, 338609, 4127)); //behind taco bell
                break;
            case "caer sursbrooke":
            case "caer erasleigh":
            case "caer berkstead":
            case "caer boldiam":
                flagLocs.Add(new Point3D(571890, 372990, 4648)); //northwestern hill
                flagLocs.Add(new Point3D(603833, 364123, 6568)); //boar plateau
                flagLocs.Add(new Point3D(614293, 397396, 4312)); //boldiam underlook
                flagLocs.Add(new Point3D(593066, 410307, 4984)); //southern border
                break;
            case "caer renaris":
                flagLocs.Add(new Point3D(616897, 469399, 3066)); //SE forest
                flagLocs.Add(new Point3D(615302, 432450, 2970)); //NE river-side
                flagLocs.Add(new Point3D(574704, 443837, 3004)); //south of ren
                flagLocs.Add(new Point3D(573076, 463390, 2970)); //SW near lake
                break;
            case "caer hurbury":
                flagLocs.Add(new Point3D(561438, 361288, 2896)); //pennine border
                flagLocs.Add(new Point3D(558434, 320483, 2810)); //cyclops city
                flagLocs.Add(new Point3D(531313, 335228, 7095)); //mid-zone tower
                flagLocs.Add(new Point3D(524836, 321065, 6928)); //myrddin approach
                break;
            default:
                flagLocs.Add(new Point3D(0, 0, 0)); //near hmg
                flagLocs.Add(new Point3D(0, 0, 0)); //near mmg
                flagLocs.Add(new Point3D(0, 0, 0)); //near the keep
                flagLocs.Add(new Point3D(0, 0, 0)); //southern crossroads
                break;
                
        }
        
        return flagLocs;
    }

    public List<String> GetPlayerCoordsForKeep(AbstractGameKeep keep)
    {
        List<String> flagLocs = new List<String>();

        switch (keep.Name.ToLower())
        {
            
            //mid
            case "bledmeer faste":
                flagLocs.Add("X:42361 Y:11658 | Hibernia Milegate"); 
                flagLocs.Add("X:39243 Y:57577 | Albion Milegate"); 
                flagLocs.Add("X:59073 Y:23499 | Giant Overlook"); 
                flagLocs.Add("X:46228 Y:27925 | Hilltop Watch"); 
                break;
            case "hlidskialf faste":
            case "nottmoor faste":
            case "blendrake faste":
            case "glenlock faste":
                flagLocs.Add("X:61300 Y:41900 | Near Dodens"); 
                flagLocs.Add("X:38946 Y:10371 | Northern Alcove"); 
                flagLocs.Add("X:11565 Y:39432 | Western Hill"); 
                flagLocs.Add("X:38908 Y:55551 | Glenlock Lakes"); 
                break;
            case "arvakr faste":
                flagLocs.Add("X:14778 Y:11000 | Northwestern Valley"); 
                flagLocs.Add("X:52891 Y:19155 | Northeastern Glen"); 
                flagLocs.Add("X:58206 Y:43010 | Hilltop Camp"); 
                flagLocs.Add("X:23931 Y:46291 | Northern Lakeshore"); 
                break;
            case "fensalir faste":
                flagLocs.Add("X: 6825 Y:51398 | West of Fensalir"); 
                flagLocs.Add("X:19192 Y:19158 | Northwestern Hill"); 
                flagLocs.Add("X:35950 Y:30297 | Forest Glen"); 
                flagLocs.Add("X:38247 Y:47891 | Inner Valley"); 
                break;
            //hib
            case "dun crauchon":
                flagLocs.Add("X:22166 Y: 3645 | Clifftop Overlook"); 
                flagLocs.Add("X:14712 Y:22727 | Clifftop Valley"); 
                flagLocs.Add("X:22818 Y:50349 | Crossroad Tower"); 
                flagLocs.Add("X: 7164 Y:50566 | Hilltop Fortress"); 
                break;
            case "dun bolg":
            case "dun nged":
            case "dun da behnn":
            case "dun crimthain":
                flagLocs.Add("X:35602 Y:55361 | Between nGed and Behn"); 
                flagLocs.Add("X:49265 Y:31611 | Evern Overlook"); 
                flagLocs.Add("X:33936 Y:30504 | Stone Gates"); 
                flagLocs.Add("X: 1730 Y:25377 | Western Tower"); 
                break;
            case "dun ailinne":
                flagLocs.Add("X:16543 Y:20631 | Claurican Fields"); 
                flagLocs.Add("X:17386 Y:39901 | Field near Ligen"); 
                flagLocs.Add("X:58339 Y:54224 | Ailinne Plateau"); 
                flagLocs.Add("X:53328 Y:18067 | Roadside Pillar"); 
                break;
            case "dun scathaig":
                flagLocs.Add("X:39923 Y: 7736 | Three Huts"); 
                flagLocs.Add("X:59457 Y:43731 | Betwixt Dead Trees"); 
                flagLocs.Add("X: 5600 Y:24225 | Western Valley"); 
                flagLocs.Add("X:12153 Y:19459 | Faeghoul Alley"); 
                break;
            //albion
            case "caer benowyc":
                flagLocs.Add("X: 6884 Y:46087 | Hibernia Milegate");
                flagLocs.Add("X:60233 Y:47788 | Midgard Milegate"); 
                flagLocs.Add("X:37771 Y:43456 | Hilltop Tower");
                flagLocs.Add("X:25313 Y:51889 | Legionnaire Fortress"); 
                break;            
            case "caer sursbrooke":
            case "caer erasleigh":
            case "caer berkstead":
            case "caer boldiam":
                flagLocs.Add("X: 6642 Y:20734 | Western Hill"); 
                flagLocs.Add("X:38585 Y:11867 | Boar Plateau"); 
                flagLocs.Add("X:49045 Y:45140 | Boldiam Underlook"); 
                flagLocs.Add("X:27818 Y:58051 | Southern Border"); 
                break;
            case "caer renaris":
                flagLocs.Add("X:51649 Y:51607 | Southeast Forest"); 
                flagLocs.Add("X:50054 Y:14658 | Northeast Riverside"); 
                flagLocs.Add("X: 9456 Y:26045 | South of Renaris"); 
                flagLocs.Add("X: 7828 Y:45598 | Southwestern Lake"); 
                break;
            case "caer hurbury":
                flagLocs.Add("X:61721 Y:58185 | Pennine Border"); 
                flagLocs.Add("X:58722 Y:17379 | Cyclops City"); 
                flagLocs.Add("X:31601 Y:32124 | Mid-Zone Tower"); 
                flagLocs.Add("X:25124 Y:17961 | Myrddin Approach"); 
                break;
            default:
                flagLocs.Add(""); 
                flagLocs.Add(""); 
                flagLocs.Add("");
                flagLocs.Add(""); 
                break;
                
        }
        
        return flagLocs;
    }

    public void DoPeriodicReward()
    {
        AwardContributors();
        ResetContribution();
        LastRolloverTick = GameLoop.GameLoopTime;
        Console.WriteLine($"Periodic Reward for objective: {this}");
    }

    public void ConquestCapture()
    {
        //TODO: make a capture reward here
        ResetObjective();
    }

    private void ResetObjective()
    {
        ObjectiveOne.Cleanup();
        ObjectiveTwo.Cleanup();
        ObjectiveThree.Cleanup();
        ObjectiveFour.Cleanup();
        
        ObjectiveOne = null;
        ObjectiveTwo = null;
        ObjectiveThree = null;
        ObjectiveFour = null;
        
        ResetContribution();
    }

    private void AwardContributors()
    {
        foreach (GamePlayer player in ConquestService.ConquestManager.GetContributors())
        {
            if (player.CurrentRegion != Keep.CurrentRegion || player.Level < 40) continue;
            player.Out.SendMessage($"The realm thanks you for your efforts in the conquest.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            int RPBase = _realmPointTickAward;
            double flagMod = 1 + 0.25 * GetNumFlagsOwnedByRealm(player.Realm);
            player.GainRealmPoints((long)(RPBase * flagMod), false);
        }
    }

    public int GetNumFlagsOwnedByRealm(eRealm realm)
    {
        int output = 0;
        if (ObjectiveOne.OwningRealm == realm) output++;
        if (ObjectiveTwo.OwningRealm == realm) output++;
        if (ObjectiveThree.OwningRealm == realm) output++;
        if (ObjectiveFour.OwningRealm == realm) output++;
        return output;
    }

    public void CheckNearbyPlayers()
    {
        ObjectiveOne?.CheckNearbyPlayers();
        ObjectiveTwo?.CheckNearbyPlayers();
        ObjectiveThree?.CheckNearbyPlayers();
        ObjectiveFour?.CheckNearbyPlayers();
    }

    private void ResetContribution()
    {
        AlbionContribution = 0;
        HiberniaContribution = 0;
        MidgardContribution = 0;
    }
    
    public SubObjective GetObjective(int objectiveNumber)
    {
        switch (objectiveNumber)
        {
            case 1:
                return ObjectiveOne;
            case 2:
                return ObjectiveTwo;
            case 3:
                return ObjectiveThree;
            case 4:
                return ObjectiveFour;
        }

        return null;
    }
}