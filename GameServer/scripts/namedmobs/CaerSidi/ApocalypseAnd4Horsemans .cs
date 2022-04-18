using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;
using Timer = System.Timers.Timer;
using System.Timers;

#region Apoc Initializator
namespace DOL.GS
{
    public class ApocInitializator : GameNPC
    {
        public ApocInitializator() : base() { }
        public static int HorsemanCount = 4;
        public static int ApocCount = 1;
        public static bool horseman2 = false;
        public static bool horseman3 = false;
        public static bool horseman4 = false;
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
                if(HorsemanCount == 3 && horseman2==false)
                {
                    BroadcastMessage(String.Format("Bellum says, 'Prepare yourselves for war. One minute, you are granted.'"));
                    new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnHorsemanBellum), 60000);//60s before starting
                    horseman2 = true;
                }
                if(HorsemanCount == 2 && horseman2 == true && horseman3==false)
                {
                    BroadcastMessage(String.Format("Morbus says, 'Sometimes it is the smallest things that are the most deadly. Be prepared in one minute..'"));
                    new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnHorsemanMorbus), 60000);//60s before starting
                    horseman3 = true;
                }
                if (HorsemanCount == 1 && horseman2 == true && horseman3 == true && horseman4 == false)
                {
                    BroadcastMessage(String.Format("Funus says, 'Prepare to die. Sixty seconds you are given to arrange for the event.'\n" +
                    "For a brief moment, the clerics in the area glow softly as if bathed in a divine light, and their eyes shine as if a sudden" +
                    " rush of energy now courses through them. A faint whisper in your mind warns you that mundane attacks on this creature of death" +
                    " would have little effect or even make the situation worse, but it also reassures you that the clerics, a direct conduit between the " +
                    "divine and this world, posses an unexpected advantage over the creature, Funus."));
                    new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnHorsemanFunus), 60000);//60s before starting
                    horseman4 = true;
                }
                if (HorsemanCount == 0 && horseman2 == true && horseman3 == true && horseman4 == true && spawn_apoc==false)
                {
                    BroadcastMessage(String.Format("A thunderous voice echoes off the walls, 'Well done. You have succeeded in besting my harbingers.'"));
                    new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnApoc), 5000);
                    spawn_apoc = true;
                }
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
            start_respawn_check = false;
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

        #region Spawn Boss Timers
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
            return 0;
        }
        public int SpawnHorsemanBellum(ECSGameTimer timer)
        {
            Bellum Add = new Bellum();
            Add.X = X; 
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 4072;
            Add.AddToWorld();
            return 0;
        }
        public int SpawnHorsemanMorbus(ECSGameTimer timer)
        {
            Morbus Add = new Morbus();
            Add.X = X;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 4072;
            Add.AddToWorld();
            return 0;
        }
        public int SpawnHorsemanFunus(ECSGameTimer timer)
        {
            Funus Add = new Funus();//inside controller
            Add.X = X;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 4072;
            Add.AddToWorld();
            return 0;
        }
        public int SpawnApoc(ECSGameTimer timer)
        {
            Apocalypse Add = new Apocalypse();
            Add.X = X;
            Add.Y = Y;
            Add.Z = Z;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = 4072;
            Add.AddToWorld();
            return 0;
        }
        #endregion

        #region Pick Random Player, PlayerEnter
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
                        {
                            PlayersInRoom.Add(player);
                        }
                    }
                }
            }

            if (PickedTarget == false && PlayersInRoom.Count > 0)
            {
                GamePlayer ptarget = ((GamePlayer)(PlayersInRoom[Util.Random(1, PlayersInRoom.Count) - 1]));
                RandomTarget = ptarget;

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
                if (ApocCount == 0 && HorsemanCount == 0 && start_respawn_check == false && horseman2 == true && horseman3 == true && horseman4 == true)
                {
                    RandomTarget = null;//reset picked player
                    horseman2 = false;
                    horseman3 = false;
                    horseman4 = false;
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
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Apoc Initializator", 60, (eRealm)0);
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
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 80; // dmg reduction for melee dmg
                case eDamageType.Crush: return 80; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 80; // dmg reduction for melee dmg
                default: return 65; // dmg reduction for rest resists
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
            return true;
        }      
        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str)) return false;
            if (!(source is GamePlayer)) return false;
            GamePlayer t = (GamePlayer)source;
            if (CanInteract == false)
            {
                if (t == ApocInitializator.RandomTarget)
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
        public override double AttackDamage(InventoryItem weapon)
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
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 850;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override void Die(GameObject killer)//on kill generate orbs
        {

            --ApocInitializator.HorsemanCount;
            FamesBrain.StartedFames = false;
            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            Model = 938;
            MeleeDamageType = eDamageType.Body;
            Name = "Fames";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 120;
            Level = 83;
            MaxSpeedBase = 300;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160695);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = eRealm.None;
            FamesBrain.spawn_fate = false;
            CanInteract = false;
            FamesBrain.StartedFames = false;


            Strength = 5;
            Empathy = 325;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 220;
            Intelligence = 220;          

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
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                BafMobs = false;
                StartedFames = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.InCombat || HasAggro || Body.AttackState == true)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
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
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double AttackDamage(InventoryItem weapon)
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 850;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override void Die(GameObject killer)//on kill generate orbs
        {

            foreach(GameNPC npc in GetNPCsInRadius(4000))
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
            
            --ApocInitializator.HorsemanCount;
            BellumBrain.StartedBellum = false;
            spawn_fate2 = false;
            base.Die(killer);
        }
        public static bool spawn_fate2 = false;
        public void SpawnFateBearer()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
            GameEpicNPC Add = new GameEpicNPC();
            Add.LoadTemplate(npcTemplate);
            Add.X = this.X - 100;
            Add.Y = this.Y;
            Add.Z = this.Z;
            Add.CurrentRegionID = this.CurrentRegionID;
            Add.Heading = this.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "BellumBaf";
            Add.AddToWorld();
        }

        public override bool AddToWorld()
        {
            Model = 927;
            MeleeDamageType = eDamageType.Body;
            Name = "Bellum";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 140;
            Level = 83;
            MaxSpeedBase = 300;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158353);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = eRealm.None;
            BellumBrain.StartedBellum = false;
            BellumBrain.SpawnWeapons = false;

            this.AbilityBonus[(int)eProperty.Resist_Body] = -10;
            this.AbilityBonus[(int)eProperty.Resist_Heat] = -10;
            this.AbilityBonus[(int)eProperty.Resist_Cold] = -10;
            this.AbilityBonus[(int)eProperty.Resist_Matter] = -10;
            this.AbilityBonus[(int)eProperty.Resist_Energy] = -10;
            this.AbilityBonus[(int)eProperty.Resist_Spirit] = -10;
            this.AbilityBonus[(int)eProperty.Resist_Slash] = 99;
            this.AbilityBonus[(int)eProperty.Resist_Crush] = 99;
            this.AbilityBonus[(int)eProperty.Resist_Thrust] = 99;

            Strength = 5;
            Empathy = 325;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 220;
            Intelligence = 220;
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
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                StartedBellum = false;
                SpawnWeapons = false;
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
            }
            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.InCombat && HasAggro)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
            {
                StartedBellum = true;
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
            Add.MeleeDamageType = eDamageType.Crush;
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
            Add.MeleeDamageType = eDamageType.Slash;
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
            Add.MeleeDamageType = eDamageType.Thrust;
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
        public bool Master = true;
        public GameNPC Master_NPC;
        public List<GameNPC> CopyNPC;
        public WarIncarnateCrush() : base() { }
        public WarIncarnateCrush(bool master)
        {
            Master = master;
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {

            if (!Master && Master_NPC != null)
                Master_NPC.TakeDamage(source, damageType, damageAmount, criticalAmount);
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                int damageDealt = damageAmount + criticalAmount;

                if (CopyNPC != null && CopyNPC.Count > 0)
                {
                    lock (CopyNPC)
                    {
                        foreach (GameNPC npc in CopyNPC)
                        {
                            if (npc == null) break;
                            npc.Health = Health;//they share same healthpool
                        }
                    }
                }
            }
        }
        public override void Die(GameObject killer)
        {
            if (!(killer is WarIncarnateCrush) && !Master && Master_NPC != null)
                Master_NPC.Die(killer);
            else
            {

                if (CopyNPC != null && CopyNPC.Count > 0)
                {
                    lock (CopyNPC)
                    {
                        foreach (GameNPC npc in CopyNPC)
                        {
                            if (npc.IsAlive)
                                npc.Die(this);//if one die all others aswell
                        }
                    }
                }
                CopyNPC = new List<GameNPC>();

                
                base.Die(killer);
            }
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
                        template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 17, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;

                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 70, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 12, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.Standard);
                        VisibleActiveWeaponSlots = 10;
                    }
                    break;
            }          
            Model = 665;
            Name = "war incarnate";
            MeleeDamageType = eDamageType.Crush;
            RespawnInterval = -1;
            MaxSpeedBase = 210;
            Strength = 150;
            Piety = 250;

            Size = 100;
            Level = 75;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Realm = eRealm.None;
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
                            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 17, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 2:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 70, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 3:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 12, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.Standard);
                            Add.VisibleActiveWeaponSlots = 10;
                        }
                        break;
                }
                Add.X = Body.X + Util.Random(-200, 200);
                Add.Y = Body.Y + Util.Random(-200, 200);
                Add.Z = Body.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.MeleeDamageType = eDamageType.Crush;
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

                Add.Master_NPC = Body;
                Add.Master = false;
                if (Body is WarIncarnateCrush)
                {
                    WarIncarnateCrush wi = Body as WarIncarnateCrush;
                    wi.CopyNPC.Add(Add);
                }
            }
        }
        public static bool spawn_copies = false;
        public override void Think()
        {
            if (!(Body is WarIncarnateCrush))
            {
                base.Think();
                return;
            }
            WarIncarnateCrush sg = Body as WarIncarnateCrush;

            if (sg.CopyNPC == null || sg.CopyNPC.Count == 0)
                sg.CopyNPC = new List<GameNPC>();

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
        public bool Master2 = true;
        public GameNPC Master_NPC2;
        public List<GameNPC> CopyNPC2;
        public WarIncarnateSlash() : base() { }
        public WarIncarnateSlash(bool master2)
        {
            Master2 = master2;
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {

            if (!Master2 && Master_NPC2 != null)
                Master_NPC2.TakeDamage(source, damageType, damageAmount, criticalAmount);
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                int damageDealt = damageAmount + criticalAmount;

                if (CopyNPC2 != null && CopyNPC2.Count > 0)
                {
                    lock (CopyNPC2)
                    {
                        foreach (GameNPC npc in CopyNPC2)
                        {
                            if (npc == null) break;
                            npc.Health = Health;//they share same healthpool
                        }
                    }
                }
            }
        }
        public override void Die(GameObject killer)
        {
            if (!(killer is WarIncarnateSlash) && !Master2 && Master_NPC2 != null)
                Master_NPC2.Die(killer);
            else
            {

                if (CopyNPC2 != null && CopyNPC2.Count > 0)
                {
                    lock (CopyNPC2)
                    {
                        foreach (GameNPC npc in CopyNPC2)
                        {
                            if (npc.IsAlive)
                                npc.Die(this);//if one die all others aswell
                        }
                    }
                }
                CopyNPC2 = new List<GameNPC>();

                
                base.Die(killer);
            }
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
                        template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 6, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;

                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 73, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 67, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 4:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 4, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.Standard);
                        VisibleActiveWeaponSlots = 10;
                    }
                    break;
            }
            Model = 665;
            Name = "war incarnate";
            MeleeDamageType = eDamageType.Slash;
            RespawnInterval = -1;
            MaxSpeedBase = 210;
            Strength = 150;
            Piety = 250;

            Size = 100;
            Level = 75;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Realm = eRealm.None;
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
                            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 6, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 2:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 73, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 3:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 67, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 4:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 4, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.Standard);
                            Add.VisibleActiveWeaponSlots = 10;
                        }
                        break;
                }
                Add.X = Body.X + Util.Random(-200, 200);
                Add.Y = Body.Y + Util.Random(-200, 200);
                Add.Z = Body.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.MeleeDamageType = eDamageType.Slash;
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

                Add.Master_NPC2 = Body;
                Add.Master2 = false;
                if (Body is WarIncarnateSlash)
                {
                    WarIncarnateSlash wi = Body as WarIncarnateSlash;
                    wi.CopyNPC2.Add(Add);
                }
            }
        }
        public static bool spawn_copies2 = false;
        public override void Think()
        {
            if (!(Body is WarIncarnateSlash))
            {
                base.Think();
                return;
            }
            WarIncarnateSlash sg = Body as WarIncarnateSlash;

            if (sg.CopyNPC2 == null || sg.CopyNPC2.Count == 0)
                sg.CopyNPC2 = new List<GameNPC>();

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
        public bool Master3 = true;
        public GameNPC Master_NPC3;
        public List<GameNPC> CopyNPC3;
        public WarIncarnateThrust() : base() { }
        public WarIncarnateThrust(bool master3)
        {
            Master3 = master3;
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (!Master3 && Master_NPC3 != null)
                Master_NPC3.TakeDamage(source, damageType, damageAmount, criticalAmount);
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                int damageDealt = damageAmount + criticalAmount;

                if (CopyNPC3 != null && CopyNPC3.Count > 0)
                {
                    lock (CopyNPC3)
                    {
                        foreach (GameNPC npc in CopyNPC3)
                        {
                            if (npc == null) break;
                            npc.Health = Health;//they share same healthpool
                        }
                    }
                }
            }
        }
        public override void Die(GameObject killer)
        {
            if (!(killer is WarIncarnateThrust) && !Master3 && Master_NPC3 != null)
                Master_NPC3.Die(killer);
            else
            {

                if (CopyNPC3 != null && CopyNPC3.Count > 0)
                {
                    lock (CopyNPC3)
                    {
                        foreach (GameNPC npc in CopyNPC3)
                        {
                            if (npc.IsAlive)
                                npc.Die(this);//if one die all others aswell
                        }
                    }
                }
                CopyNPC3 = new List<GameNPC>();

                
                base.Die(killer);
            }
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
                        template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 69, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;

                    }
                    break;
                case 2:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 846, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                        VisibleActiveWeaponSlots = 34;
                    }
                    break;
                case 3:
                    {
                        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                        template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 886, 0, 0);
                        Inventory = template.CloseTemplate();
                        SwitchWeapon(eActiveWeaponSlot.Standard);
                        VisibleActiveWeaponSlots = 255;
                    }
                    break;
            }
            Model = 665;
            Name = "war incarnate";
            MeleeDamageType = eDamageType.Thrust;
            RespawnInterval = -1;
            MaxSpeedBase = 210;
            Strength = 150;
            Piety = 250;

            Size = 100;
            Level = 75;
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            Realm = eRealm.None;
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
                            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 69, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;

                        }
                        break;
                    case 2:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 846, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            Add.VisibleActiveWeaponSlots = 34;
                        }
                        break;
                    case 3:
                        {
                            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                            template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 886, 0, 0);
                            Add.Inventory = template.CloseTemplate();
                            Add.SwitchWeapon(eActiveWeaponSlot.Standard);
                            Add.VisibleActiveWeaponSlots = 255;
                        }
                        break;
                }
                Add.X = Body.X + Util.Random(-200, 200);
                Add.Y = Body.Y + Util.Random(-200, 200);
                Add.Z = Body.Z;
                Add.CurrentRegionID = Body.CurrentRegionID;
                Add.MeleeDamageType = eDamageType.Thrust;
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

                Add.Master_NPC3 = Body;
                Add.Master3 = false;
                if (Body is WarIncarnateThrust)
                {
                    WarIncarnateThrust wi = Body as WarIncarnateThrust;
                    wi.CopyNPC3.Add(Add);
                }
            }
        }
        public static bool spawn_copies3 = false;
        public override void Think()
        {
            if (!(Body is WarIncarnateThrust))
            {
                base.Think();
                return;
            }
            WarIncarnateThrust sg = Body as WarIncarnateThrust;

            if (sg.CopyNPC3 == null || sg.CopyNPC3.Count == 0)
                sg.CopyNPC3 = new List<GameNPC>();

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
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 80; // dmg reduction for melee dmg
                case eDamageType.Crush: return 80; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 80; // dmg reduction for melee dmg
                default: return 65; // dmg reduction for rest resists
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override void WalkToSpawn()
        {
            if (MorbusBrain.IsBug)
                return;
            base.WalkToSpawn();
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (Morbus_Swarm_count > 0)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
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
        public override double AttackDamage(InventoryItem weapon)
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 850;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override void Die(GameObject killer)//on kill generate orbs
        {
            --ApocInitializator.HorsemanCount;
            MorbusBrain.StartedMorbus = false;
            spawn_fate3 = false;
            base.Die(killer);
        }
        public static bool spawn_fate3 = false;
        public static int Morbus_Swarm_count = 0;
        public void SpawnFateBearer()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
            GameEpicNPC Add = new GameEpicNPC();
            Add.LoadTemplate(npcTemplate);
            Add.X = this.X - 100;
            Add.Y = this.Y;
            Add.Z = this.Z;
            Add.CurrentRegionID = this.CurrentRegionID;
            Add.Heading = this.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "MorbusBaf";
            Add.AddToWorld();
        }
        public override bool AddToWorld()
        {
            Model = 952;
            MeleeDamageType = eDamageType.Crush;
            Name = "Morbus";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 140;
            Level = 83;
            MaxSpeedBase = 300;
            
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164171);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = eRealm.None;
            MorbusBrain.StartedMorbus = false;
            MorbusBrain.BafMobs3 = false;
            MorbusBrain.spawn_swarm = false;
            MorbusBrain.message_warning1 = false;
            MorbusBrain.IsBug = false;

            this.AbilityBonus[(int)eProperty.Resist_Body] = 26;
            this.AbilityBonus[(int)eProperty.Resist_Heat] = 26;
            this.AbilityBonus[(int)eProperty.Resist_Cold] = -15;//weak to cold
            this.AbilityBonus[(int)eProperty.Resist_Matter] = 26;
            this.AbilityBonus[(int)eProperty.Resist_Energy] = 26;
            this.AbilityBonus[(int)eProperty.Resist_Spirit] = 26;
            this.AbilityBonus[(int)eProperty.Resist_Slash] = 80;
            this.AbilityBonus[(int)eProperty.Resist_Crush] = 80;
            this.AbilityBonus[(int)eProperty.Resist_Thrust] = 80;

            Strength = 5;
            Empathy = 325;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 220;
            Intelligence = 220;
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
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                StartedMorbus = false;
                BafMobs3 = false;
                message_warning1 = false;
                foreach(GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if(npc != null)
                    {
                        if(npc.IsAlive)
                        {
                            if(npc.PackageID =="MorbusBaf" && npc.Brain is MorbusSwarmBrain)
                            {
                                npc.RemoveFromWorld();
                                Morbus.Morbus_Swarm_count = 0;
                            }
                        }
                    }
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
            if (Body.InCombat || HasAggro || Body.AttackState == true)
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
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnSwarm), Util.Random(40000, 60000));//40s-60s

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
                for (int i = 0; i < 4; i++)
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
        public override double AttackDamage(InventoryItem weapon)
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.40;
        }
        public override int MaxHealth
        {
            get { return 8000; }
        }
        public override void Die(GameObject killer)
        {
            --Morbus.Morbus_Swarm_count;
            base.Die(killer);
        }

        public override void AutoSetStats(Mob dbMob = null)
        {
            if (this.PackageID == "MorbusBaf")
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
                        this.Strength = 35;
                        this.Dexterity = 100;
                        this.Quickness = 80;
                        Size = (byte)Util.Random(20, 45);
                        MaxSpeedBase = 185;
                        MeleeDamageType = eDamageType.Crush;
                    }
                    break;
                case 2:
                    {
                        Model = 567;//rat dps
                        Constitution = 100;
                        this.Strength = 55;
                        this.Dexterity = 100;
                        this.Quickness = 100;
                        Size = (byte)Util.Random(20, 30);
                        MaxSpeedBase = 200;
                        MeleeDamageType = eDamageType.Slash;
                    }
                    break;
                case 3:
                    {
                        Model = 771;//roach tanky+dps
                        Constitution = 200;
                        this.Strength = 80;
                        this.Dexterity = 100;
                        this.Quickness = 65;
                        Size = (byte)Util.Random(20, 30);
                        MaxSpeedBase = 165;
                        MeleeDamageType = eDamageType.Crush;
                    }
                    break;
                case 4:
                    {
                        Model = 824;//cicada quick attacks
                        Size = (byte)Util.Random(20, 30);
                        Constitution = 100;
                        this.Strength = 25;
                        this.Dexterity = 100;
                        this.Quickness = 200;
                        MaxSpeedBase = 220;
                        MeleeDamageType = eDamageType.Thrust;
                    }
                    break;
                case 5:
                    {
                        Model = 819;//dragonfly quick attacks
                        Size = (byte)Util.Random(20, 30);
                        Constitution = 100;
                        this.Strength = 25;
                        this.Dexterity = 100;
                        this.Quickness = 200;
                        MaxSpeedBase = 220;
                        MeleeDamageType = eDamageType.Thrust;
                    }
                    break;
            }
            MaxDistance = 2500;
            TetherRange = 3000;
            Level = 75;

            this.AbilityBonus[(int)eProperty.Resist_Body] = 15;
            this.AbilityBonus[(int)eProperty.Resist_Heat] = 15;
            this.AbilityBonus[(int)eProperty.Resist_Cold] = -15;//weak to cold
            this.AbilityBonus[(int)eProperty.Resist_Matter] = 15;
            this.AbilityBonus[(int)eProperty.Resist_Energy] = 15;
            this.AbilityBonus[(int)eProperty.Resist_Spirit] = 15;
            this.AbilityBonus[(int)eProperty.Resist_Slash] = 25;
            this.AbilityBonus[(int)eProperty.Resist_Crush] = 25;
            this.AbilityBonus[(int)eProperty.Resist_Thrust] = 25;

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 7;
            Realm = eRealm.None;          

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
            if(Body.InCombat || HasAggro)
            {
                if(Util.Chance(15))
                {
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
                    DBSpell spell = new DBSpell();
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
                    spell.DamageType = (int)eDamageType.Body; //Energy DMG Type
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
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                GamePlayer truc = source as GamePlayer;
                //cleri,merc,arms,infi,scout
                if (truc.CharacterClass.ID == 6 || (truc.CharacterClass.ID == 11 && truc.AttackWeapon.Object_Type == 5) || (truc.CharacterClass.ID == 2 && truc.AttackWeapon.Object_Type == 10) 
                    || (truc.CharacterClass.ID == 9 && truc.AttackWeapon.Object_Type == 10) || (truc.CharacterClass.ID == 3 && truc.AttackWeapon.Object_Type == 9))
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
                else
                {
                    truc.Out.SendMessage(Name + " absorbs all your damage to heal iself!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    this.Health += this.MaxHealth;
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            if(source is GamePet)
            {
                GamePet truc = source as GamePet;
                GamePlayer pet_owner = truc.Owner as GamePlayer;
                pet_owner.Out.SendMessage(Name + " absorbs all your damage to heal iself!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                this.Health += this.MaxHealth;
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
        public override double AttackDamage(InventoryItem weapon)
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
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 850;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public override void Die(GameObject killer)//on kill generate orbs
        {
            --ApocInitializator.HorsemanCount;
            spawn_fate4 = false;
            FunusBrain.StartedFunus = false;
            base.Die(killer);
        }
        public void SpawnFateBearer()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160741);
            GameEpicNPC Add = new GameEpicNPC();
            Add.LoadTemplate(npcTemplate);
            Add.X = this.X - 100;
            Add.Y = this.Y;
            Add.Z = this.Z;
            Add.CurrentRegionID = this.CurrentRegionID;
            Add.Heading = this.Heading;
            Add.RespawnInterval = -1;
            Add.PackageID = "FunusBaf";
            Add.AddToWorld();
        }

        public static bool spawn_fate4 = false;
        public override bool AddToWorld()
        {
            Model = 911;
            MeleeDamageType = eDamageType.Heat;
            Name = "Funus";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 120;
            Level = 83;
            MaxSpeedBase = 300;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161151);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = eRealm.None;
            FunusBrain.StartedFunus = false;
            FunusBrain.BafMobs4 = false;

            this.AbilityBonus[(int)eProperty.Resist_Body] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Heat] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Cold] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Matter] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Energy] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Spirit] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Slash] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Crush] = 55;
            this.AbilityBonus[(int)eProperty.Resist_Thrust] = 55;

            Strength = 5;
            Empathy = 325;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 220;
            Intelligence = 220;
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
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
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
            if (Body.InCombat || HasAggro || Body.AttackState == true)//bring mobs from rooms if mobs got set PackageID="FamesBaf"
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
            foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override double AttackDamage(InventoryItem weapon)
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
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 900;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.60;
        }
        public override int MaxHealth
        {
            get { return 40000; }
        }
        public override void Die(GameObject killer)//on kill generate orbs
        {
            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive)
                    {
                        if (npc.Brain is HarbringerOfFateBrain || npc.Brain is RainOfFireBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }
            BroadcastMessage(String.Format("Apocalypse shouts, 'Your end is at hand!'"));
            
            --ApocInitializator.ApocCount;
            ApocalypseBrain.StartedApoc = false;
            base.Die(killer);
        }      
        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 843, 82, 32);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;

            Model = 857;
            MeleeDamageType = eDamageType.Slash;
            Name = "Apocalypse";
            RespawnInterval = -1;

            MaxDistance = 3500;
            TetherRange = 3600;
            Size = 120;
            Level = 87;
            MaxSpeedBase = 300;
            ParryChance = 35;
            Flags = eFlags.FLYING;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157955);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 11;
            Realm = eRealm.None;

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

            this.AbilityBonus[(int)eProperty.Resist_Body] = 70;
            this.AbilityBonus[(int)eProperty.Resist_Heat] = 70;
            this.AbilityBonus[(int)eProperty.Resist_Cold] = 70;
            this.AbilityBonus[(int)eProperty.Resist_Matter] = 70;
            this.AbilityBonus[(int)eProperty.Resist_Energy] = 70;
            this.AbilityBonus[(int)eProperty.Resist_Spirit] = 70;
            this.AbilityBonus[(int)eProperty.Resist_Slash] = 80;
            this.AbilityBonus[(int)eProperty.Resist_Crush] = 80;
            this.AbilityBonus[(int)eProperty.Resist_Thrust] = 80;

            foreach (GameClient client in WorldMgr.GetClientsOfRegion(this.CurrentRegionID))
            {
                if (client == null) break;
                if (client.Player == null) continue;
                if (client.IsPlaying)
                {
                    client.Out.SendSoundEffect(2452, 0, 0, 0, 0, 0);//play sound effect for every player in boss currentregion
                }
            }

            Strength = 5;//no need str at all
            Dexterity = 200;
            Constitution = 100;
            Quickness = 10;//slow attacks
            Piety = 350;
            Intelligence = 350;
            Charisma = 350;
            Empathy = 302;//most important stat for boss damage, with this sestings it will hit for 940~
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
        public override void AttackMostWanted()
        {
            if (IsInFlyPhase == true)
                return;
            else
            {
                base.AttackMostWanted();
            }
        }
        public override void OnAttackedByEnemy(AttackData ad)//another check to not attack enemys
        {
            if (IsInFlyPhase == true)
                return;
            else
            {
                base.OnAttackedByEnemy(ad);
            }
        }
        public override void Think()
        {
            #region Reset boss
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                spawn_rain_of_fire = false;
                spawn_harbringers = false;

                apoc_fly_phase = false;
                IsInFlyPhase = false;
                fly_phase1 = false;
                fly_phase2 = false;
                ApocAggro = false;
                pop_harbringers = false;
                StartedApoc = false;
                HarbringerOfFate.HarbringersCount = 0;

                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if(npc != null)
                    {
                        if(npc.IsAlive)
                        {
                            if(npc.Brain is HarbringerOfFateBrain || npc.Brain is RainOfFireBrain)
                            {
                                npc.RemoveFromWorld();
                            }    
                        }
                    }
                }
            }
            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            #endregion
            #region Boss combat
            if (Body.InCombat || HasAggro || Body.AttackState == true)//bring mobs from rooms if mobs got set PackageID="ApocBaf"
            {
                StartedApoc = true;
                if (ApocAggro == false && Body.HealthPercent <=99)//1st time apoc fly to celling
                {
                    Point3D point1 = new Point3D();
                    point1.X = Body.SpawnPoint.X; point1.Y = Body.SpawnPoint.Y + 100; point1.Z = Body.SpawnPoint.Z + 750;
                    Body.StopAttack();
                    if (!Body.IsWithinRadius(point1, 100))
                    {
                        Body.WalkTo(point1, 100);
                        IsInFlyPhase = true;
                    }
                    else
                    {
                        if (fly_phase2 == false)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(FlyPhaseStart), 1000);
                            fly_phase2 = true;
                            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                            {
                                if (player != null)
                                {
                                    player.Out.SendMessage("Apocalypse says, 'Is it power? Fame? Fortune? Perhaps it is all three.'", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                                }
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
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnHarbringers), 2000);

                        foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                        {
                            if (npc != null)
                            {
                                if (npc.IsAlive && npc.PackageID == "ApocBaf" && npc.Brain is HarbringerOfFateBrain)
                                {
                                    AddAggroListTo(npc.Brain as HarbringerOfFateBrain);// add to aggro mobs with ApocBaf PackageID
                                }
                            }
                        }
                        spawn_harbringers = true;
                    }
                }
                if(Body.HealthPercent <= 50 && fly_phase1==false)//2nd time apoc fly to celling
                {
                    Point3D point1 = new Point3D();
                    point1.X = Body.SpawnPoint.X; point1.Y = Body.SpawnPoint.Y+100; point1.Z = Body.SpawnPoint.Z + 750;
                    Body.StopAttack();                      
                    if (!Body.IsWithinRadius(point1, 100))
                    {
                        Body.StopAttack();
                        Body.WalkTo(point1, 70);
                        IsInFlyPhase = true;                       
                    }
                    else
                    {
                        if(fly_phase1 == false)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(FlyPhaseStart), 1000);
                            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                            {
                                if (player != null)
                                {
                                    player.Out.SendMessage("Apocalypse says, 'I wonder, also, about the motivation that drives one to such an audacious move.'", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                                }
                            }
                           fly_phase1 = true;
                        }
                    }
                }
                if (apoc_fly_phase == true)//here cast rain of fire from celling for 30s
                {
                    Body.GroundTarget.X = Body.X;
                    Body.GroundTarget.Y = Body.Y;
                    Body.GroundTarget.Z = Body.Z - 750;
                    Body.CastSpell(Apoc_Gtaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
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
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 4;
                    spell.ClientEffect = 368;
                    spell.Icon = 368;
                    spell.Damage = 750;
                    spell.Name = "Rain of Fire";
                    spell.Radius = 1000;
                    spell.Range = 2800;
                    spell.SpellID = 11740;
                    spell.Target = "Area";
                    spell.Type = "DirectDamageNoVariance";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
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
        public override double AttackDamage(InventoryItem weapon)
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
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 600;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public override void Die(GameObject killer)
        {
            base.Die(killer);
        }

        public override void AutoSetStats(Mob dbMob = null)
        {
            if (this.PackageID == "ApocBaf")
                return;
            base.AutoSetStats(dbMob);
        }
        public static int HarbringersCount = 0;
        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 6, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Slash;

            Name = "Harbringer of Fate";
            RespawnInterval = -1;
            Model = 952;
            Size = 90;

            Strength = 5;
            Constitution = 100;
            Dexterity = 200;
            Quickness = 50;
            ParryChance = 25;

            this.AbilityBonus[(int)eProperty.Resist_Body] = 25;
            this.AbilityBonus[(int)eProperty.Resist_Heat] = 25;
            this.AbilityBonus[(int)eProperty.Resist_Cold] = 25;
            this.AbilityBonus[(int)eProperty.Resist_Matter] = 25;
            this.AbilityBonus[(int)eProperty.Resist_Energy] = 26;
            this.AbilityBonus[(int)eProperty.Resist_Spirit] = 25;
            this.AbilityBonus[(int)eProperty.Resist_Slash] = 30;
            this.AbilityBonus[(int)eProperty.Resist_Crush] = 30;
            this.AbilityBonus[(int)eProperty.Resist_Thrust] = 30;

            MaxDistance = 2500;
            TetherRange = 3000;
            MaxSpeedBase = 220;
            Level = 75;
            PackageID = "ApocBaf";

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 6;
            Realm = eRealm.None;

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
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 99;
                case eDamageType.Thrust: return 99;
                case eDamageType.Crush: return 99;
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
        public override void AutoSetStats(Mob dbMob = null)
        {
            if (this.PackageID == "RainOfFire")
                return;
            base.AutoSetStats(dbMob);
        }
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
            Dexterity = 200;
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
            Realm = eRealm.None;

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
                Body.TargetInView = true;            
                Body.CastSpell(Apoc_Rain_of_Fire, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
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
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 378;
                    spell.Icon = 378;
                    spell.Damage = 750;
                    spell.Name = "Rain of Fire";
                    spell.Radius = 1000;
                    spell.Range = 2800;
                    spell.SpellID = 11738;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamageNoVariance";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
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
