using DOL.Events;
using DOL.Language;
using System;
using System.Reflection;
using DOL.Logging;

namespace DOL.GS
{
    // NEU: Diese Hilfsklasse muss außerhalb der statischen Klasse definiert werden.
    // "internal" ist der Standard und erlaubt den Zugriff innerhalb des GameServer-Projekts.
    internal class TemplePadInfo // oder einfach: class TemplePadInfo
    {
        public int EmblemID { get; set; }
        public ushort RegionID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public ushort Heading { get; set; }
        public string TempleName { get; set; }
    }

    /// <summary>
    /// Lädt die sechs New Frontiers Temple Relic Pads (ohne sichtbares Modell).
    /// </summary>
    public static class TempleRelicPadsLoader
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // HINWEIS: Ersetzen Sie die Platzhalter-Werte (RegionID, X, Y, Z) durch die ECHTEN NF-Koordinaten
        private static readonly TemplePadInfo[] TemplePads = new TemplePadInfo[]
        {
            // ALBION
            new TemplePadInfo { 
                EmblemID = 1, TempleName = "Castle Excalibur",
                RegionID = 163, X = 673846, Y = 589994, Z = 8748, Heading = 0 
            },
            new TemplePadInfo { 
                EmblemID = 11, TempleName = "Castle Myrddin",
                RegionID = 163, X = 578176, Y = 676596, Z = 8740, Heading = 0 
            },
            
            // MIDGARD
            new TemplePadInfo { 
                EmblemID = 2, TempleName = "Mjollner Faste",
                RegionID = 163, X = 610911, Y = 302488, Z = 8500, Heading = 0 
            },
            new TemplePadInfo { 
                EmblemID = 12, TempleName = "Grallarhorn Faste",
                RegionID = 163, X = 713091, Y = 403189, Z = 8788, Heading = 0 
            },

            // HIBERNIA
            new TemplePadInfo { 
                EmblemID = 3, TempleName = "Dun Lamfhota",
                RegionID = 163, X = 372735, Y = 590532, Z = 8740, Heading = 0 
            },
            new TemplePadInfo { 
                EmblemID = 13, TempleName = "Dun Dagda",
                RegionID = 163, X = 470210, Y = 677203, Z = 8116, Heading = 0 
            },
        };

        [ScriptLoadedEvent]
        private static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            LoadTemplePads();
        }

        public static void LoadTemplePads()
        {
            log.Info("Loading New Frontiers Temple Relic Pads...");

            foreach (var info in TemplePads)
            {
                if (WorldMgr.GetRegion(info.RegionID) == null)
                {
                    log.Warn($"Region {info.RegionID} for {info.TempleName} not found. Skipping Pad load.");
                    continue;
                }

                GameTempleRelicPad pad = new GameTempleRelicPad
                {
                    Emblem = info.EmblemID, 
                    Name = info.TempleName,
                    X = info.X,
                    Y = info.Y,
                    Z = info.Z,
                    CurrentRegionID = info.RegionID,
                    Heading = info.Heading,
                };
                
                // AddToWorld ruft RelicMgr.AddRelicPad(this) auf
                if (pad.AddToWorld())
                {
                    log.Debug($"Successfully loaded {pad.Name} (Realm: {GlobalConstants.RealmToName(pad.Realm)}, Type: {pad.PadType})");
                }
            }
            log.Info("Finished loading Temple Relic Pads.");
        }
    }
}