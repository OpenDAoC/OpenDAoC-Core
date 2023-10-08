﻿using System;
using System.Collections.Generic;
using System.Timers;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

#region Apoc Initializator
namespace DOL.GS
{
    public class ApocInitializator : GameNPC
    {
        public ApocInitializator() : base() { }
        public static bool spawn_apoc = false;
        public static bool start_respawn_check = false;
        public static bool StartEncounter = false;

        #region Timer cycling and repeatable dostuff
        public void StartTimer()
        {
            Timer myTimer = new Timer();
            myTimer.Elapsed += new ElapsedEventHandler(DisplayTimeEvent);
            myTimer.Interval = 1000; // 1000 ms is one second
            myTimer.Start();
        }
        public void DisplayTimeEvent(object source, ElapsedEventArgs e)
        {
            DoStuff();
        }
        public void DoStuff()
        {
            if (this.IsAlive)
            {
                PlayerEnter();
                DontAllowLeaveRoom();
                StartRespawnEncounter();
            }
        }
        #endregion

        #region Message Timers and Broadcast
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public int Message_timer(ECSGameTimer timer)
        {         
            BroadcastMessage(String.Format("Fames says loudly, 'I sense presence of many, the presence of power and ambition...'"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer2), 6000);//60s before starting
            return 0;
        }
        public int Message_timer2(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("Morbus says, 'The presence of those who would challenge fate.'"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer3), 6000);//60s before starting
            return 0;
        }
        public int Message_timer3(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("Bellum says, '...challenge the inevitable.'"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer4), 5000);//60s before starting
            return 0;
        }
        public int Message_timer4(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("Morbus says, 'Fate cannot be changed.'"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer5), 5000);//60s before starting
            return 0;
        }
        public int Message_timer5(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("Funus says with a gravely hiss, 'It is the fate of man to die, to expire like the flame of a candle.'"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer6), 8000);//60s before starting
            return 0;
        }
        public int Message_timer6(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("Bellum says, 'It is the fate of man to know pain and loss.'"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer7), 6000);//60s before starting
            return 0;
        }
        public int Message_timer7(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("Morbus says, '... and misery.'"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer8), 5000);//60s before starting
            return 0;
        }
        public static bool FamesWaitForText = false;
        public int Message_timer8(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("Fames asks, 'You, "+RandomTarget.Name+", do you come to challenge fate? Come to me with your answer so that I may see the answer" +
                " in your eyes as well as hear it your voice"));
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnHorsemanFames), 1000);
            return 0;
        }
        #endregion

        #region Spawn Fames Timer
        public int SpawnHorsemanFames(ECSGameTimer timer)
        {
            Fames Add = new Fames();
            Add.X = X;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Flags = eFlags.PEACE;
            Add.Heading = 4072;
            Add.AddToWorld();
            FamesWaitForText = true;
            new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(OtherPlayersCanInteract), 60000);
            return 0;
        }
        public static bool OthersCanInteract = false;
        private int OtherPlayersCanInteract(ECSGameTimer timer)
        {          
            OthersCanInteract = true;
            RandomTarget = null;
            return 0;
        }
        #endregion

        #region Pick Random Player, PlayerEnter
        private bool CheckNullPlayer = false;
        public static GamePlayer randomtarget=null;
        public static GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        List<GamePlayer> PlayersInRoom = new List<GamePlayer>();
        public static bool PickedTarget = false;
        public void PlayerEnter()
        {
            foreach (GamePlayer player in GetPlayersInRadius(1500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)//we pick only players, not gms !
                    {
                        if (!PlayersInRoom.Contains(player))
                            PlayersInRoom.Add(player);
                    }
                }
            }
            if (PickedTarget == false && PlayersInRoom.Count > 0)
            {
                GamePlayer ptarget = ((GamePlayer)(PlayersInRoom[Util.Random(1, PlayersInRoom.Count) - 1]));
                RandomTarget = ptarget;

                if(RandomTarget != null)
                    new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Message_timer), 4000);
                PickedTarget = true;
            }
        }
        #endregion

        #region Respawn Whole Encounter
        public void StartRespawnEncounter()
        {
            if (IsAlive)
            {
                if (!Fames.FamesIsUp && !Bellum.BellumUP && !Morbus.MorbusUP && !Funus.FunusUp && !Apocalypse.ApocUP && !start_respawn_check)
                {
                    RandomTarget = null;//reset picked player
                    PlayersInRoom.Clear();
                    int time = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000miliseconds 
                    new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoRespawnNow), time);
                    log.Debug("Starting respawn time for final caer sidi encounter, will respawn in " + time / 60000 + " minutes!");
                    start_respawn_check = true;
                }
            }
        }
        public int DoRespawnNow(ECSGameTimer timer)
        {           
            PickedTarget = false;//we start encounter again here!
            OthersCanInteract = false;//other players can interact too!
            return 0;
        }
        #endregion

        #region AddToWorld Initialize mob
        public override bool AddToWorld()
        {
            StartTimer();
            ApocIniBrain hi = new ApocIniBrain();
            SetOwnBrain(hi);
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Apoc Initializator", 60, (ERealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Apoc Initializator not found, creating it...");

                log.Warn("Initializing Apoc Initializator...");
                ApocInitializator CO = new ApocInitializator();
                CO.Name = "Apoc Initializator";
                CO.GuildName = "DO NOT REMOVE!";
                CO.RespawnInterval = 5000;
                CO.Model = 665;
                CO.Realm = 0;
                CO.Level = 50;
                CO.Size = 50;
                CO.CurrentRegionID = 60;//caer sidi
                CO.Flags ^= eFlags.CANTTARGET;
                CO.Flags ^= eFlags.FLYING;
                CO.Flags ^= eFlags.DONTSHOWNAME;
                CO.Flags ^= eFlags.PEACE;
                CO.Faction = FactionMgr.GetFactionByID(64);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
                CO.X = 29462;
                CO.Y = 25240;
                CO.Z = 19490;
                ApocIniBrain ubrain = new ApocIniBrain();
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.SaveIntoDatabase();
                CO.Brain.Start();
            }
            else
                log.Warn("Apoc Initializator exist ingame, remove it and restart server if you want to add by script code.");
        }
        #endregion

        #region Dont allow players to leave room during encounter fights
        public void DontAllowLeaveRoom()
        {
            Point3D point1 = new Point3D();
            point1.X = 29459; point1.Y = 26401; point1.Z = 19503;

            if(this.CurrentRegionID == 60)//caer sidi
            {
                if(FamesBrain.StartedFames==true || BellumBrain.StartedBellum==true || MorbusBrain.StartedMorbus==true || FunusBrain.StartedFunus==true || ApocalypseBrain.StartedApoc==true)
                {
                    foreach (GamePlayer player in GetPlayersInRadius(1500))
                    {
                        if(player != null)
                        {
                            if(player.IsAlive)
                            {
                                if(player.IsWithinRadius(point1,150) && player.Client.Account.PrivLevel == 1)//only players will be ported back
                                {
                                    player.MoveTo(60, 29469, 25244, 19490, 2014);
                                    player.Out.SendMessage("Magic energy moves you to the center of room!", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                //dont move player if it's not caer sidi
            }
        }
        #endregion
    }
}
#region Initializator Brain
namespace DOL.AI.Brain
{
    public class ApocIniBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ApocIniBrain()
            : base()
        {
            ThinkInterval = 2000;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}
#endregion
#endregion

#region 1st Horseman Fames
////////////////////////////////////////////////////////////1st Horseman - Fames///////////////////////////////////
namespace DOL.GS
{
    public class Fames : GameEpicBoss
    {
        public Fames() : base() { }
        public int StartFamesTimer(ECSGameTimer timer)
        {
            Flags = 0;
            return 0;
        }
        public static bool CanInteract = false;
        public static bool FamesIsUp = true;
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 40; // dmg reduction for melee dmg
                case EDamageType.Crush: return 40; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
        }
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            GamePlayer player2 = ApocInitializator.RandomTarget;
            if (CanInteract == false)
            {
                if (player == player2)
                {
                    TurnTo(player.X, player.Y);

                    player.Out.SendMessage("Fames says, Well? Do you challenge fate itself?\n" +
                        "Say [no] and walk away...\n" +
                        "Say [yes] and prepare yourselves.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
            }
            if (ApocInitializator.OthersCanInteract == true)
            {
                if (player != null)
                {
                    TurnTo(player.X, player.Y);

                    player.Out.SendMessage("Fames says, Well? Do you challenge fate itself?\n" +
                        "Say [no] and walk away...\n" +
                        "Say [yes] and prepare yourselves.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
            }
            return true;
        }      
        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str)) return false;
            if (!(source is GamePlayer)) return false;
            GamePlayer t = (GamePlayer)source;
            if (CanInteract == false)
            {            
                if (t == ApocInitializator.RandomTarget || ApocInitializator.OthersCanInteract == true)
                {
                    TurnTo(t.X, t.Y);
                    switch (str.ToLower())
                    {
                        case "no":
                            {
                                t.Out.SendMessage("Then be gone and continue on with what you were meant to do.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                            }
                            break;
                        case "yes":
                            {
                                foreach (GamePlayer player in GetPlayersInRadius(2500))
                                {
                                    if (player != null)
                                    {
                                        player.Out.SendMessage("Fames says, 'Done. You are brave " + t.CharacterClass.Name + " ... or foolish. While it is most certain that your" +
                                            " actions will have little chance to alter the course of fate, you and your companions are granted a few grains of time," +
                                            " two minutes in your terms, to prepare.", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                                        new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(StartFamesTimer), 120000);//2min
                                        CanInteract = true;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            else
                return false;
            return true;
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        private bool prepareBellum = false;
        public override void Die(GameObject killer)//on kill generate orbs
        {
            
            if(!prepareBellum)
            { 
                BroadcastMessage(String.Format("Bellum says, 'Prepare yourselves for war. One minute, you are granted.'"));
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnHorsemanBellum), 60000);//60s before starting
                prepareBellum = true;
            }
            FamesIsUp = false;
            FamesBrain.StartedFames = false;
            base.Die(killer);
        }
        public int SpawnHorsemanBellum(ECSGameTimer timer)
        {
            Bellum Add = new Bellum();
            Add.X = 29468;
            Add.Y = 25235;
            Add.Z = 19490;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 29;
            Add.AddToWorld();
            return 0;
        }

        public override bool AddToWorld()
        {
            Model = 938;
            MeleeDamageType = EDamageType.Body;
            Name = "Fames";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 120;
            Level = 83;
            MaxSpeedBase = 300;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160695);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = ERealm.None;
            FamesBrain.spawn_fate = false;
            CanInteract = false;
            FamesBrain.StartedFames = false;
            FamesIsUp = true;
            prepareBellum = false;

            FamesBrain adds = new FamesBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class FamesBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public FamesBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }
        public static bool BafMobs = false;
        public static bool spawn_fate = false;
        public static bool StartedFames = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                BafMobs = false;
                StartedFames = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.InCombat || HasAggro || Body.attackComponent.AttackState == true)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
            {
                StartedFames = true;
                if (spawn_fate == false)
                {
                    SpawnFateBearer();
                    spawn_fate = true;
                }
                if (BafMobs == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "FamesBaf" && npc is GameEpicNPC)
                            {
                                AddAggroListTo(npc.Brain as StandardMobBrain);// add to aggro mobs with FamesBaf PackageID
                                BafMobs = true;
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        public void SpawnFateBearer()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
            GameEpicNPC Add = new GameEpicNPC();
            Add.LoadTemplate(npcTemplate);
            Add.X = Body.X - 100;
            Add.Y = Body.Y;
            Add.Z = Body.Z;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "FamesBaf";
            Add.Faction = FactionMgr.GetFactionByID(64);
            Add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Add.AddToWorld();
        }
    }
}
#endregion

#region 2nd Horseman Bellum
//////////////////////////////////////////////////2nd Horseman - Bellum////////////////////////////////////
namespace DOL.GS
{
    public class Bellum : GameEpicBoss
    {
        public Bellum() : base() { }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public static bool BellumUP = true;
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        private bool prepareMorbus = false;
        public override void Die(GameObject killer)//on kill generate orbs
        {
            if (!prepareMorbus)
            {
                BroadcastMessage(String.Format("Morbus says, 'Sometimes it is the smallest things that are the most deadly. Be prepared in one minute..'"));
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnHorsemanMorbus), 60000);//60s before starting
                prepareMorbus = true;
            }

            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if(npc != null)
                {
                    if(npc.IsAlive)
                    {
                        if(npc.Brain is WarIncarnateCrushBrain || npc.Brain is WarIncarnateSlashBrain || npc.Brain is WarIncarnateThrustBrain)
                        {
                            npc.Die(this);
                        }
                    }
                }
            }
            
            BellumBrain.StartedBellum = false;
            BellumUP = false;
            spawn_fate2 = false;
            base.Die(killer);
        }
        public int SpawnHorsemanMorbus(ECSGameTimer timer)
        {
            Morbus Add = new Morbus();
            Add.X = 29467;
            Add.Y = 25235;
            Add.Z = 19490;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 29;
            Add.AddToWorld();
            return 0;
        }
        public static bool spawn_fate2 = false;
        public void SpawnFateBearer()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
            GameEpicNPC Add = new GameEpicNPC();
            Add.LoadTemplate(npcTemplate);
            Add.X = X - 100;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegionID = CurrentRegionID;
            Add.Heading = Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            Add.Faction = FactionMgr.GetFactionByID(64);
            Add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Add.AddToWorld();
        }

        public override bool AddToWorld()
        {
            Model = 927;
            MeleeDamageType = EDamageType.Body;
            Name = "Bellum";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 140;
            Level = 83;
            MaxSpeedBase = 300;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158353);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = ERealm.None;
            BellumBrain.StartedBellum = false;
            BellumBrain.SpawnWeapons = false;
            BellumUP = true;
            prepareMorbus = false;

            AbilityBonus[(int)EProperty.Resist_Body] = -10;
            AbilityBonus[(int)EProperty.Resist_Heat] = -10;
            AbilityBonus[(int)EProperty.Resist_Cold] = -10;
            AbilityBonus[(int)EProperty.Resist_Matter] = -10;
            AbilityBonus[(int)EProperty.Resist_Energy] = -10;
            AbilityBonus[(int)EProperty.Resist_Spirit] = -10;
            AbilityBonus[(int)EProperty.Resist_Slash] = 99;
            AbilityBonus[(int)EProperty.Resist_Crush] = 99;
            AbilityBonus[(int)EProperty.Resist_Thrust] = 99;

            if (spawn_fate2 == false)
            {
                SpawnFateBearer();
                spawn_fate2 = true;
            }
            BellumBrain adds = new BellumBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class BellumBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public BellumBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1200;
            ThinkInterval = 2000;
        }
        public static bool StartedBellum= false;
        public static bool SpawnWeapons = false;
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                StartedBellum = false;
                SpawnWeapons = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive)
                            {
                                if (npc.Brain is WarIncarnateCrushBrain || npc.Brain is WarIncarnateSlashBrain || npc.Brain is WarIncarnateThrustBrain)
                                {
                                    npc.Die(Body);
                                }
                            }
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (HasAggro && Body.TargetObject != null)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
            {
                StartedBellum = true;
                RemoveAdds = false;
                if(SpawnWeapons==false)
                {
                    SpawnCrushWeapon();
                    SpawnSlashWeapon();
                    SpawnThrustWeapon();
                    SpawnWeapons = true;
                }
            }
            base.Think();
        }

        public void SpawnCrushWeapon()
        {
            WarIncarnateCrush Add = new WarIncarnateCrush();
            Add.X = Body.X;
            Add.Y = Body.Y + 200;
            Add.Z = Body.Z;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.MeleeDamageType = EDamageType.Crush;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            WarIncarnateCrushBrain adds = new WarIncarnateCrushBrain();
            Add.SetOwnBrain(adds);
            Add.AddToWorld();
        }
        public void SpawnSlashWeapon()
        {
            WarIncarnateSlash Add = new WarIncarnateSlash();
            Add.X = Body.X - 200;
            Add.Y = Body.Y;
            Add.Z = Body.Z;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.MeleeDamageType = EDamageType.Slash;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            WarIncarnateSlashBrain adds = new WarIncarnateSlashBrain();
            Add.SetOwnBrain(adds);
            Add.AddToWorld();
        }
        public void SpawnThrustWeapon()
        {
            WarIncarnateThrust Add = new WarIncarnateThrust();
            Add.X = Body.X + 200;
            Add.Y = Body.Y;
            Add.Z = Body.Z;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.MeleeDamageType = EDamageType.Thrust;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            WarIncarnateThrustBrain adds = new WarIncarnateThrustBrain();
            Add.SetOwnBrain(adds);
            Add.AddToWorld();
        }
    }
}
#region Bellum adds crush dmg
/////////////////////////////////////////////Bellum Adds flying weapons///////////////////////////
namespace DOL.GS
{
    public class WarIncarnateCrush : GameNPC
    {
        public WarIncarnateCrush() : base() { }

        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }

        public override int MaxHealth
        {
            get { return 10000; }
        }

        public override bool AddToWorld()
        {
            int random = Util.Random(1, 3);
            switch(random)
            {
                case 1:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 17, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;

                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 70, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 12, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.Standard);
                        VisibleActiveWeaponSlots = 10;
                    }
                    break;
            }          
            Model = 665;
            Name = "war incarnate";
            MeleeDamageType = EDamageType.Crush;
            RespawnInterval = -1;
            MaxSpeedBase = 210;
            Strength = 150;
            Piety = 250;

            Size = 100;
            Level = 75;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Realm = ERealm.None;
            WarIncarnateCrushBrain adds = new WarIncarnateCrushBrain();
            LoadedFromScript = true;
            WarIncarnateCrushBrain.spawn_copies = false;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class WarIncarnateCrushBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public WarIncarnateCrushBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1000;
        }
        public void SpawnCrushWeapons()
        {
            for (int i = 0; i < 4; i++)
            {
                GameLiving ptarget = CalculateNextAttackTarget();
                WarIncarnateCrush Add = new WarIncarnateCrush();
                int random = Util.Random(1, 3);
                switch (random)
                {
                    case 1:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 17, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 2:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 70, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 3:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 12, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.Standard);
                            Add.VisibleActiveWeaponSlots = 10;
                        }
                        break;
                }
                Add.X = Body.X + Util.Random(-200, 200);
                Add.Y = Body.Y + Util.Random(-200, 200);
                Add.Z = Body.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.MeleeDamageType = EDamageType.Crush;
                Add.Heading = Body.Heading;
                Add.RespawnInterval = -1;
                Add.PackageID = "BellumBaf";
                WarIncarnateCrushBrain smb = new WarIncarnateCrushBrain();
                smb.AggroLevel = 100;
                smb.AggroRange = 1000;
                Add.AddBrain(smb);
                Add.AddToWorld();
                WarIncarnateCrushBrain brain = (WarIncarnateCrushBrain)Add.Brain;
                brain.AddToAggroList(ptarget, 1);
                Add.StartAttack(ptarget);
            }
        }
        public static bool spawn_copies = false;
        public override void Think()
        {
            if (Body.IsAlive)
            {
                if(spawn_copies==false)
                {
                    SpawnCrushWeapons();
                    spawn_copies = true;
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Bellum adds slash dmg
namespace DOL.GS
{
    public class WarIncarnateSlash : GameNPC
    {
        public WarIncarnateSlash() : base() { }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }  
        
        public override int MaxHealth
        {
            get { return 10000; }
        }

        public override bool AddToWorld()
        {
            int random = Util.Random(1, 4);
            switch (random)
            {
                case 1:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 6, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;

                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 73, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 67, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 4:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.Standard);
                        VisibleActiveWeaponSlots = 10;
                    }
                    break;
            }
            Model = 665;
            Name = "war incarnate";
            MeleeDamageType = EDamageType.Slash;
            RespawnInterval = -1;
            MaxSpeedBase = 210;
            Strength = 150;
            Piety = 250;

            Size = 100;
            Level = 75;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Realm = ERealm.None;
            WarIncarnateSlashBrain adds = new WarIncarnateSlashBrain();
            LoadedFromScript = true;
            WarIncarnateSlashBrain.spawn_copies2 = false;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class WarIncarnateSlashBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public WarIncarnateSlashBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1000;
        }
        public void SpawnSlashWeapons()
        {
            for (int i = 0; i < 4; i++)
            {
                GameLiving ptarget = CalculateNextAttackTarget();
                WarIncarnateSlash Add = new WarIncarnateSlash();
                int random = Util.Random(1, 4);
                switch (random)
                {
                    case 1:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 6, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 2:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 73, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 3:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 67, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 4:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.Standard);
                            Add.VisibleActiveWeaponSlots = 10;
                        }
                        break;
                }
                Add.X = Body.X + Util.Random(-200, 200);
                Add.Y = Body.Y + Util.Random(-200, 200);
                Add.Z = Body.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.MeleeDamageType = EDamageType.Slash;
                Add.Heading = Body.Heading;
                Add.RespawnInterval = -1;
                Add.PackageID = "BellumBaf";
                WarIncarnateSlashBrain smb = new WarIncarnateSlashBrain();
                smb.AggroLevel = 100;
                smb.AggroRange = 1000;
                Add.AddBrain(smb);
                Add.AddToWorld();
                WarIncarnateSlashBrain brain = (WarIncarnateSlashBrain)Add.Brain;
                brain.AddToAggroList(ptarget, 1);
                Add.StartAttack(ptarget);
            }
        }
        public static bool spawn_copies2 = false;
        public override void Think()
        {
            if (Body.IsAlive)
            {
                if (spawn_copies2 == false)
                {
                    SpawnSlashWeapons();
                    spawn_copies2 = true;
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Bellum adds thrust dmg
namespace DOL.GS
{
    public class WarIncarnateThrust : GameNPC
    {
        public WarIncarnateThrust() : base() { }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }

        public override int MaxHealth
        {
            get { return 10000; }
        }

        public override bool AddToWorld()
        {
            int random = Util.Random(1, 3);
            switch (random)
            {
                case 1:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 69, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;

                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 846, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 886, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(EActiveWeaponSlot.Standard);
                        VisibleActiveWeaponSlots = 255;
                    }
                    break;
            }
            Model = 665;
            Name = "war incarnate";
            MeleeDamageType = EDamageType.Thrust;
            RespawnInterval = -1;
            MaxSpeedBase = 210;
            Strength = 150;
            Piety = 250;

            Size = 100;
            Level = 75;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Realm = ERealm.None;
            WarIncarnateThrustBrain adds = new WarIncarnateThrustBrain();
            LoadedFromScript = true;
            WarIncarnateThrustBrain.spawn_copies3 = false;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class WarIncarnateThrustBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public WarIncarnateThrustBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1000;
        }
        public void SpawnThrustWeapons()
        {
            for (int i = 0; i < 4; i++)
            {
                GameLiving ptarget = CalculateNextAttackTarget();
                WarIncarnateThrust Add = new WarIncarnateThrust();
                int random = Util.Random(1, 3);
                switch (random)
                {
                    case 1:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 69, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;

                        }
                        break;
                    case 2:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 846, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 3:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 886, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(EActiveWeaponSlot.Standard);
                            Add.VisibleActiveWeaponSlots = 255;
                        }
                        break;
                }
                Add.X = Body.X + Util.Random(-200, 200);
                Add.Y = Body.Y + Util.Random(-200, 200);
                Add.Z = Body.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.MeleeDamageType = EDamageType.Thrust;
                Add.Heading = Body.Heading;
                Add.RespawnInterval = -1;
                Add.PackageID = "BellumBaf";
                WarIncarnateThrustBrain smb = new WarIncarnateThrustBrain();
                smb.AggroLevel = 100;
                smb.AggroRange = 1000;
                Add.AddBrain(smb);
                Add.AddToWorld();
                WarIncarnateThrustBrain brain = (WarIncarnateThrustBrain)Add.Brain;
                brain.AddToAggroList(ptarget, 1);
                Add.StartAttack(ptarget);
            }
        }
        public static bool spawn_copies3 = false;
        public override void Think()
        {
            if (Body.IsAlive)
            {
                if (spawn_copies3 == false)
                {
                    SpawnThrustWeapons();
                    spawn_copies3 = true;
                }
            }
            base.Think();
        }
    }
}
#endregion
#endregion

#region 3th Horseman Morbus
namespace DOL.GS
{
    public class Morbus : GameEpicBoss
    {
        public Morbus() : base() { }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override void ReturnToSpawnPoint(short speed)
        {
            if (MorbusBrain.IsBug)
                return;

            base.ReturnToSpawnPoint(speed);
        }
        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (Morbus_Swarm_count > 0)
                {
                    if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
                        || damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                        || damageType == EDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GameSummonedPet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public static bool MorbusUP = true;
        private bool prepareFunus = false;
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void Die(GameObject killer)//on kill generate orbs
        {
            if (!prepareFunus)
            {
                BroadcastMessage(String.Format("Funus says, 'Prepare to die. Sixty seconds you are given to arrange for the event.'\n" +
                "For a brief moment, the clerics in the area glow softly as if bathed in a divine light, and their eyes shine as if a sudden" +
                " rush of energy now courses through them. A faint whisper in your mind warns you that mundane attacks on this creature of death" +
                " would have little effect or even make the situation worse, but it also reassures you that the clerics, a direct conduit between the " +
                "divine and this world, posses an unexpected advantage over the creature, Funus."));
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnHorsemanFunus), 60000);//60s before starting
                prepareFunus = true;
            }
            MorbusBrain.StartedMorbus = false;
            spawn_fate3 = false;
            MorbusUP = false;
            base.Die(killer);
        }
        public int SpawnHorsemanFunus(ECSGameTimer timer)
        {
            Funus Add = new Funus();//inside controller
            Add.X = 29467;
            Add.Y = 25235;
            Add.Z = 19490;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 29;
            Add.AddToWorld();
            return 0;
        }
        public static bool spawn_fate3 = false;
        public static int Morbus_Swarm_count = 0;
        public void SpawnFateBearer()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
            GameEpicNPC Add = new GameEpicNPC();
            Add.LoadTemplate(npcTemplate);
            Add.X = X - 100;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegionID = CurrentRegionID;
            Add.Heading = Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "MorbusBaf";
            Add.Faction = FactionMgr.GetFactionByID(64);
            Add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Add.AddToWorld();
        }
        public override bool AddToWorld()
        {
            Model = 952;
            MeleeDamageType = EDamageType.Crush;
            Name = "Morbus";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 140;
            Level = 83;
            MaxSpeedBase = 300;
            
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164171);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = ERealm.None;
            MorbusBrain.StartedMorbus = false;
            MorbusBrain.BafMobs3 = false;
            MorbusBrain.spawn_swarm = false;
            MorbusBrain.message_warning1 = false;
            MorbusBrain.IsBug = false;
            MorbusUP = true;
            prepareFunus = false;

            AbilityBonus[(int)EProperty.Resist_Body] = 26;
            AbilityBonus[(int)EProperty.Resist_Heat] = 26;
            AbilityBonus[(int)EProperty.Resist_Cold] = -15;//weak to cold
            AbilityBonus[(int)EProperty.Resist_Matter] = 26;
            AbilityBonus[(int)EProperty.Resist_Energy] = 26;
            AbilityBonus[(int)EProperty.Resist_Spirit] = 26;
            AbilityBonus[(int)EProperty.Resist_Slash] = 60;
            AbilityBonus[(int)EProperty.Resist_Crush] = 60;
            AbilityBonus[(int)EProperty.Resist_Thrust] = 60;

            if (spawn_fate3 == false)
            {
                SpawnFateBearer();
                spawn_fate3 = true;
            }

            MorbusBrain adds = new MorbusBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class MorbusBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MorbusBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1200;
            ThinkInterval = 2000;
        }
        public static bool BafMobs3 = false;
        public static bool spawn_swarm = false;
        public static bool message_warning1 = false;
        public static bool IsBug = false;
        public static bool StartedMorbus = false;
        private bool RemoveAdds = false;
        public override void AttackMostWanted()
        {
            if (IsBug == true)
                return;
            else
            {
                base.AttackMostWanted();
            }
        }
        public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
        {
            if (IsBug == true)
                return;
            else
            {
                base.OnAttackedByEnemy(ad);
            }
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                StartedMorbus = false;
                BafMobs3 = false;
                message_warning1 = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive)
                            {
                                if (npc.PackageID == "MorbusBaf" && npc.Brain is MorbusSwarmBrain)
                                {
                                    npc.RemoveFromWorld();
                                    Morbus.Morbus_Swarm_count = 0;
                                }
                            }
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
                BafMobs3 = false;
            }
            else if (Body.InCombatInLast(40 * 1000) == false && this.Body.InCombatInLast(45 * 1000))
            {
                Body.Health = Body.MaxHealth;
                Body.Model = 952;
                Body.Size = 140;
                IsBug = false;
                ClearAggroList();
            }
            if (HasAggro && Body.TargetObject != null)
                RemoveAdds = false;

            if (Body.InCombat || HasAggro || Body.attackComponent.AttackState == true)
            {
                StartedMorbus = true;
                if(Morbus.Morbus_Swarm_count > 0)
                {
                    Body.Model = 771;
                    Body.Size = 50;
                    IsBug = true;                   
                    if (message_warning1 == false)
                    {
                        BroadcastMessage(String.Format("Morbus looks very pale as he slowly reads over the note."));
                        message_warning1 = true;
                    }
                    Body.StopAttack();
                }
                else
                {
                    Body.Model = 952;
                    Body.Size = 140;
                    IsBug = false;
                    message_warning1 = false;
                }
                if (BafMobs3 == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "MorbusBaf" && npc.Brain is MorbusSwarmBrain)
                            {
                                AddAggroListTo(npc.Brain as MorbusSwarmBrain);// add to aggro mobs with MorbusBaf PackageID
                                BafMobs3 = true;
                            }
                        }
                    }
                }
                if(Morbus.Morbus_Swarm_count == 0)
                {
                    if (spawn_swarm == false)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnSwarm), Util.Random(25000, 40000));//25s-40s

                        foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                        {
                            if (npc != null)
                            {
                                if (npc.IsAlive && npc.PackageID == "MorbusBaf" && npc.Brain is MorbusSwarmBrain)
                                {
                                    AddAggroListTo(npc.Brain as MorbusSwarmBrain);// add to aggro mobs with MorbusBaf PackageID
                                }
                            }
                        }
                        spawn_swarm = true;
                    }
                }
            }
            base.Think();
        }
        public int SpawnSwarm(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                for (int i = 0; i < Util.Random(6,10); i++)
                {
                    MorbusSwarm Add = new MorbusSwarm();
                    Add.X = Body.X + Util.Random(-100, 100);
                    Add.Y = Body.Y + Util.Random(-100, 100);
                    Add.Z = Body.Z;
                    Add.CurrentRegionID = Body.CurrentRegionID;
                    Add.Heading = Body.Heading;
                    Add.RespawnInterval = -1;
                    Add.PackageID = "MorbusBaf";
                    Add.AddToWorld();
                    ++Morbus.Morbus_Swarm_count;
                }
            }
            spawn_swarm=false;
            return 0;
        }
    }
}
/////////////////////////////////////////////////Morbus Swarm///////////////////////////////////////////////////////
#region Morbus Swarm
namespace DOL.GS
{
    public class MorbusSwarm : GameNPC
    {
        public MorbusSwarm() : base() { }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 15000; }
        }
        public override void Die(GameObject killer)
        {
            --Morbus.Morbus_Swarm_count;
            base.Die(killer);
        }

        public override void AutoSetStats(DbMob dbMob = null)
        {
            if (PackageID == "MorbusBaf")
                return;
            base.AutoSetStats(dbMob);
        }
        public override bool AddToWorld()
        {         
            Name = "swarm of morbus";
            RespawnInterval = -1;
            int random = Util.Random(1, 5);
            switch (random)
            {
                case 1:
                    {
                        Model = 1201;//bug tanky
                        Constitution = 150;
                        Strength = 35;
                        Dexterity = 100;
                        Quickness = 80;
                        Size = (byte)Util.Random(20, 45);
                        MaxSpeedBase = 185;
                        MeleeDamageType = EDamageType.Crush;
                    }
                    break;
                case 2:
                    {
                        Model = 567;//rat dps
                        Constitution = 100;
                        Strength = 55;
                        Dexterity = 100;
                        Quickness = 100;
                        Size = (byte)Util.Random(20, 30);
                        MaxSpeedBase = 200;
                        MeleeDamageType = EDamageType.Slash;
                    }
                    break;
                case 3:
                    {
                        Model = 771;//roach tanky+dps
                        Constitution = 200;
                        Strength = 80;
                        Dexterity = 100;
                        Quickness = 65;
                        Size = (byte)Util.Random(20, 30);
                        MaxSpeedBase = 165;
                        MeleeDamageType = EDamageType.Crush;
                    }
                    break;
                case 4:
                    {
                        Model = 824;//cicada quick attacks
                        Size = (byte)Util.Random(20, 30);
                        Constitution = 100;
                        Strength = 25;
                        Dexterity = 100;
                        Quickness = 200;
                        MaxSpeedBase = 220;
                        MeleeDamageType = EDamageType.Thrust;
                    }
                    break;
                case 5:
                    {
                        Model = 819;//dragonfly quick attacks
                        Size = (byte)Util.Random(20, 30);
                        Constitution = 100;
                        Strength = 25;
                        Dexterity = 100;
                        Quickness = 200;
                        MaxSpeedBase = 220;
                        MeleeDamageType = EDamageType.Thrust;
                    }
                    break;
            }
            MaxDistance = 2500;
            TetherRange = 3000;
            Level = 75;

            AbilityBonus[(int)EProperty.Resist_Body] = 15;
            AbilityBonus[(int)EProperty.Resist_Heat] = 15;
            AbilityBonus[(int)EProperty.Resist_Cold] = -15;//weak to cold
            AbilityBonus[(int)EProperty.Resist_Matter] = 15;
            AbilityBonus[(int)EProperty.Resist_Energy] = 15;
            AbilityBonus[(int)EProperty.Resist_Spirit] = 15;
            AbilityBonus[(int)EProperty.Resist_Slash] = 25;
            AbilityBonus[(int)EProperty.Resist_Crush] = 25;
            AbilityBonus[(int)EProperty.Resist_Thrust] = 25;

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 7;
            Realm = ERealm.None;          

            MorbusSwarmBrain adds = new MorbusSwarmBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class MorbusSwarmBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MorbusSwarmBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1000;
        }
        public override void Think()
        {
            if(Body.InCombat && HasAggro && Body.TargetObject != null)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (target != null && target.IsAlive)
                {
                    if (Util.Chance(15) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
                        Body.CastSpell(BlackPlague, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
            base.Think();
        }
        public Spell m_black_plague;

        public Spell BlackPlague
        {
            get
            {
                if (m_black_plague == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 4375;
                    spell.Icon = 4375;
                    spell.Name = "Touch of Death";
                    spell.Message1 = "You are diseased!";
                    spell.Message2 = "{0} is diseased!";
                    spell.Message3 = "You look healthy.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 4375;
                    spell.Range = 500;
                    spell.Radius = 350;
                    spell.Duration = 120;
                    spell.SpellID = 11737;
                    spell.Target = "Enemy";
                    spell.Type = "Disease";
                    spell.Uninterruptible = true;
                    spell.DamageType = (int)EDamageType.Body; //Energy DMG Type
                    m_black_plague = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_black_plague);
                }
                return m_black_plague;
            }
        }
    }
}
#endregion
#endregion

#region 4th Horseman Funus
////////////////////////////////////////////////////////////4th Horseman - Funus///////////////////////////////////
namespace DOL.GS
{
    public class Funus : GameEpicBoss
    {
        public Funus() : base() { }
        //Funus only take all dmg from clerics melee/magic/dmgadd
        //and some restricted dmg from other classes:
        //-from mercenary by bow
        //-from armsman by crossbow
        //-from infiltrator by crossbow
        //-from scouts by longbow
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(4000))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                GamePlayer truc = source as GamePlayer;
                //cleri,merc,arms,infi,scout
                if (truc.CharacterClass.ID == 6 || (truc.CharacterClass.ID == 11 && truc.ActiveWeapon.Object_Type == 5) || (truc.CharacterClass.ID == 2 && truc.ActiveWeapon.Object_Type == 10) 
                    || (truc.CharacterClass.ID == 9 && truc.ActiveWeapon.Object_Type == 10) || (truc.CharacterClass.ID == 3 && truc.ActiveWeapon.Object_Type == 9))
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
                else
                {
                    truc.Out.SendMessage(Name + " absorbs all your damage to heal iself!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    BroadcastMessage(String.Format("Funus takes damage from " + source.Name + " and restoring it's whole health."));
                    Health += MaxHealth;
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            if(source is GameSummonedPet)
            {
                GameSummonedPet truc = source as GameSummonedPet;
                GamePlayer pet_owner = truc.Owner as GamePlayer;
                pet_owner.Out.SendMessage(Name + " absorbs all your damage to heal iself!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                BroadcastMessage(String.Format("Funus takes damage from " + pet_owner.Name + " and restoring it's whole health."));
                Health += MaxHealth;
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 250;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.10;
        }
        public override int MaxHealth
        {
            get { return 50000; }
        }
        public static bool FunusUp = true;
        private bool prepareApoc = false;
        public override void Die(GameObject killer)//on kill generate orbs
        {
            if (!prepareApoc)
            {
                BroadcastMessage(String.Format("A thunderous voice echoes off the walls, 'Well done. You have succeeded in besting my harbingers.'"));
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnApoc), 5000);
                prepareApoc = true;
            }
            spawn_fate4 = false;
            FunusBrain.StartedFunus = false;
            FunusUp = false;
            base.Die(killer);
        }
        public int SpawnApoc(ECSGameTimer timer)
        {
            Apocalypse Add = new Apocalypse();
            Add.X = 29467;
            Add.Y = 25235;
            Add.Z = 19490;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 29;
            Add.AddToWorld();
            return 0;
        }
        public void SpawnFateBearer()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
            GameEpicNPC Add = new GameEpicNPC();
            Add.LoadTemplate(npcTemplate);
            Add.X = X - 100;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegionID = CurrentRegionID;
            Add.Heading = Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "FunusBaf";
            Add.Faction = FactionMgr.GetFactionByID(64);
            Add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Add.AddToWorld();
        }

        public static bool spawn_fate4 = false;
        public override bool AddToWorld()
        {
            Model = 911;
            MeleeDamageType = EDamageType.Heat;
            Name = "Funus";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 120;
            Level = 83;
            MaxSpeedBase = 300;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161151);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = ERealm.None;
            FunusBrain.StartedFunus = false;
            FunusBrain.BafMobs4 = false;
            FunusUp = true;
            prepareApoc = false;

            AbilityBonus[(int)EProperty.Resist_Body] = -25;
            AbilityBonus[(int)EProperty.Resist_Heat] = -25;
            AbilityBonus[(int)EProperty.Resist_Cold] = -25;
            AbilityBonus[(int)EProperty.Resist_Matter] = -25;
            AbilityBonus[(int)EProperty.Resist_Energy] = -25;
            AbilityBonus[(int)EProperty.Resist_Spirit] = -25;
            AbilityBonus[(int)EProperty.Resist_Slash] = -25;
            AbilityBonus[(int)EProperty.Resist_Crush] = -25;
            AbilityBonus[(int)EProperty.Resist_Thrust] = -25;

            if (spawn_fate4 == false)
            {
                SpawnFateBearer();
                spawn_fate4 = true;
            }
            FunusBrain adds = new FunusBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class FunusBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public FunusBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1200;
            ThinkInterval = 2000;
        }
        public static bool BafMobs4 = false;
        public static bool StartedFunus = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                StartedFunus = false;
                BafMobs4 = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.TargetObject != null && HasAggro)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
            {
                StartedFunus = true;
                if (BafMobs4 == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "FunusBaf" && npc is GameEpicNPC)
                            {
                                AddAggroListTo(npc.Brain as StandardMobBrain);// add to aggro mobs with FamesBaf PackageID
                                BafMobs4 = true;
                            }
                        }
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Apocalypse
////////////////////////////////////////////////////////////Apocalypse///////////////////////////////////
namespace DOL.GS
{
    public class Apocalypse : GameEpicBoss
    {
        public Apocalypse() : base() { }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 40; // dmg reduction for melee dmg
                case EDamageType.Crush: return 40; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
        }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100; 
        }
        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 300000; }
        }
        public static bool ApocUP = true;
        public override void Die(GameObject killer)//on kill generate orbs
        {
            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive)
                    {
                        if (npc.Brain is HarbringerOfFateBrain || npc.Brain is RainOfFireBrain)
                            npc.RemoveFromWorld();
                    }
                }
            }
            BroadcastMessage(String.Format("Apocalypse shouts, 'Your end is at hand!'"));

            AwardEpicEncounterKillPoint();
           
            ApocalypseBrain.StartedApoc = false;
            ApocInitializator.start_respawn_check = false;
            ApocUP = false;
            base.Die(killer);
        }      
        
        protected int AwardEpicEncounterKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.KillsEpicBoss++;
                player.Achieve(AchievementUtils.AchievementNames.Epic_Boss_Kills);
                count++;
            }
            return count;
        }

        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 843, 82, 32);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;

            Model = 857;
            MeleeDamageType = EDamageType.Slash;
            Name = "Apocalypse";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 120;
            Level = 87;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157955);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

            ApocalypseBrain.spawn_harbringers = false;
            ApocalypseBrain.spawn_rain_of_fire = false;
            ApocalypseBrain.apoc_fly_phase = false;
            ApocalypseBrain.IsInFlyPhase = false;
            ApocalypseBrain.fly_phase1 = false;
            ApocalypseBrain.fly_phase2 = false;
            ApocalypseBrain.ApocAggro = false;
            ApocalypseBrain.pop_harbringers = false;
            ApocalypseBrain.StartedApoc = false;
            HarbringerOfFate.HarbringersCount = 0;
            ApocUP = true;


            foreach (GamePlayer player in ClientService.GetPlayersOfRegion(CurrentRegion))
                player.Out.SendSoundEffect(2452, 0, 0, 0, 0, 0);//play sound effect for every player in boss currentregion

            KilledEnemys = 0;
            ApocalypseBrain adds = new ApocalypseBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }

        public static int KilledEnemys = 0;
        public override void EnemyKilled(GameLiving enemy)
        {
            if(enemy is GamePlayer)
            {
                ++KilledEnemys;
            }
            base.EnemyKilled(enemy);
        }
        public override void StartAttack(GameObject target)
        {
            if (ApocalypseBrain.IsInFlyPhase)
                return;
            else
                base.StartAttack(target);
        }
    }
}
namespace DOL.AI.Brain
{
    public class ApocalypseBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ApocalypseBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1300;
            ThinkInterval = 2000;
        }
        public static bool spawn_rain_of_fire = false;
        public static bool apoc_fly_phase = false;
        public static bool IsInFlyPhase = false;
        public static bool fly_phase1 = false;
        public static bool fly_phase2 = false;
        public static bool ApocAggro = false;
        public static bool pop_harbringers = false;
        public static bool StartedApoc = false;
        private bool RemoveAdds = false;

        public override void Think()
        {
            #region Reset boss
            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
                spawn_rain_of_fire = false;
                spawn_harbringers = false;

                apoc_fly_phase = false;
                IsInFlyPhase = false;
                fly_phase1 = false;
                fly_phase2 = false;
                ApocAggro = false;
                pop_harbringers = false;
                StartedApoc = false;
                Apocalypse.KilledEnemys = 0;
                HarbringerOfFate.HarbringersCount = 0;

                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive)
                            {
                                if (npc.Brain is HarbringerOfFateBrain || npc.Brain is RainOfFireBrain)
                                    npc.RemoveFromWorld();
                            }
                        }
                    }
                    RemoveAdds = true;
                }
                //ClearAggroList();
            }
            #endregion
            #region Boss combat
            if (Body.IsAlive)//bring mobs from rooms if mobs got set PackageID="ApocBaf"
            {
                StartedApoc = true;
                if (HasAggro && Body.TargetObject != null)
                    RemoveAdds = false;

                if (ApocAggro == false && Body.HealthPercent <=99)//1st time apoc fly to celling
                {
                    Point3D point1 = new Point3D();
                    point1.X = Body.SpawnPoint.X; point1.Y = Body.SpawnPoint.Y + 100; point1.Z = Body.SpawnPoint.Z + 750;
                    ClearAggroList();
                    if (!Body.IsWithinRadius(point1, 100))
                    {
                        Body.WalkTo(point1, 200);
                        IsInFlyPhase = true;
                    }
                    else
                    {
                        if (fly_phase2 == false)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(FlyPhaseStart), 500);
                            fly_phase2 = true;
                            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                            {
                                if (player != null)
                                    player.Out.SendMessage("Apocalypse says, 'Is it power? Fame? Fortune? Perhaps it is all three.'", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                            }
                            ApocAggro = true;
                        }
                    }
                }

                if (spawn_rain_of_fire==false)
                {
                    SpawnRainOfFire();
                    spawn_rain_of_fire = true;
                }
                if (HarbringerOfFate.HarbringersCount == 0)
                {
                    if (spawn_harbringers == false)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnHarbringers), 500);
                        foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                        {
                            if (npc != null)
                            {
                                if (npc.IsAlive && npc.PackageID == "ApocBaf" && npc.Brain is HarbringerOfFateBrain)
                                    AddAggroListTo(npc.Brain as HarbringerOfFateBrain);// add to aggro mobs with ApocBaf PackageID
                            }
                        }
                        spawn_harbringers = true;
                    }
                }
                if(Body.HealthPercent <= 50 && fly_phase1==false)//2nd time apoc fly to celling
                {
                    Point3D point1 = new Point3D();
                    point1.X = Body.SpawnPoint.X; point1.Y = Body.SpawnPoint.Y+100; point1.Z = Body.SpawnPoint.Z + 750;
                    ClearAggroList();
                    if (!Body.IsWithinRadius(point1, 100))
                    {
                        Body.WalkTo(point1, 200);
                        IsInFlyPhase = true;                       
                    }
                    else
                    {
                        if(fly_phase1 == false)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(FlyPhaseStart), 500);
                            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                            {
                                if (player != null)
                                    player.Out.SendMessage("Apocalypse says, 'I wonder, also, about the motivation that drives one to such an audacious move.'", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                            }
                           fly_phase1 = true;
                        }
                    }
                }
                if (apoc_fly_phase == true)//here cast rain of fire from celling for 30s
                {
                    foreach(GamePlayer player in Body.GetPlayersInRadius(1800))
                    {
                        if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !AggroTable.ContainsKey(player))
                            AggroTable.Add(player, 200);
                    }
                    Body.SetGroundTarget(Body.X, Body.Y, Body.Z - 750);
                    if (!Body.IsCasting)
                    {
                        //Body.TurnTo(Body.GroundTarget.X, Body.GroundTarget.Y);
                        Body.CastSpell(Apoc_Gtaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                    }
                }
            }
            #endregion
            base.Think();
        }
        #region Boss fly phase timers
        public int FlyPhaseStart(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                apoc_fly_phase = true;
                Body.MaxSpeedBase = 0;//make sure it will not move until phase ends
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(FlyPhaseDuration), 30000);//30s duration of phase
            }
            return 0;
        }
        public int FlyPhaseDuration(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                AttackMostWanted();
                IsInFlyPhase = false;
                apoc_fly_phase = false;
                Body.MaxSpeedBase = 300;
            }
            return 0;
        }
        #endregion

        #region Spawn Harbringers
        public static bool spawn_harbringers = false;
        public int SpawnHarbringers(ECSGameTimer timer)
        {
            if (Apocalypse.KilledEnemys == 4)//he doint it only once, spawning 2 harbringers is killed 4 players
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        player.Out.SendMessage("Apocalypse says, 'One has to wonder what kind of power lay behind that feat, for my harbingers of fate were no small adversaries.'", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    HarbringerOfFate Add = new HarbringerOfFate();
                    Add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
                    Add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
                    Add.Z = Body.SpawnPoint.Z;
                    Add.CurrentRegionID = Body.CurrentRegionID;
                    Add.Heading = Body.Heading;
                    Add.RespawnInterval = -1;
                    Add.PackageID = "ApocBaf";
                    Add.AddToWorld();
                    ++HarbringerOfFate.HarbringersCount;
                }
            }
            if(Body.HealthPercent <= 50 && pop_harbringers==false)/// spawning another 2 harbringers if boss is at 50%
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        player.Out.SendMessage("Apocalypse says, 'In all of this, however, it would seem that you have overlooked " +
                            "the small matter of price for your actions. I am not the vengeful sort, so do not take this the wrong way," +
                            " but good harbingers are hard to come by. And, thanks to you, they will need to be replaced.'", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    HarbringerOfFate Add = new HarbringerOfFate();
                    Add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
                    Add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
                    Add.Z = Body.SpawnPoint.Z;
                    Add.CurrentRegionID = Body.CurrentRegionID;
                    Add.Heading = Body.Heading;
                    Add.RespawnInterval = -1;
                    Add.PackageID = "ApocBaf";
                    Add.AddToWorld();
                    ++HarbringerOfFate.HarbringersCount;
                }
                pop_harbringers = true;
            }
            spawn_harbringers = false;
            return 0;
        }
        #endregion
        #region Spawn Rain of Fire mob
        public void SpawnRainOfFire()
        {
            RainOfFire Add = new RainOfFire();
            Add.X = Body.X;
            Add.Y = Body.Y;
            Add.Z = Body.Z + 940;
            Add.CurrentRegionID = Body.CurrentRegionID;
            Add.Heading = Body.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "RainOfFire";
            Add.AddToWorld();
        }
        #endregion
        #region Apoc gtaoe spell for fly phases
        private Spell m_Apoc_Gtaoe;
        private Spell Apoc_Gtaoe
        {
            get
            {
                if (m_Apoc_Gtaoe == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 368;
                    spell.Icon = 368;
                    spell.Damage = 650;
                    spell.Name = "Rain of Fire";
                    spell.Radius = 800;
                    spell.Range = 2800;
                    spell.SpellID = 11740;
                    spell.Target = "Area";
                    spell.Type = "DirectDamageNoVariance";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Heat;
                    m_Apoc_Gtaoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Apoc_Gtaoe);
                }
                return m_Apoc_Gtaoe;
            }
        }
        #endregion
    }
}
////////////////////////////////////////////////////////////Apoc Adds Harbringer of Fate///////////////////////////////////////////////
#region Harbringer of Fate
namespace DOL.GS
{
    public class HarbringerOfFate : GameEpicNPC
    {
        public HarbringerOfFate() : base() { }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 90;
        }
        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 30000; }
        }
        public override void Die(GameObject killer)
        {
            base.Die(killer);
        }

        public override void AutoSetStats(DbMob dbMob = null)
        {
            if (this.PackageID == "ApocBaf")
                return;
            base.AutoSetStats(dbMob);
        }
        public static int HarbringersCount = 0;
        public override short Quickness { get => base.Quickness; set => base.Quickness = 50; }
        public override short Strength { get => base.Strength; set => base.Strength = 200; }
        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 6, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Slash;

            Name = "Harbringer of Fate";
            RespawnInterval = -1;
            Model = 952;
            Size = 90;
            ParryChance = 25;

            this.AbilityBonus[(int)EProperty.Resist_Body] = 25;
            this.AbilityBonus[(int)EProperty.Resist_Heat] = 25;
            this.AbilityBonus[(int)EProperty.Resist_Cold] = 25;
            this.AbilityBonus[(int)EProperty.Resist_Matter] = 25;
            this.AbilityBonus[(int)EProperty.Resist_Energy] = 26;
            this.AbilityBonus[(int)EProperty.Resist_Spirit] = 25;
            this.AbilityBonus[(int)EProperty.Resist_Slash] = 30;
            this.AbilityBonus[(int)EProperty.Resist_Crush] = 30;
            this.AbilityBonus[(int)EProperty.Resist_Thrust] = 30;

            MaxDistance = 2500;
            TetherRange = 3000;
            MaxSpeedBase = 220;
            Level = 75;
            PackageID = "ApocBaf";

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 6;
            Realm = ERealm.None;

            HarbringerOfFateBrain adds = new HarbringerOfFateBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class HarbringerOfFateBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public HarbringerOfFateBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1200;
        }
        public override void Think()
        {
            if (Body.InCombat || HasAggro)
            {
            }
            base.Think();
        }
    }
}
#endregion
/// <summary>
/// ///////////////////////////////////////////////////Rain of Fire////////////////////////////////////////////////////////////////////////
/// </summary>
#region Rain of Fire
namespace DOL.GS
{
    public class RainOfFire : GameEpicNPC
    {
        public RainOfFire() : base() { }
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 99;
                case EDamageType.Thrust: return 99;
                case EDamageType.Crush: return 99;
                default: return 99;
            }
        }
        public override void EnemyKilled(GameLiving enemy)
        {
            if (enemy is GamePlayer)
            {
                ++Apocalypse.KilledEnemys;
            }
            base.EnemyKilled(enemy);
        }
        public override int MaxHealth
        {
            get { return 10000 * this.Constitution / 100; }
        }
        public override void Die(GameObject killer)
        {
            base.Die(killer);
        }
        public override void AutoSetStats(DbMob dbMob = null)
        {
            if (this.PackageID == "RainOfFire")
                return;
            base.AutoSetStats(dbMob);
        }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 300; }
        public override bool AddToWorld()
        {
            Name = "Rain of Fire";
            RespawnInterval = -1;
            Model = 665;
            PackageID = "RainOfFire";

            Flags ^= eFlags.FLYING;
            Flags ^= eFlags.CANTTARGET;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.STATUE;

            Strength = 450;
            Constitution = 100;
            Quickness = 125;
            Piety = 350;
            Intelligence = 350;
            Charisma = 350;
            Empathy = 350;
            MaxSpeedBase = 0;//mob does not move
            Level = 75;

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 8;
            Realm = ERealm.None;

            RainOfFireBrain adds = new RainOfFireBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class RainOfFireBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public RainOfFireBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 0;
        }
        public override void Think()
        {
            if (Body.IsAlive)
            {
                PickTarget();
            }
            base.Think();
        }
        List<GamePlayer> ApocPlayerList = new List<GamePlayer>();
        public GamePlayer dd_Target = null;
        public GamePlayer DD_Target
        {
            get { return dd_Target; }
            set { dd_Target = value; }
        }
        public static bool cast_dd = false;
        public static bool reset_cast = false;
        public void PickTarget()
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive)
                    {
                        if (!ApocPlayerList.Contains(player))
                        {
                            ApocPlayerList.Add(player);
                        }
                    }
                }
            }
            if (ApocPlayerList.Count > 0)
            {
                if (cast_dd == false)
                {
                    GamePlayer ptarget = ((GamePlayer)(ApocPlayerList[Util.Random(1, ApocPlayerList.Count) - 1]));
                    DD_Target = ptarget;
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDD), 6000);
                    cast_dd = true;
                    reset_cast = false;
                }
            }
        }
        private int CastDD(ECSGameTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = DD_Target;
            Body.TurnTo(DD_Target);
            if (Body.TargetObject != null)
            {
                Body.CastSpell(Apoc_Rain_of_Fire, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
                if (reset_cast == false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetCast), 25000);//25s recast
                    reset_cast = true;
                }
            }
            DD_Target = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }
        public int ResetCast(ECSGameTimer timer)
        {
            cast_dd = false;
            return 0;
        }

        private Spell m_Apoc_Rain_of_Fire;
        private Spell Apoc_Rain_of_Fire
        {
            get
            {
                if (m_Apoc_Rain_of_Fire == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 360;
                    spell.Icon = 360;
                    spell.Damage = 800;
                    spell.Name = "Rain of Fire";
                    spell.Radius = 600;
                    spell.Range = 2800;
                    spell.SpellID = 11738;
                    spell.Target = "Enemy";
                    spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)EDamageType.Heat;
                    m_Apoc_Rain_of_Fire = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Apoc_Rain_of_Fire);
                }
                return m_Apoc_Rain_of_Fire;
            }
        }
    }
}
#endregion
#endregion
