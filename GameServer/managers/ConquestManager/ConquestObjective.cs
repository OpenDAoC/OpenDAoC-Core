using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.GS.Keeps;

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

    private Dictionary<GamePlayer, int> PlayerToContributionDict;

    public SubObjective ObjectiveOne;
    public SubObjective ObjectiveTwo;
    public SubObjective ObjectiveThree;
    public SubObjective ObjectiveFour;

    public bool ActiveFlags => ObjectiveOne != null && ObjectiveTwo != null && ObjectiveThree != null && ObjectiveFour != null;

    public ConquestObjective(AbstractGameKeep keep)
    {
        Keep = keep;
        PlayerToContributionDict = new Dictionary<GamePlayer, int>();
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
                flagLocs.Add(new Point3D(632183, 585097, 5400)); //near hmg
                flagLocs.Add(new Point3D(629067, 631012, 5355)); //near amg
                flagLocs.Add(new Point3D(648889, 596948, 5511)); //near the keep
                flagLocs.Add(new Point3D(636055, 601363, 5448)); //crossroads
                break;
            case "hlidskialf faste":
            case "nottmoor faste":
            case "blendrake faste":
            case "glenlock faste":
                flagLocs.Add(new Point3D(674589, 659938, 4096)); //near hlidskialf
                flagLocs.Add(new Point3D(665436, 629879, 4230)); //west of nottmoor
                flagLocs.Add(new Point3D(701633, 622093, 3328)); //N of blendrake
                flagLocs.Add(new Point3D(697332, 655377, 3992)); //between lakes near glenlock
                break;
            case "fensalir faste":
                flagLocs.Add(new Point3D(736092, 653463, 5692)); //near fensalir
                flagLocs.Add(new Point3D(729352, 637453, 5966)); //near jamtland
                flagLocs.Add(new Point3D(758533, 657601, 6663)); //between svasud and fensalir
                flagLocs.Add(new Point3D(771678, 636304, 5917)); //near mjolnir
                break;
            case "arvakr faste":
                flagLocs.Add(new Point3D(694026, 686674, 5704)); //near arvakr
                flagLocs.Add(new Point3D(704865, 727127, 5704)); //near vindsaul
                flagLocs.Add(new Point3D(680079, 716974, 5704)); //near grallarhorn lake
                flagLocs.Add(new Point3D(669995, 682832, 5730)); //NW basin
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
            /*
            //mid
            case "bledmeer faste":
                flagLocs.Add(); //near hmg
                flagLocs.Add(); //near amg
                flagLocs.Add(); //near the keep
                flagLocs.Add(); //crossroads
                break;
            case "hlidskialf faste":
            case "nottmoor faste":
            case "blendrake faste":
            case "glenlock faste":
                flagLocs.Add(); //near hlidskialf
                flagLocs.Add(); //west of nottmoor
                flagLocs.Add( ); //N of blendrake
                flagLocs.Add(); //between lakes near glenlock
                break;
            case "fensalir faste":
                flagLocs.Add(); //near fensalir
                flagLocs.Add(); //near jamtland
                flagLocs.Add(); //between svasud and fensalir
                flagLocs.Add(); //near mjolnir
                break;
            case "arvakr faste":
                flagLocs.Add(); //near arvakr
                flagLocs.Add(); //near vindsaul
                flagLocs.Add(); //near grallarhorn lake
                flagLocs.Add(); //NW basin
                break;
            //hib
            case "dun crauchon":
                flagLocs.Add(); //near amg
                flagLocs.Add(); //near mmg
                flagLocs.Add(); //near the keep
                flagLocs.Add(); //near briefine
                break;
            case "dun bolg":
            case "dun nged":
            case "dun da behnn":
            case "dun crimthain":
                flagLocs.Add(); //bolg crossroads
                flagLocs.Add(); //near Evern
                flagLocs.Add(); //between Behn and nGed
                flagLocs.Add(); //western hill
                break;
            */
            case "dun ailinne":
                flagLocs.Add("X: 48957  Y: 48906"); //basin near ailinne
                flagLocs.Add("X: 22397  Y: 55729"); //hill near Ligen
                flagLocs.Add("X: 17549 Y: 20683"); //west of lamfhota
                flagLocs.Add("X: 47466 Y: 12502"); //NE of Lamfhota
                break;
            /*
            case "dun scathaig":
                flagLocs.Add(); //front of Scathaig
                flagLocs.Add(); //SE corner
                flagLocs.Add(); //between scath and dagda
                flagLocs.Add(); //NE of scath
                break;
            
            //albion
            case "caer benowyc":
                flagLocs.Add("X: 10108 Y:33927"); //near hmg
                flagLocs.Add("X: 55408 Y:36250"); //near mmg
                flagLocs.Add("X: 47227 Y:57780"); //near the keep
                flagLocs.Add("X: 26591 Y:58548"); //southern crossroads
                break;
            case "caer sursbrooke":
            case "caer erasleigh":
            case "caer berkstead":
            case "caer boldiam":
                flagLocs.Add(); //near boldiam
                flagLocs.Add(); //near forest sauvage
                flagLocs.Add(); //near caer surs
                flagLocs.Add(); //near hadrian's
                break;
            case "caer renaris":
                flagLocs.Add(); //SE forest
                flagLocs.Add(); //NE river-side
                flagLocs.Add(); //hill near Renaris
                flagLocs.Add(); //SW near lake
                break;
            case "caer hurbury":
                flagLocs.Add(); //pennine border
                flagLocs.Add(); //near hurbury
                flagLocs.Add(); //near snowdonia keep
                flagLocs.Add(); //myrddin approach
                break;
                */
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
        //TODO: Redo the award algo
        /*
        ConquestManager conqMan = ConquestService.ConquestManager;
        foreach (var player in PlayerToContributionDict.Keys.Where(x => PlayerToContributionDict[x] > 0))
        {
            switch (player.Realm)
            {
                case eRealm.Albion:
                    int albaward = (int)Math.Round(AlbionContribution * (PlayerToContributionDict[player] / (double) AlbionContribution));
                    if (albaward > SubTickMaxReward) albaward = SubTickMaxReward;
                    if (conqMan.AlbStreak > 0) albaward *= conqMan.AlbStreak;
                    player.GainRealmPoints(albaward, false, true);
                    break;
                case eRealm.Hibernia:
                    int hibaward = (int)Math.Round(HiberniaContribution * (PlayerToContributionDict[player] / (double) HiberniaContribution));
                    if (hibaward > SubTickMaxReward) hibaward = SubTickMaxReward;
                    if (conqMan.HibStreak > 0) hibaward *= conqMan.HibStreak;
                    player.GainRealmPoints(hibaward, false, true);
                    break;
                case eRealm.Midgard:
                    int midaward = (int)Math.Round(MidgardContribution * (PlayerToContributionDict[player] / (double) MidgardContribution));
                    if (midaward > SubTickMaxReward) midaward = SubTickMaxReward;
                    if (conqMan.MidStreak > 0) midaward *= conqMan.MidStreak;
                    player.GainRealmPoints(midaward, false, true);
                    break;
            }
        }*/
    }

    public void CheckNearbyPlayers()
    {
        ObjectiveOne?.CheckNearbyPlayers();
        ObjectiveTwo?.CheckNearbyPlayers();
        ObjectiveThree?.CheckNearbyPlayers();
        ObjectiveFour?.CheckNearbyPlayers();
    }

    public List<GamePlayer> GetContributingPlayers()
    {
        return PlayerToContributionDict.Keys.ToList();
    }

    public int GetPlayerContributionValue(GamePlayer player)
    {
        if (!PlayerToContributionDict.ContainsKey(player))
            return 0;
        
        return PlayerToContributionDict[player];
    }

    private void ResetContribution()
    {
        PlayerToContributionDict.Clear();
        AlbionContribution = 0;
        HiberniaContribution = 0;
        MidgardContribution = 0;
    }
}