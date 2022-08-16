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
                flagLocs.Add(new Point3D(447813, 341818, 3457)); //near amg
                flagLocs.Add(new Point3D(447201, 297120, 4254)); //near mmg
                flagLocs.Add(new Point3D(434208, 317595, 3033)); //near the keep
                flagLocs.Add(new Point3D(432212, 345540, 2841)); //near briefine
                break;
            case "dun bolg":
            case "dun nged":
            case "dun da behnn":
            case "dun crimthain":
                flagLocs.Add(new Point3D(415444, 368974, 1686)); //bolg crossroads
                flagLocs.Add(new Point3D(428997, 377892, 2170)); //near Evern
                flagLocs.Add(new Point3D(420162, 399425, 4467)); //between Behn and nGed
                flagLocs.Add(new Point3D(394135, 386115, 4574)); //western hill
                break;
            case "dun ailinne":
                flagLocs.Add(new Point3D(368445, 401162, 3976)); //basin near ailinne
                flagLocs.Add(new Point3D(341886, 407986, 4124)); //hill near Ligen
                flagLocs.Add(new Point3D(337035, 372939, 3131)); //west of lamfhota
                flagLocs.Add(new Point3D(366955, 364754, 3501)); //NE of Lamfhota
                break;
            case "dun scathaig":
                flagLocs.Add(new Point3D(415093, 435971, 3608)); //front of Scathaig
                flagLocs.Add(new Point3D(438871, 463389, 2964)); //SE corner
                flagLocs.Add(new Point3D(404350, 452628, 2036)); //between scath and dagda
                flagLocs.Add(new Point3D(430946, 432740, 2024)); //NE of scath
                break;
            
            //albion
            case "caer benowyc":
                flagLocs.Add(new Point3D(608127, 320648, 3864)); //near hmg
                flagLocs.Add(new Point3D(653422, 322966, 4335)); //near mmg
                flagLocs.Add(new Point3D(645242, 344503, 4325)); //near the keep
                flagLocs.Add(new Point3D(624610, 345270, 4290)); //southern crossroads
                break;
            case "caer sursbrooke":
            case "caer erasleigh":
            case "caer berkstead":
            case "caer boldiam":
                flagLocs.Add(new Point3D(613343, 403062, 6376)); //near boldiam
                flagLocs.Add(new Point3D(586969, 412557, 909)); //near forest sauvage
                flagLocs.Add(new Point3D(580120, 373885, 5800)); //near caer surs
                flagLocs.Add(new Point3D(603369, 364277, 6568)); //near hadrian's
                break;
            case "caer renaris":
                flagLocs.Add(new Point3D(615913, 470492, 2974)); //SE forest
                flagLocs.Add(new Point3D(616462, 439163, 2799)); //NE river-side
                flagLocs.Add(new Point3D(581617, 440066, 4887)); //hill near Renaris
                flagLocs.Add(new Point3D(579472, 460846, 2885)); //SW near lake
                break;
            case "caer hurbury":
                flagLocs.Add(new Point3D(561438, 361288, 2896)); //pennine border
                flagLocs.Add(new Point3D(549344, 344804, 4336)); //near hurbury
                flagLocs.Add(new Point3D(518983, 352767, 4623)); //near snowdonia keep
                flagLocs.Add(new Point3D(513218, 319972, 6928)); //myrddin approach
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
                flagLocs.Add("X:59073 Y:23499 | Near Bledmeer"); 
                flagLocs.Add("X:46228 Y:27925 | Crossroads"); 
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
                flagLocs.Add("X: 6825 Y:51398 | West of Fensalir"); //near fensalir
                flagLocs.Add("X:19192 Y:19158 | Northwestern Hill"); //northwestern hill
                flagLocs.Add("X:35950 Y:30297 | Forest Glen"); //forest glen
                flagLocs.Add("X:38247 Y:47891 | "); //inner valley
                break;
            //hib
            case "dun crauchon":
                flagLocs.Add("X:30020 Y:55108 | Albion Milegate"); //near amg
                flagLocs.Add("X:29409 Y:10408 | Midgard Milegate"); //near mmg
                flagLocs.Add("X:16413 Y:30882 | Near Crauchon"); //near the keep
                flagLocs.Add("X:14415 Y:58827 | Near Briefine"); //near briefine
                break;
            case "dun bolg":
            case "dun nged":
            case "dun da behnn":
            case "dun crimthain":
                flagLocs.Add("X:30416 Y:16716 | Crossroads Near Bolg"); //bolg crossroads
                flagLocs.Add("X:43975 Y:25618 | Near Evern"); //near Evern
                flagLocs.Add("X:35124 Y:47180 | Between Behn and nGed"); //between Behn and nGed
                flagLocs.Add("X: 9106 Y:33850 | Western Hill"); //western hill
                break;
            case "dun ailinne":
                flagLocs.Add("X:48957 Y:48906 | Basin Near Ailinne"); //basin near ailinne
                flagLocs.Add("X:22397 Y:55729 | Hill near Ligen"); //hill near Ligen
                flagLocs.Add("X:17549 Y:20683 | West of Lamfhota"); //west of lamfhota
                flagLocs.Add("X:47466 Y:12502 | Northeast of Lamfhota"); //NE of Lamfhota
                break;
            case "dun scathaig":
                flagLocs.Add("X:30065 Y:18191 | Near Scathaig"); //front of Scathaig
                flagLocs.Add("X:53847 Y:45606 | Southeast Corner"); //SE corner
                flagLocs.Add("X:19318 Y:34835 | Between Scathaig and Dagda"); //between scath and dagda
                flagLocs.Add("X:45924 Y:14953 | Northeast of Scathaig"); //NE of scath
                break;
            //albion
            case "caer benowyc":
                flagLocs.Add("X:10108 Y:33927 | Hibernia Milegate"); //near hmg
                flagLocs.Add("X:55408 Y:36250 | Midgard Milegate"); //near mmg
                flagLocs.Add("X:47227 Y:57780 | Near Benowyc"); //near the keep
                flagLocs.Add("X:26591 Y:58548 | Southern Crossroads"); //southern crossroads
                break;            
            case "caer sursbrooke":
            case "caer erasleigh":
            case "caer berkstead":
            case "caer boldiam":
                flagLocs.Add("X:48094 Y:50807 | Near Boldiam"); //near boldiam
                flagLocs.Add("X:21724 Y:60307 | Sauvage Border"); //near forest sauvage
                flagLocs.Add("X:14868 Y:21627 | Near Sursbrooke"); //near caer surs
                flagLocs.Add("X:38120 Y:12023 | Hadrian's Wall Border"); //near hadrian's
                break;
            case "caer renaris":
                flagLocs.Add("X:50670 Y:52700 | Southeast Forest"); //SE forest
                flagLocs.Add("X:51207 Y:21373 | Northeast Riverside"); //NE river-side
                flagLocs.Add("X:16361 Y:22258 | Near Renaris"); //hill near Renaris
                flagLocs.Add("X:14320 Y:43064 | Southwestern Lake"); //SW near lake
                break;
            case "caer hurbury":
                flagLocs.Add("X:61721 Y:58185 | Pennine Border"); //pennine border
                flagLocs.Add("X:49636 Y:41704 | Near Hurbury"); //near hurbury
                flagLocs.Add("X:19267 Y:49672 | Near Snowdonia Keep"); //near snowdonia keep
                flagLocs.Add("X:13502 Y:16876 | Myrddin Approach"); //myrddin approach
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