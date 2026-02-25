using System;
using System.Reflection;
using System.Collections.Generic;
using DOL.GS;
using DOL.Logging;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class TempleRelicPadsLoader
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static volatile bool _initialized = false;

        /// <summary>
        /// Init NF Tempel-Pads. 
        /// </summary>
        public static bool LoadTemplePads()
        {
            if (_initialized) return true;
            _initialized = true;

            log.Info("Loading New Frontiers Temple Relic Pads...");

            foreach (TemplePadInfo info in TemplePadsList)
            {
                if (WorldMgr.GetRegion(info.RegionID) == null)
                {
                    log.Error($"[Relic] Region {info.RegionID} für '{info.TempleName}' nicht geladen!");
                    continue;
                }

                try
                {
                    GameTempleRelicPad pad = new GameTempleRelicPad();
                    pad.Emblem = info.EmblemID;
                    pad.Name = info.TempleName;
                    pad.X = info.X;
                    pad.Y = info.Y;
                    pad.Z = info.Z;
                    pad.CurrentRegionID = info.RegionID;
                    pad.Heading = info.Heading;

                    if (!pad.AddToWorld())
                    {
                        log.Error($"[Relic] AddToWorld fehlgeschlagen für: {info.TempleName}");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"[Relic] Fehler beim Erstellen von {info.TempleName}:", ex);
                }
            }
            return true;
        }

        public static void SendTempleMessage(string message)
        {
            // Wir nutzen den ClientService, um alle Spieler zu erreichen
            foreach (GamePlayer player in ClientService.Instance.GetPlayers())
            {
                if (player != null && player.CurrentRegionID == 163)
                {
                    player.Out.SendMessage(message, eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }
            }
        }

        public static int GetEnemiesNearby(GameObject lord)
        {
            ICollection<GamePlayer> players = lord.GetPlayersInRadius(2000);
            int enemyNearby = 0;

            foreach (GamePlayer player in players)
            {
                if (player.Realm == lord.Realm)
                    continue;

                if (!player.IsAlive)
                    continue;

                enemyNearby++;
            }

            return enemyNearby;
        }

        private static readonly TemplePadInfo[] TemplePadsList =
        {
            new TemplePadInfo(1, 163, 673846, 589994, 8748, 0, "Castle Excalibur"),   // Alb Strength
            new TemplePadInfo(11, 163, 578176, 676596, 8740, 0, "Castle Myrddin"),    // Alb Magic
            new TemplePadInfo(2, 163, 610911, 302488, 8500, 0, "Mjollner Faste"),     // Mid Strength
            new TemplePadInfo(12, 163, 713091, 403189, 8788, 0, "Grallarhorn Faste"), // Mid Magic
            new TemplePadInfo(3, 163, 372735, 590550, 8740, 0, "Dun Lamfhota"),       // Hib Strength
            new TemplePadInfo(13, 163, 470210, 677203, 8116, 0, "Dun Dagda")          // Hib Magic
        };
    }

    public class TemplePadInfo
    {
        public int EmblemID { get; }
        public ushort RegionID { get; }
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public ushort Heading { get; }
        public string TempleName { get; }

        public TemplePadInfo(int emblem, ushort region, int x, int y, int z, ushort heading, string name)
        {
            EmblemID = emblem;
            RegionID = region;
            X = x;
            Y = y;
            Z = z;
            Heading = heading;
            TempleName = name;
        }
    }
}