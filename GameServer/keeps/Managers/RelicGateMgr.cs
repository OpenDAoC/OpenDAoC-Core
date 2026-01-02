/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * (Lizenzkommentar unverändert)
 *
 */
using System;
using System.Collections;
using DOL.GS; 
using DOL.Database; 
using System.Collections.Generic;
using System.Linq; 
using System.Reflection; 
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Keeps; 

namespace DOL.GS.Keeps
{
    public class RelicGateMgr
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // ====================================================================
        // 1. KONSTANTEN FÜR DIE RELIKTÜR-ZUWEISUNG
        // ====================================================================
        public const int DOOR_ALB_POWER_ID = 175014001; 
        public const int DOOR_ALB_STRENGTH_ID = 176233901;
        public const int DOOR_MID_POWER_ID = 169000501;
        public const int DOOR_MID_STRENGTH_ID = 170128101;
        public const int DOOR_HIB_POWER_ID = 174061701;
        public const int DOOR_HIB_STRENGTH_ID = 173000301;

        public static readonly int[] RELIC_GATE_INTERNAL_IDS = 
        {
            DOOR_ALB_POWER_ID, DOOR_ALB_STRENGTH_ID,
            DOOR_MID_POWER_ID, DOOR_MID_STRENGTH_ID,
            DOOR_HIB_POWER_ID, DOOR_HIB_STRENGTH_ID
        };


        // ====================================================================
        // 2. KEEP-KETTEN DEFINITION 
        // ====================================================================
        
        private static readonly Dictionary<RelicGate, List<int>> ALL_RELIC_CHAINS = new(); // NEU: Dictionary anstelle von Tuple-Liste

        private static readonly List<int> ALB_POWER_CHAIN = new List<int> { 50, 51, 53, 55 }; 
        public static RelicGate Door_Alb_Power = null; // STATISCHES FELD

        private static readonly List<int> ALB_STRENGTH_CHAIN = new List<int> { 50, 52, 54, 56 };
        public static RelicGate Door_Alb_Strength = null; // STATISCHES FELD
        
        private static readonly List<int> MID_POWER_CHAIN = new List<int> { 75, 76, 79, 81 };
        public static RelicGate Door_Mid_Power = null;
        
        private static readonly List<int> MID_STRENGTH_CHAIN = new List<int> { 75, 78, 77, 82 };
        public static RelicGate Door_Mid_Strength = null;
        
        private static readonly List<int> HIB_POWER_CHAIN = new List<int> { 100, 101, 103, 105 };
        public static RelicGate Door_Hib_Power = null;
        
        private static readonly List<int> HIB_STRENGTH_CHAIN = new List<int> { 100, 102, 104, 106 };
        public static RelicGate Door_Hib_Strength = null;

        // AddAllChains WIRD NICHT MEHR BENÖTIGT, da die statischen Felder verwendet werden.
        
        // ====================================================================
        // 3. ZUWEISUNGSMETHODE
        // ====================================================================
        
        public static void AssignRelicDoor(RelicGate door, int internalID)
        {
            if (door == null) return;
            
            // Setzen der statischen Variablen (Diese Referenzen sind die einzigen, die wir brauchen)
            if (internalID == DOOR_ALB_POWER_ID) Door_Alb_Power = door;
            else if (internalID == DOOR_ALB_STRENGTH_ID) Door_Alb_Strength = door;
            else if (internalID == DOOR_MID_POWER_ID) Door_Mid_Power = door;
            else if (internalID == DOOR_MID_STRENGTH_ID) Door_Mid_Strength = door;
            else if (internalID == DOOR_HIB_POWER_ID) Door_Hib_Power = door;
            else if (internalID == DOOR_HIB_STRENGTH_ID) Door_Hib_Strength = door;

            // NEU: Hinzufügen der Tür und Kette zum robusten Dictionary, sobald die Tür instanziiert ist
            if (internalID == DOOR_ALB_POWER_ID) ALL_RELIC_CHAINS[door] = ALB_POWER_CHAIN;
            else if (internalID == DOOR_ALB_STRENGTH_ID) ALL_RELIC_CHAINS[door] = ALB_STRENGTH_CHAIN;
            else if (internalID == DOOR_MID_POWER_ID) ALL_RELIC_CHAINS[door] = MID_POWER_CHAIN;
            else if (internalID == DOOR_MID_STRENGTH_ID) ALL_RELIC_CHAINS[door] = MID_STRENGTH_CHAIN;
            else if (internalID == DOOR_HIB_POWER_ID) ALL_RELIC_CHAINS[door] = HIB_POWER_CHAIN;
            else if (internalID == DOOR_HIB_STRENGTH_ID) ALL_RELIC_CHAINS[door] = HIB_STRENGTH_CHAIN;
            
            if (log.IsDebugEnabled)
                log.Debug($"RelicGateMgr: Assigned door with InternalID {internalID} to static field and chain dictionary.");
        }
        
        // ... (GetRelicGateType unverändert) ...
        
        public static Type GetRelicGateType(int internalID)
        {
            if (RELIC_GATE_INTERNAL_IDS.Contains(internalID))
            {
                return typeof(RelicGate);
            }
            return null; 
        }

        // ====================================================================
        // 5. PRÜFUNG UND INITIALISIERUNG
        // ====================================================================
        
        public static void OnServerStart()
        {
            // AddAllChains() ist jetzt unnötig, da die Zuweisung in AssignRelicDoor erfolgt.
 
            GameEventMgr.AddHandler(null, KeepEvent.KeepTaken, new DOLEventHandler(OnKeepChange));
            
            log.Info("RelicGateMgr: Starting initial keep check...");
            CheckKeeps();
        }
        
        public static void CheckKeeps()
        {
            // FIX: Debug-Meldung, um zu sehen, ob die Methode arbeitet
            log.Info("DEBUG: CheckKeeps is running..."); 
            
            // NEU: Wir iterieren über das Dictionary, das erst in AssignRelicDoor gefüllt wird
            foreach (var chainEntry in ALL_RELIC_CHAINS)
            {
                RelicGate door = chainEntry.Key;
                List<int> chain = chainEntry.Value;
                
                // Wir brauchen door != null hier nicht, da ALL_RELIC_CHAINS nur 
                // mit erfolgreich instanziierten Türen gefüllt wird.

                CheckKeepChain(door, chain);
            }

            // NEU: Fallback-Meldung, falls das Dictionary leer ist
            if (ALL_RELIC_CHAINS.Count == 0)
            {
                 log.Warn("RelicGateMgr: ALL_RELIC_CHAINS is empty. NO Relic Gates were successfully assigned by DoorMgr.");
            }
        }

        private static void CheckKeepChain(RelicGate door, List<int> chain)
        {
            AbstractGameKeep homeKeep = GameServer.KeepManager.GetKeepByID(chain[0]);
            
            if (homeKeep == null)
            {
                if (door.State != eDoorState.Open) door.OpenDoor();
                log.Warn($"RelicGateMgr: Home Keep (ID: {chain[0]}) for chain not found. Door state forced to OPEN.");
                return;
            }

            eRealm requiredRealm = homeKeep.OriginalRealm;

            bool isChainBlocked = false; 
            
            for (int i = 0; i < chain.Count; i++)
            {
                AbstractGameKeep currentKeep = GameServer.KeepManager.GetKeepByID(chain[i]);
                
                if (currentKeep == null) continue;
                
                log.Info($"DEBUG: RelicGateMgr - Keep ID {currentKeep.KeepID} ({currentKeep.Name}). Current Realm: {currentKeep.Realm}. Required Realm: {requiredRealm}.");
                
                if (currentKeep.Realm == requiredRealm || currentKeep.Realm == eRealm.None)
                {
                    isChainBlocked = true;
                    log.Info($"DEBUG: Chain BLOCKED by Keep ID {currentKeep.KeepID}. BREAKING LOOP.");
                    break;
                }
            }
            
            if (isChainBlocked)
            {
                if (door.State != eDoorState.Closed)
                {
                    door.CloseDoor();
                    log.Info($"ACTION: Relic Gate {door.Name} CLOSED. Kette blockiert durch Realm {requiredRealm} oder Neutral.");
                }
            }
            else 
            {
                if (door.State != eDoorState.Open)
                {
                    door.OpenDoor();
                    log.Info($"ACTION: Relic Gate {door.Name} OPENED. ALLE Keeps von Feinden erobert.");
                }
            }
        }

        public static void OnKeepChange(DOLEvent e, object sender, EventArgs args)
        {
            log.Warn("!! RELICGATE MGR EVENT FIRED !! Starting check..."); 
            CheckKeeps();
            log.Info("RelicGateMgr: Re-checking all Relic Gates after Keep change event.");
        }

        // ====================================================================
        // 6. WARMAP-STATUS FÜR DEN CLIENT (Flamme / Offene Tür)
        // ====================================================================

        /// <summary>
        /// Erstellt die 16-Bit-Flag-Maske für die Warmap, basierend auf dem aktuellen Zustand
        /// der Reliktore (offen = brennt/offen auf Warmap).
        /// </summary>
        public static ushort GetRelicTempleWarmapFlags()
        {
            ushort flags = 0;

            // --- ALBION ---
            // Castle Excalibur = 1 << 1 (Albion Strength)
            if (Door_Alb_Strength != null && Door_Alb_Strength.State == eDoorState.Open)
            {
                flags |= (1 << 1); 
            }
            // Castle Myrddin = 1 << 3 (Albion Power/Magic)
            if (Door_Alb_Power != null && Door_Alb_Power.State == eDoorState.Open)
            {
                flags |= (1 << 3); 
            }
            
            // --- MIDGARD ---
            // Mjollner Faste = 1 << 5 (Midgard Strength)
            if (Door_Mid_Strength != null && Door_Mid_Strength.State == eDoorState.Open)
            {
                flags |= (1 << 5); 
            }
            // Grallarhorn Faste = 1 << 7 (Midgard Power/Magic)
            if (Door_Mid_Power != null && Door_Mid_Power.State == eDoorState.Open)
            {
                flags |= (1 << 7);
            }
            
            // --- HIBERNIA ---
            // Dun Lamfhota = 1 << 9 (Hibernia Strength)
            if (Door_Hib_Strength != null && Door_Hib_Strength.State == eDoorState.Open)
            {
                flags |= (1 << 9); 
            }
            // Dun Dagda = 1 << 11 (Hibernia Power/Magic)
            if (Door_Hib_Power != null && Door_Hib_Power.State == eDoorState.Open)
            {
                flags |= (1 << 11);
            }

            return flags;
        }
        
    }
}