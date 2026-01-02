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
using DOL.Language; 

namespace DOL.GS.Keeps
{
    public class RelicGateMgr
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // ====================================================================
        // 1. KONSTANTEN FÜR DIE RELIKTÜR-ZUWEISUNG (Bleiben INT)
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
        
        private static readonly Dictionary<RelicGate, List<int>> ALL_RELIC_CHAINS = new(); 

        private static readonly List<int> ALB_POWER_CHAIN = new List<int> { 50, 51, 53, 55 }; 
        public static RelicGate Door_Alb_Power = null; 

        private static readonly List<int> ALB_STRENGTH_CHAIN = new List<int> { 50, 52, 54, 56 };
        public static RelicGate Door_Alb_Strength = null; 
        
        private static readonly List<int> MID_POWER_CHAIN = new List<int> { 75, 76, 79, 81 };
        public static RelicGate Door_Mid_Power = null;
        
        private static readonly List<int> MID_STRENGTH_CHAIN = new List<int> { 75, 78, 77, 82 };
        public static RelicGate Door_Mid_Strength = null;
        
        private static readonly List<int> HIB_POWER_CHAIN = new List<int> { 100, 101, 103, 105 };
        public static RelicGate Door_Hib_Power = null;
        
        private static readonly List<int> HIB_STRENGTH_CHAIN = new List<int> { 100, 102, 104, 106 };
        public static RelicGate Door_Hib_Strength = null;

        // ====================================================================
        // 3. ZUWEISUNGSMETHODE
        // ====================================================================
        
        public static void AssignRelicDoor(RelicGate door, int internalID)
        {
            if (door == null) return;
            
            // Setzen der statischen Variablen
            if (internalID == DOOR_ALB_POWER_ID) Door_Alb_Power = door;
            else if (internalID == DOOR_ALB_STRENGTH_ID) Door_Alb_Strength = door;
            else if (internalID == DOOR_MID_POWER_ID) Door_Mid_Power = door;
            else if (internalID == DOOR_MID_STRENGTH_ID) Door_Mid_Strength = door;
            else if (internalID == DOOR_HIB_POWER_ID) Door_Hib_Power = door;
            else if (internalID == DOOR_HIB_STRENGTH_ID) Door_Hib_Strength = door;

            // Hinzufügen der Tür und Kette zum Dictionary
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
            GameEventMgr.AddHandler(KeepEvent.KeepTaken, new DOLEventHandler(OnKeepChange));
            
            log.Info("RelicGateMgr: Starting initial keep check...");
            CheckKeeps();
        }
        
        public static void CheckKeeps()
        {
            log.Info("DEBUG: CheckKeeps is running..."); 
            
            foreach (var chainEntry in ALL_RELIC_CHAINS)
            {
                RelicGate door = chainEntry.Key;
                List<int> chain = chainEntry.Value;
                
                CheckKeepChain(door, chain);
            }

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
                // Wenn HomeKeep fehlt, sollte die Tür offen sein, aber wir vermeiden unnötigen Logik-Aufruf
                if (door.State != eDoorState.Open) 
                {
                    door.OpenDoor();
                }
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
                    SendRelicDoorStatusMessage(door, false);
                    
                    log.Info($"ACTION: Relic Gate {door.Name} CLOSED. Kette blockiert durch Realm {requiredRealm} oder Neutral.");
                }
            }
            else 
            {
                if (door.State != eDoorState.Open)
                {
                    door.OpenDoor();
                    SendRelicDoorStatusMessage(door, true);

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
            if (Door_Alb_Strength != null && Door_Alb_Strength.State == eDoorState.Open)
            {
                flags |= (1 << 1); 
            }
            if (Door_Alb_Power != null && Door_Alb_Power.State == eDoorState.Open)
            {
                flags |= (1 << 3); 
            }
            
            // --- MIDGARD ---
            if (Door_Mid_Strength != null && Door_Mid_Strength.State == eDoorState.Open)
            {
                flags |= (1 << 5); 
            }
            if (Door_Mid_Power != null && Door_Mid_Power.State == eDoorState.Open)
            {
                flags |= (1 << 7);
            }
            
            // --- HIBERNIA ---
            if (Door_Hib_Strength != null && Door_Hib_Strength.State == eDoorState.Open)
            {
                flags |= (1 << 9); 
            }
            if (Door_Hib_Power != null && Door_Hib_Power.State == eDoorState.Open)
            {
                flags |= (1 << 11);
            }

            return flags;
        }

        // ====================================================================
        // 7. ZENTRIERTE NACHRICHTEN LOGIK (NEU)
        // ====================================================================

        /// <summary>
        /// Identifiziert die Tür und sendet eine globale zentrierte Nachricht über den Statuswechsel.
        /// </summary>
        /// <param name="door">Das RelictGate-Objekt.</param>
        /// <param name="isOpen">True, wenn die Tür geöffnet wurde; False, wenn sie geschlossen wurde.</param>
        private static void SendRelicDoorStatusMessage(RelicGate door, bool isOpen)
        {
            if (door == null) return;
            
            // --- KORRIGIERTER ZUGRIFF: Verwende door.DoorId, die Property, die auf DbDoor.InternalID zugreift.
            // Dies ist die numerische ID (z.B. 175014001) und löst den GUID-Konvertierungsfehler.
            int doorID = door.DoorId; 
            // ---------------------------------------------------------------------------------------------

            string realmName = "";
            string relicType = "";
            string action = isOpen ? "opened" : "closed";

            // Bestimmen des Reichs und Relikttyps basierend auf der doorID (int == int)
            if (doorID == DOOR_ALB_POWER_ID || doorID == DOOR_ALB_STRENGTH_ID)
                realmName = "Albion's";
            else if (doorID == DOOR_MID_POWER_ID || doorID == DOOR_MID_STRENGTH_ID)
                realmName = "Midgard's";
            else if (doorID == DOOR_HIB_POWER_ID || doorID == DOOR_HIB_STRENGTH_ID)
                realmName = "Hibernia's";

            if (doorID == DOOR_ALB_POWER_ID || doorID == DOOR_MID_POWER_ID || doorID == DOOR_HIB_POWER_ID)
                relicType = "Power";
            else if (doorID == DOOR_ALB_STRENGTH_ID || doorID == DOOR_MID_STRENGTH_ID || doorID == DOOR_HIB_STRENGTH_ID)
                relicType = "Strength";

            if (string.IsNullOrEmpty(realmName) || string.IsNullOrEmpty(relicType))
            {
                log.Warn($"RelicGateMgr: Unbekannte Tür-ID {doorID} konnte nicht benachrichtigt werden.");
                return;
            }

            // Nachricht formatieren
            string message = $"{realmName} {relicType} relic door has been {action}.";
            
            // Senden der zentrierten Nachricht
            PlayerMgr.BroadcastCenteredSystemMessage(message);
            
            log.Info($"GLOBAL: Sent Relic Door status message: {message}");
        }
    }
}