using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Scheduler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace DOL.GS.Scripts
{
    static class ZoneBonusRotator
    {
        // Region ID's
        public const int ALBION_CLASSIC_ID = 1;
        public const int ALBION_SI_ID = 51;
        public const int MIDGARD_CLASSIC_ID = 100;
        public const int MIDGARD_SI_ID = 151;
        public const int HIBERNIA_CLASSIC_ID = 200;
        public const int HIBERNIA_SI_ID = 181;

        // RvR Zone ID's
        private static List<int> albionRvRZones = new List<int>() { 11, 12, 14, 15};
        private static List<int> midgardRvRZones = new List<int>() { 111, 112, 113, 115 };
        private static List<int> hiberniaRvRZones = new List<int>() { 210, 211, 212, 214 };

        // PvE Zone ID's
        private static List<int> albionClassicZones = new List<int>();
        private static List<int> albionSIZones = new List<int>();
        private static List<int> midgardClassicZones = new List<int>();
        private static List<int> midgardSIZones = new List<int>();
        private static List<int> hiberniaClassicZones = new List<int>();
        private static List<int> hiberniaSIZones = new List<int>();

        // Current OF Realm with Bonuses
        private static int currentRvRRealm = 0;

        // Current PvE Zones with Bonuses
        private static int currentAlbionZone;
        private static int currentAlbionZoneSI;
        private static int currentMidgardZone;
        private static int currentMidgardZoneSI;
        private static int currentHiberniaZone;
        private static int currentHiberniaZoneSI;

        private static Zones albDBZone;
        private static Zones albDBZoneSI;
        private static Zones midDBZone;
        private static Zones midDBZoneSI;
        private static Zones hibDBZone;
        private static Zones hibDBZoneSI;

        private static SimpleScheduler scheduler = new SimpleScheduler();

        public static int PvETimer { get; set; }
        public static int RvRTimer { get; set; } 
        public static int PvEExperienceBonusAmount { get; set; }
        public static int RvRExperienceBonusAmount { get; set; } 
        public static int RPBonusAmount { get; set; } 
        public static int BPBonusAmount { get; set; }

        public static long _lastRvRChangeTick { get; set; }
        public static long _lastPvEChangeTick { get; set; }

        private static int RvRTickTime = 2700;
        private static int PvETickTime = 7200;

        [GameServerStartedEvent]
        public static void OnServerStart(DOLEvent e, object sender, EventArgs arguments)
        {
            Initialize();
            GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEntered));
            
        }

        [GameServerStoppedEvent]
        public static void OnServerStopped(DOLEvent e, object sender, EventArgs arguments)
        {
            // Should be changed to keep data saved in DB for restart
            ClearPvEZones();
            ClearRvRZones();

            GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEntered));
        }

        public static void Initialize()
        {
            PvETimer = PvETickTime * 1000;
            RvRTimer = RvRTickTime * 1000;
            PvEExperienceBonusAmount = 50;
            RvRExperienceBonusAmount = 100;
            RPBonusAmount = 25;
            BPBonusAmount = 25;

            GetZones();
            UpdatePvEZones();
            UpdateRvRZones();
        }

        /// <summary>
        /// Gets all ZoneID's for PvE Zones
        /// </summary>
        public static void GetZones()
        {
            // Get Albion ZoneID's
            foreach (Zones zone in DOLDB<Zones>.SelectObjects(DB.Column("RegionID").IsEqualTo(ALBION_CLASSIC_ID)))
            {
                if (!albionRvRZones.Contains(zone.ZoneID))
                {
                    albionClassicZones.Add(zone.ZoneID);
                }
            }
            foreach (Zones zone in DOLDB<Zones>.SelectObjects(DB.Column("RegionID").IsEqualTo(ALBION_SI_ID)))
            {
                albionSIZones.Add(zone.ZoneID);
            }

            // Get Midgard ZoneID's
            foreach (Zones zone in DOLDB<Zones>.SelectObjects(DB.Column("RegionID").IsEqualTo(MIDGARD_CLASSIC_ID)))
            {
                if (!midgardRvRZones.Contains(zone.ZoneID))
                {
                    midgardClassicZones.Add(zone.ZoneID);
                }
            }
            foreach (Zones zone in DOLDB<Zones>.SelectObjects(DB.Column("RegionID").IsEqualTo(MIDGARD_SI_ID)))
            {
                midgardSIZones.Add(zone.ZoneID);
            }

            // Get Hibernia ZoneID's
            foreach (Zones zone in DOLDB<Zones>.SelectObjects(DB.Column("RegionID").IsEqualTo(HIBERNIA_CLASSIC_ID)))
            {
                if (!hiberniaRvRZones.Contains(zone.ZoneID))
                {
                    hiberniaClassicZones.Add(zone.ZoneID);
                }
            }
            foreach (Zones zone in DOLDB<Zones>.SelectObjects(DB.Column("RegionID").IsEqualTo(HIBERNIA_SI_ID)))
            {
                hiberniaSIZones.Add(zone.ZoneID);
            }
        }
        public static void PlayerEntered(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer player = sender as GamePlayer;
            TellClient(player.Client);
        }

        private static int UpdatePvEZones()
        {
            _lastPvEChangeTick = GameLoop.GameLoopTime;

            ClearPvEZones();

            GetNextPvEZones();

            albDBZone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(albionClassicZones[currentAlbionZone]));
            albDBZoneSI = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(albionSIZones[currentAlbionZoneSI]));
            midDBZone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(midgardClassicZones[currentMidgardZone]));
            midDBZoneSI = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(midgardSIZones[currentMidgardZoneSI]));
            hibDBZone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(hiberniaClassicZones[currentHiberniaZone]));
            hibDBZoneSI = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(hiberniaSIZones[currentHiberniaZoneSI]));

            // Set XP Bonuses in DB
            albDBZone.Experience = PvEExperienceBonusAmount;
            albDBZoneSI.Experience = PvEExperienceBonusAmount;
            midDBZone.Experience = PvEExperienceBonusAmount;
            midDBZoneSI.Experience = PvEExperienceBonusAmount;
            hibDBZone.Experience = PvEExperienceBonusAmount;
            hibDBZoneSI.Experience = PvEExperienceBonusAmount;

            // Save XP Bonuses in DB
            GameServer.Database.SaveObject(albDBZone);
            GameServer.Database.SaveObject(albDBZoneSI);
            GameServer.Database.SaveObject(midDBZone);
            GameServer.Database.SaveObject(midDBZoneSI);
            GameServer.Database.SaveObject(hibDBZone);
            GameServer.Database.SaveObject(hibDBZoneSI);

            // Update Bonuses In-Game
            WorldMgr.Zones[(ushort)albionClassicZones[currentAlbionZone]].BonusExperience = PvEExperienceBonusAmount;
            WorldMgr.Zones[(ushort)albionSIZones[currentAlbionZoneSI]].BonusExperience = PvEExperienceBonusAmount;
            WorldMgr.Zones[(ushort)midgardClassicZones[currentMidgardZone]].BonusExperience = PvEExperienceBonusAmount;
            WorldMgr.Zones[(ushort)midgardSIZones[currentMidgardZoneSI]].BonusExperience = PvEExperienceBonusAmount;
            WorldMgr.Zones[(ushort)hiberniaClassicZones[currentHiberniaZone]].BonusExperience = PvEExperienceBonusAmount;
            WorldMgr.Zones[(ushort)hiberniaSIZones[currentHiberniaZoneSI]].BonusExperience = PvEExperienceBonusAmount;

            foreach (GameClient client in WorldMgr.GetAllClients())
            {
                TellClient(client);
            }

            scheduler.Start(UpdatePvEZones, PvETimer);

            return 0;
        }

        private static int UpdateRvRZones()
        {

            _lastRvRChangeTick = GameLoop.GameLoopTime;

            ClearRvRZones();

            // Get new RvR Realm and set Bonuses for each Zone
            GetNextRvRZone();

            switch (currentRvRRealm)
            {
                case 1:
                    foreach (int i in albionRvRZones)
                    {
                        Zones zone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(i));
                        zone.Experience = RvRExperienceBonusAmount;
                        zone.Realmpoints = RPBonusAmount;
                        zone.Bountypoints = BPBonusAmount;
                        GameServer.Database.SaveObject(zone);

                        WorldMgr.Zones[(ushort)i].BonusExperience = RvRExperienceBonusAmount;
                        WorldMgr.Zones[(ushort)i].BonusRealmpoints = RPBonusAmount;
                        WorldMgr.Zones[(ushort)i].BonusBountypoints = BPBonusAmount;
                    }
                    break;
                case 2:
                    foreach (int i in midgardRvRZones)
                    {
                        Zones zone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(i));
                        zone.Experience = RvRExperienceBonusAmount;
                        zone.Realmpoints = RPBonusAmount;
                        zone.Bountypoints = BPBonusAmount;
                        GameServer.Database.SaveObject(zone);

                        WorldMgr.Zones[(ushort)i].BonusExperience = RvRExperienceBonusAmount;
                        WorldMgr.Zones[(ushort)i].BonusRealmpoints = RPBonusAmount;
                        WorldMgr.Zones[(ushort)i].BonusBountypoints = BPBonusAmount;
                    }
                    break;
                case 3:
                    foreach (int i in hiberniaRvRZones)
                    {
                        Zones zone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(i));
                        zone.Experience = RvRExperienceBonusAmount;
                        zone.Realmpoints = RPBonusAmount;
                        zone.Bountypoints = BPBonusAmount;
                        GameServer.Database.SaveObject(zone);

                        WorldMgr.Zones[(ushort)i].BonusExperience = RvRExperienceBonusAmount;
                        WorldMgr.Zones[(ushort)i].BonusRealmpoints = RPBonusAmount;
                        WorldMgr.Zones[(ushort)i].BonusBountypoints = BPBonusAmount;
                    }
                    break;
            }

            foreach (GameClient client in WorldMgr.GetAllClients())
            {
                TellClient(client);
            }

            scheduler.Start(UpdateRvRZones, RvRTimer);

            

            return 0;
        }

        /// <summary>
        /// Rotate through the realms for Bonuses
        /// 1) Albion
        /// 2) Midgard
        /// 3) Hibernia
        /// </summary>
        private static void GetNextRvRZone()
        {
            /*
            if (currentRvRRealm < 3)
                currentRvRRealm += 1; // currentRvRRealm++;// currentRealm + 1;
            else
                currentRvRRealm = 1;
            */
            //get random int from 1-3 to decide realm
            //if realm is undefined or current realm, reroll to new result
            //set new realm
            int rand = Util.Random(2) + 1;
            while(rand == currentRvRRealm)
            {
                rand = Util.Random(2) + 1;
            }
            currentRvRRealm = rand;
        }

        private static void GetNextPvEZones()
        {
            /*
            if (currentAlbionZone < albionClassicZones.Count - 1)
                currentAlbionZone += 1;
            else
                currentAlbionZone = 0;

            if (currentAlbionZoneSI < albionSIZones.Count - 1)
                currentAlbionZoneSI += 1;
            else
                currentAlbionZoneSI = 0;

            if (currentMidgardZone < midgardClassicZones.Count - 1)
                currentMidgardZone += 1;
            else
                currentMidgardZone = 0;

            if (currentMidgardZoneSI < midgardSIZones.Count - 1)
                currentMidgardZoneSI += 1;
            else
                currentMidgardZoneSI = 0;

            if (currentHiberniaZone < hiberniaClassicZones.Count - 1)
                currentHiberniaZone += 1;
            else
                currentHiberniaZone = 0;

            if (currentHiberniaZoneSI < hiberniaSIZones.Count - 1)
                currentHiberniaZoneSI += 1;
            else
                currentHiberniaZoneSI = 0;
            */
            currentAlbionZone = Util.Random(albionClassicZones.Count - 1);
            currentAlbionZoneSI = Util.Random(albionSIZones.Count - 1);

            currentMidgardZone = Util.Random(midgardClassicZones.Count - 1);
            currentMidgardZoneSI = Util.Random(midgardSIZones.Count - 1);

            currentHiberniaZone = Util.Random(hiberniaClassicZones.Count - 1);
            currentHiberniaZoneSI = Util.Random(hiberniaSIZones.Count - 1);

        }


        private static void TellClient(GameClient client)
        {
            client.Out.SendMessage(GetText(), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public static string GetText()
        {
            string realm = "";
            switch (currentRvRRealm)
            {
                case 1:
                    realm = "Albion";
                    break;
                case 2:
                    realm = "Midgard";
                    break;
                case 3:
                    realm = "Hibernia";
                    break;
            }
            return "\nOF Bonus Region: " + realm + "\n\n" +
                "Albion Classic: " + albDBZone.Name + " (XP +" + albDBZone.Experience + "%)\n" +
                "Albion SI: " + albDBZoneSI.Name + " (XP +" + albDBZoneSI.Experience + "%)\n\n" +
                "Midgard Classic: " + midDBZone.Name + " (XP +" + midDBZone.Experience + "%)\n" +
                "MIdgard SI: " + midDBZoneSI.Name + " (XP +" + midDBZoneSI.Experience + "%)\n\n" +
                "Hibernia Classic: " + hibDBZone.Name + " (XP +" + hibDBZone.Experience + "%)\n" +
                "Hibernia SI: " + hibDBZoneSI.Name + " (XP +" + hibDBZoneSI.Experience + "%)\n\n";
        }

        public static List<string> GetTextList()
        {
            List<string> temp = new List<string>();
            string realm = "";
            switch (currentRvRRealm)
            {
                case 1:
                    realm = "Albion";
                    break;
                case 2:
                    realm = "Midgard";
                    break;
                case 3:
                    realm = "Hibernia";
                    break;
            }
            temp.Add("Current OF Bonus Region: " + realm);
            temp.Add("Bonus RP: " + RPBonusAmount + "%");
            temp.Add("Bonus BP: " + BPBonusAmount + "%");
            temp.Add("Bonus XP: " + RvRExperienceBonusAmount + "%");
            temp.Add("");
            temp.Add("Current Albion Zones: ");
            temp.Add("Classic Zone: " + albDBZone.Name + " (XP +" + albDBZone.Experience + "%)");
            temp.Add("SI Zone: " + albDBZoneSI.Name + " (XP +" + albDBZoneSI.Experience + "%)");
            temp.Add("");
            temp.Add("Current Midgard Zones: ");
            temp.Add("Classic Zone: " + midDBZone.Name + " (XP +" + midDBZone.Experience + "%)");
            temp.Add("SI Zone: " + midDBZoneSI.Name + " (XP +" + midDBZoneSI.Experience + "%)");
            temp.Add("");
            temp.Add("Current Hibernia Zones: ");
            temp.Add("Classic Zone: " + hibDBZone.Name + " (XP +" + hibDBZone.Experience + "%)");
            temp.Add("SI Zone: " + hibDBZoneSI.Name + " (XP +" + hibDBZoneSI.Experience + "%)");

            temp.Add("");
            var rvr = _lastRvRChangeTick + RvRTimer - GameLoop.GameLoopTime;
            temp.Add("RvR Time Remaining: " + TimeSpan.FromMilliseconds(rvr).Hours + "h " + TimeSpan.FromMilliseconds(rvr).Minutes + "m " + TimeSpan.FromMilliseconds(rvr).Seconds + "s");
            
            var pve = _lastPvEChangeTick + PvETimer - GameLoop.GameLoopTime;
            temp.Add("PvE Time Remaining: " + TimeSpan.FromMilliseconds(pve).Hours + "h " + TimeSpan.FromMilliseconds(pve).Minutes + "m " + TimeSpan.FromMilliseconds(pve).Seconds + "s");

            temp.Add("");
            temp.Add("");

            temp.Add("Permanent Bonuses:");
            temp.Add("All Dungeons: 25%");
            temp.Add("RvR Dungeons: 50%");
            temp.Add("Darkness Falls: 75%");
            
            temp.Add("");
            temp.Add("");

            ConquestObjective hibObj = ConquestService.ConquestManager.ActiveHiberniaObjective;
            ConquestObjective albObj = ConquestService.ConquestManager.ActiveAlbionObjective;
            ConquestObjective midObj = ConquestService.ConquestManager.ActiveMidgardObjective;
            ArrayList hibList = new ArrayList();
            ArrayList midList = new ArrayList();
            ArrayList albList = new ArrayList();
            hibList = hibObj.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER, hibObj.Keep.X,
                hibObj.Keep.Y, hibObj.Keep.Z, 15000, hibList, true);
            albList = albObj.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER, albObj.Keep.X,
                albObj.Keep.Y, albObj.Keep.Z, 15000, albList, true);
            midList =  midObj.Keep.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.PLAYER, midObj.Keep.X,
                midObj.Keep.Y, midObj.Keep.Z, 15000, midList, true);

            long timeSinceTaskStart = GameLoop.GameLoopTime - ConquestService.ConquestManager.LastTaskRolloverTick;
            temp.Add("Conquest Max Time Limit: " + ServerProperties.Properties.MAX_CONQUEST_TASK_DURATION + "m");
            temp.Add("Time since beginning of this conquest: " + TimeSpan.FromMilliseconds(timeSinceTaskStart).Minutes + "m " + TimeSpan.FromMilliseconds(timeSinceTaskStart).Seconds + "s");
            temp.Add("(H) Conquest Target: " + hibObj.Keep.Name + " | Players Nearby: " + hibList.Count);
            temp.Add("(M) Conquest Target: " + midObj.Keep.Name + " | Players Nearby: " + midList.Count);
            temp.Add("(A) Conquest Target: " + albObj.Keep.Name + " | Players Nearby: " + albList.Count);
            
                     

            return temp;
        }

        private static void ClearRvRZones()
        {
            // Clear RvR Zone
            switch (currentRvRRealm)
            {
                case 1:
                    foreach (int i in albionRvRZones)
                    {
                        Zones zone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(i));
                        zone.Experience = 0;
                        zone.Realmpoints = 0;
                        zone.Bountypoints = 0;
                        GameServer.Database.SaveObject(zone);

                        WorldMgr.Zones[(ushort)i].BonusExperience = 0;
                        WorldMgr.Zones[(ushort)i].BonusRealmpoints = 0;
                        WorldMgr.Zones[(ushort)i].BonusBountypoints = 0;
                    }
                    break;
                case 2:
                    foreach (int i in midgardRvRZones)
                    {
                        Zones zone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(i));
                        zone.Experience = 0;
                        zone.Realmpoints = 0;
                        zone.Bountypoints = 0;
                        GameServer.Database.SaveObject(zone);

                        WorldMgr.Zones[(ushort)i].BonusExperience = 0;
                        WorldMgr.Zones[(ushort)i].BonusRealmpoints = 0;
                        WorldMgr.Zones[(ushort)i].BonusBountypoints = 0;
                    }
                    break;
                case 3:
                    foreach (int i in hiberniaRvRZones)
                    {
                        Zones zone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(i));
                        zone.Experience = 0;
                        zone.Realmpoints = 0;
                        zone.Bountypoints = 0;
                        GameServer.Database.SaveObject(zone);

                        WorldMgr.Zones[(ushort)i].BonusExperience = 0;
                        WorldMgr.Zones[(ushort)i].BonusRealmpoints = 0;
                        WorldMgr.Zones[(ushort)i].BonusBountypoints = 0;
                    }
                    break;
            }
        }

        private static void ClearPvEZones()
        {
            // Clear PvE Zones
            albDBZone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(albionClassicZones[currentAlbionZone]));
            albDBZoneSI = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(albionSIZones[currentAlbionZoneSI]));
            midDBZone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(midgardClassicZones[currentMidgardZone]));
            midDBZoneSI = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(midgardSIZones[currentMidgardZoneSI]));
            hibDBZone = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(hiberniaClassicZones[currentHiberniaZone]));
            hibDBZoneSI = DOLDB<Zones>.SelectObject(DB.Column("ZoneID").IsEqualTo(hiberniaSIZones[currentHiberniaZoneSI]));

            albDBZone.Experience = 0;
            albDBZoneSI.Experience = 0;
            midDBZone.Experience = 0;
            midDBZoneSI.Experience = 0;
            hibDBZone.Experience = 0;
            hibDBZoneSI.Experience = 0;

            GameServer.Database.SaveObject(albDBZone);
            GameServer.Database.SaveObject(albDBZoneSI);
            GameServer.Database.SaveObject(midDBZone);
            GameServer.Database.SaveObject(midDBZoneSI);
            GameServer.Database.SaveObject(hibDBZone);
            GameServer.Database.SaveObject(midDBZoneSI);

            
            foreach (var zone in albionClassicZones)
                WorldMgr.Zones[(ushort)zone].BonusExperience = 0;

            foreach (var zone in albionSIZones)
                WorldMgr.Zones[(ushort)zone].BonusExperience = 0;

            foreach (var zone in midgardClassicZones)
                WorldMgr.Zones[(ushort)zone].BonusExperience = 0;

            foreach (var zone in midgardSIZones)
                WorldMgr.Zones[(ushort)zone].BonusExperience = 0;

            foreach (var zone in hiberniaClassicZones)
                WorldMgr.Zones[(ushort)zone].BonusExperience = 0;

            foreach (var zone in hiberniaSIZones)
                WorldMgr.Zones[(ushort)zone].BonusExperience = 0;

            /*
            WorldMgr.Zones[(ushort)albionClassicZones[currentAlbionZone]].BonusExperience = 0;
            WorldMgr.Zones[(ushort)albionSIZones[currentAlbionZoneSI]].BonusExperience = 0;
            WorldMgr.Zones[(ushort)midgardClassicZones[currentMidgardZone]].BonusExperience = 0;
            WorldMgr.Zones[(ushort)midgardSIZones[currentMidgardZoneSI]].BonusExperience = 0;
            WorldMgr.Zones[(ushort)hiberniaClassicZones[currentHiberniaZone]].BonusExperience = 0;
            WorldMgr.Zones[(ushort)hiberniaSIZones[currentHiberniaZoneSI]].BonusExperience = 0;
            */
        }
    }
}
