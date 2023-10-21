using System;
using System.Collections.Generic;
using System.Timers;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Apocalypse Initializer
public class ApocalypseInitializer : GameNpc
{
    public ApocalypseInitializer() : base() { }
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
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public int Message_timer(EcsGameTimer timer)
    {         
        BroadcastMessage(String.Format("Fames says loudly, 'I sense presence of many, the presence of power and ambition...'"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer2), 6000);//60s before starting
        return 0;
    }
    public int Message_timer2(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("Morbus says, 'The presence of those who would challenge fate.'"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer3), 6000);//60s before starting
        return 0;
    }
    public int Message_timer3(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("Bellum says, '...challenge the inevitable.'"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer4), 5000);//60s before starting
        return 0;
    }
    public int Message_timer4(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("Morbus says, 'Fate cannot be changed.'"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer5), 5000);//60s before starting
        return 0;
    }
    public int Message_timer5(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("Funus says with a gravely hiss, 'It is the fate of man to die, to expire like the flame of a candle.'"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer6), 8000);//60s before starting
        return 0;
    }
    public int Message_timer6(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("Bellum says, 'It is the fate of man to know pain and loss.'"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer7), 6000);//60s before starting
        return 0;
    }
    public int Message_timer7(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("Morbus says, '... and misery.'"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer8), 5000);//60s before starting
        return 0;
    }
    public static bool FamesWaitForText = false;
    public int Message_timer8(EcsGameTimer timer)
    {
        BroadcastMessage(String.Format("Fames asks, 'You, "+RandomTarget.Name+", do you come to challenge fate? Come to me with your answer so that I may see the answer" +
            " in your eyes as well as hear it your voice"));
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnHorsemanFames), 1000);
        return 0;
    }
    #endregion

    #region Spawn Fames Timer
    public int SpawnHorsemanFames(EcsGameTimer timer)
    {
        FamesHorseman Add = new FamesHorseman();
        Add.X = X;
        Add.Y = Y;
        Add.Z = Z;
        Add.CurrentRegion = this.CurrentRegion;
        Add.Flags = ENpcFlags.PEACE;
        Add.Heading = 4072;
        Add.AddToWorld();
        FamesWaitForText = true;
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(OtherPlayersCanInteract), 60000);
        return 0;
    }
    public static bool OthersCanInteract = false;
    private int OtherPlayersCanInteract(EcsGameTimer timer)
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
                new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Message_timer), 4000);
            PickedTarget = true;
        }
    }
    #endregion

    #region Respawn Whole Encounter
    public void StartRespawnEncounter()
    {
        if (IsAlive)
        {
            if (!FamesHorseman.FamesIsUp && !BellumHorseman.BellumUP && !MorbusHorseman.MorbusUP && !FunusHorseman.FunusUp && !Apocalypse.ApocUP && !start_respawn_check)
            {
                RandomTarget = null;//reset picked player
                PlayersInRoom.Clear();
                int time = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000miliseconds 
                new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoRespawnNow), time);
                log.Debug("Starting respawn time for final caer sidi encounter, will respawn in " + time / 60000 + " minutes!");
                start_respawn_check = true;
            }
        }
    }
    public int DoRespawnNow(EcsGameTimer timer)
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
        ApocalypseInitializerBrain hi = new ApocalypseInitializerBrain();
        SetOwnBrain(hi);
        base.AddToWorld();
        return true;
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Apoc Initializator", 60, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Apoc Initializator not found, creating it...");

            log.Warn("Initializing Apoc Initializator...");
            ApocalypseInitializer CO = new ApocalypseInitializer();
            CO.Name = "Apoc Initializator";
            CO.GuildName = "DO NOT REMOVE!";
            CO.RespawnInterval = 5000;
            CO.Model = 665;
            CO.Realm = 0;
            CO.Level = 50;
            CO.Size = 50;
            CO.CurrentRegionID = 60;//caer sidi
            CO.Flags ^= ENpcFlags.CANTTARGET;
            CO.Flags ^= ENpcFlags.FLYING;
            CO.Flags ^= ENpcFlags.DONTSHOWNAME;
            CO.Flags ^= ENpcFlags.PEACE;
            CO.Faction = FactionMgr.GetFactionByID(64);
            CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            CO.X = 29462;
            CO.Y = 25240;
            CO.Z = 19490;
            ApocalypseInitializerBrain ubrain = new ApocalypseInitializerBrain();
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
            if(FamesHorsemanBrain.StartedFames==true || BellumHorsemanBrain.StartedBellum==true || MorbusHorsemanBrain.StartedMorbus==true || FunusHorsemanBrain.StartedFunus==true || ApocalypseBrain.StartedApoc==true)
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
                                player.Out.SendMessage("Magic energy moves you to the center of room!", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
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
#endregion Apocalypse Initializer

#region 1st Horseman - Fames
public class FamesHorseman : GameEpicBoss
{
    public FamesHorseman() : base() { }
    public int StartFamesTimer(EcsGameTimer timer)
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
        GamePlayer player2 = ApocalypseInitializer.RandomTarget;
        if (CanInteract == false)
        {
            if (player == player2)
            {
                TurnTo(player.X, player.Y);

                player.Out.SendMessage("Fames says, Well? Do you challenge fate itself?\n" +
                    "Say [no] and walk away...\n" +
                    "Say [yes] and prepare yourselves.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
            }
        }
        if (ApocalypseInitializer.OthersCanInteract == true)
        {
            if (player != null)
            {
                TurnTo(player.X, player.Y);

                player.Out.SendMessage("Fames says, Well? Do you challenge fate itself?\n" +
                    "Say [no] and walk away...\n" +
                    "Say [yes] and prepare yourselves.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
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
            if (t == ApocalypseInitializer.RandomTarget || ApocalypseInitializer.OthersCanInteract == true)
            {
                TurnTo(t.X, t.Y);
                switch (str.ToLower())
                {
                    case "no":
                        {
                            t.Out.SendMessage("Then be gone and continue on with what you were meant to do.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
                        }
                        break;
                    case "yes":
                        {
                            foreach (GamePlayer player in GetPlayersInRadius(2500))
                            {
                                if (player != null)
                                {
                                    player.Out.SendMessage("Fames says, 'Done. You are brave " + t.PlayerClass.Name + " ... or foolish. While it is most certain that your" +
                                        " actions will have little chance to alter the course of fate, you and your companions are granted a few grains of time," +
                                        " two minutes in your terms, to prepare.", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
                                    new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(StartFamesTimer), 120000);//2min
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
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    private bool prepareBellum = false;
    public override void Die(GameObject killer)//on kill generate orbs
    {
        
        if(!prepareBellum)
        { 
            BroadcastMessage(String.Format("Bellum says, 'Prepare yourselves for war. One minute, you are granted.'"));
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnHorsemanBellum), 60000);//60s before starting
            prepareBellum = true;
        }
        FamesIsUp = false;
        FamesHorsemanBrain.StartedFames = false;
        base.Die(killer);
    }
    public int SpawnHorsemanBellum(EcsGameTimer timer)
    {
        BellumHorseman Add = new BellumHorseman();
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
        FamesHorsemanBrain.spawn_fate = false;
        CanInteract = false;
        FamesHorsemanBrain.StartedFames = false;
        FamesIsUp = true;
        prepareBellum = false;

        FamesHorsemanBrain adds = new FamesHorsemanBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion 1st Horseman - Fames

#region 2nd Horseman - Bellum
public class BellumHorseman : GameEpicBoss
{
    public BellumHorseman() : base() { }
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
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    private bool prepareMorbus = false;
    public override void Die(GameObject killer)//on kill generate orbs
    {
        if (!prepareMorbus)
        {
            BroadcastMessage(String.Format("Morbus says, 'Sometimes it is the smallest things that are the most deadly. Be prepared in one minute..'"));
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnHorsemanMorbus), 60000);//60s before starting
            prepareMorbus = true;
        }

        foreach (GameNpc npc in GetNPCsInRadius(4000))
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
        
        BellumHorsemanBrain.StartedBellum = false;
        BellumUP = false;
        spawn_fate2 = false;
        base.Die(killer);
    }
    public int SpawnHorsemanMorbus(EcsGameTimer timer)
    {
        MorbusHorseman Add = new MorbusHorseman();
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
        BellumHorsemanBrain.StartedBellum = false;
        BellumHorsemanBrain.SpawnWeapons = false;
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
        BellumHorsemanBrain adds = new BellumHorsemanBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion 2nd Horseman - Bellum

#region Bellum adds (Crush DMG)
public class WarIncarnateCrush : GameNpc
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
#endregion Bellum adds (Crush DMG)

#region Bellum adds (Slash DMG)
public class WarIncarnateSlash : GameNpc
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
#endregion Bellum adds (Slash DMG)

#region Bellum adds (Thrust DMG)
public class WarIncarnateThrust : GameNpc
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
#endregion Bellum adds (Thrust DMG)

#region 3rd Horseman - Morbus
public class MorbusHorseman : GameEpicBoss
{
    public MorbusHorseman() : base() { }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override void ReturnToSpawnPoint(short speed)
    {
        if (MorbusHorsemanBrain.IsBug)
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
                        truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);

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
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
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
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnHorsemanFunus), 60000);//60s before starting
            prepareFunus = true;
        }
        MorbusHorsemanBrain.StartedMorbus = false;
        spawn_fate3 = false;
        MorbusUP = false;
        base.Die(killer);
    }
    public int SpawnHorsemanFunus(EcsGameTimer timer)
    {
        FunusHorseman Add = new FunusHorseman();//inside controller
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
        MorbusHorsemanBrain.StartedMorbus = false;
        MorbusHorsemanBrain.BafMobs3 = false;
        MorbusHorsemanBrain.spawn_swarm = false;
        MorbusHorsemanBrain.message_warning1 = false;
        MorbusHorsemanBrain.IsBug = false;
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

        MorbusHorsemanBrain adds = new MorbusHorsemanBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion 3rd Horseman - Morbus

#region Morbus Swarm
public class MorbusSwarm : GameNpc
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
        --MorbusHorseman.Morbus_Swarm_count;
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
#endregion Morbus Swarm

#region 4th Horseman - Funus
public class FunusHorseman : GameEpicBoss
{
    public FunusHorseman() : base() { }
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
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer)
        {
            GamePlayer truc = source as GamePlayer;
            //cleri,merc,arms,infi,scout
            if (truc.PlayerClass.ID == 6 || (truc.PlayerClass.ID == 11 && truc.ActiveWeapon.Object_Type == 5) || (truc.PlayerClass.ID == 2 && truc.ActiveWeapon.Object_Type == 10) 
                || (truc.PlayerClass.ID == 9 && truc.ActiveWeapon.Object_Type == 10) || (truc.PlayerClass.ID == 3 && truc.ActiveWeapon.Object_Type == 9))
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
            else
            {
                truc.Out.SendMessage(Name + " absorbs all your damage to heal iself!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
            pet_owner.Out.SendMessage(Name + " absorbs all your damage to heal iself!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnApoc), 5000);
            prepareApoc = true;
        }
        spawn_fate4 = false;
        FunusHorsemanBrain.StartedFunus = false;
        FunusUp = false;
        base.Die(killer);
    }
    public int SpawnApoc(EcsGameTimer timer)
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
        FunusHorsemanBrain.StartedFunus = false;
        FunusHorsemanBrain.BafMobs4 = false;
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
        FunusHorsemanBrain adds = new FunusHorsemanBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion 4th Horseman - Funus

#region Apocalypse
public class Apocalypse : GameEpicBoss
{
    public Apocalypse() : base() { }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
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
        foreach (GameNpc npc in GetNPCsInRadius(4000))
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
        ApocalypseInitializer.start_respawn_check = false;
        ApocUP = false;
        base.Die(killer);
    }      
    
    protected int AwardEpicEncounterKillPoint()
    {
        int count = 0;
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.KillsEpicBoss++;
            player.Achieve(AchievementUtil.AchievementName.Epic_Boss_Kills);
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
#endregion Apocalypse

#region Harbinger of Fate
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
#endregion Harbringer of Fate

#region Rain of Fire
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

        Flags ^= ENpcFlags.FLYING;
        Flags ^= ENpcFlags.CANTTARGET;
        Flags ^= ENpcFlags.DONTSHOWNAME;
        Flags ^= ENpcFlags.STATUE;

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
#endregion Rain of Fire