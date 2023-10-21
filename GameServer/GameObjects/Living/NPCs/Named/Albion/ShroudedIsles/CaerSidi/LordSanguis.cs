using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

#region Lord Sanguis
public class LordSanguis : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public LordSanguis()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 40;// dmg reduction for melee dmg
            case EDamageType.Crush: return 40;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
            default: return 70;// dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100  * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override int MaxHealth
    {
        get { return 100000; }
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override int AttackRange
    {
        get { return 450; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
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
    public override bool AddToWorld()
    {
        Spawn_Lich_Lord = false;
        foreach (GameNpc npc in GetNPCsInRadius(5000))
        {
            if (npc != null)
            {
                if (npc.IsAlive)
                {
                    if (npc.Brain is LichLordSanguisBrain)
                        npc.RemoveFromWorld();
                }
            }
        }
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163412);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;

        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        LordSanguisBrain sbrain = new LordSanguisBrain();
        SetOwnBrain(sbrain);
        base.AddToWorld();
        return true;
    }

    public override void Die(GameObject killer)
    {
        if (Spawn_Lich_Lord == false)
        {
            BroadcastMessage(String.Format(this.Name + " comes back to life as Lich Lord Sanguis!"));
            SpawnMages();
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(SpawnLich), 6000);
            Spawn_Lich_Lord = true;
        }

        base.Die(killer);
    }

    public static bool Spawn_Lich_Lord = false;

    public int SpawnLich(EcsGameTimer timer)
    {
        LichLordSanguis Add = new LichLordSanguis();
        Add.X = X;
        Add.Y = Y;
        Add.Z = Z;
        Add.CurrentRegion = CurrentRegion;
        Add.Heading = Heading;
        Add.AddToWorld();
        return 0;
    }

    public void SpawnMages()
    {
        for (int i = 0; i < Util.Random(2, 4); i++) // Spawn 2-4 mages
        {
            BloodMage Add = new BloodMage();
            Add.X = X + Util.Random(-50, 80);
            Add.Y = Y + Util.Random(-50, 80);
            Add.Z = Z;
            Add.CurrentRegion = CurrentRegion;
            Add.Heading = Heading;
            Add.AddToWorld();
        }
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Lord Sanguis", 60, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Lord Sanguis  not found, creating it...");

            log.Warn("Initializing Lord Sanguis...");
            LordSanguis CO = new LordSanguis();
            CO.Name = "Lord Sanguis";
            CO.Model = 952;
            CO.Realm = 0;
            CO.Level = 81;
            CO.Size = 100;
            CO.CurrentRegionID = 60; //caer sidi

            CO.MeleeDamageType = EDamageType.Crush;
            CO.Faction = FactionMgr.GetFactionByID(64);
            CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            CO.RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            CO.X = 34080;
            CO.Y = 32919;
            CO.Z = 14518;
            CO.MaxDistance = 2000;
            CO.TetherRange = 2000;
            CO.MaxSpeedBase = 250;
            CO.Heading = 4079;

            LordSanguisBrain ubrain = new LordSanguisBrain();
            ubrain.AggroLevel = 100;
            ubrain.AggroRange = 500;
            CO.SetOwnBrain(ubrain);
            CO.AddToWorld();
            CO.Brain.Start();
            CO.SaveIntoDatabase();
        }
        else
            log.Warn("Lord Sanguis exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Lord Sanguis

#region Lich Lord Sanguis
public class LichLordSanguis : GameEpicBoss
{
    public LichLordSanguis() : base()
    {
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
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
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 40;// dmg reduction for melee dmg
            case EDamageType.Crush: return 40;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
            default: return 70;// dmg reduction for rest resists
        }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163267);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        ParryChance = npcTemplate.ParryChance;
        Empathy = npcTemplate.Empathy;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 442, 67);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        Model = 952;
        Flags = ENpcFlags.GHOST;
        Name = "Lich Lord Sanguis";
        ParryChance = 35;
        RespawnInterval = -1;
        LichLordSanguisBrain.set_flag = false;

        MaxDistance = 2000;
        TetherRange = 2000;
        Size = 100;
        Level = 81;
        MaxSpeedBase = 250;

        Faction = FactionMgr.GetFactionByID(64);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
        BodyType = 8;
        Realm = ERealm.None;
        LichLordSanguisBrain.set_flag = false;
        LichLordSanguisBrain adds = new LichLordSanguisBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }

    public override void Die(GameObject killer) //on kill generate orbs
    {
        base.Die(killer);
    }
}
#endregion Lich Lord Sanguis

#region Blood Mage
public class BloodMage : GameNpc //thrust resist
{
    public BloodMage() : base()
    {
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }

    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 150;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }

    public override int MaxHealth
    {
        get { return 8000; }
    }

    public override void Die(GameObject killer)
    {
        --MageCount;
        base.Die(killer);
    }

    public static int MageCount = 0;
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 150; }   
    public override bool AddToWorld()
    {
        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 798, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 67);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 67);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 96, 67);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 442, 67);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);

        Model = (byte) Util.Random(61, 68);
        IsCloakHoodUp = true;
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        Name = "blood mage";
        RespawnInterval = -1;

        MaxDistance = 2500;
        TetherRange = 3000;
        RoamingRange = 120;
        Size = 50;
        Level = 70;
        MaxSpeedBase = 200;

        Faction = FactionMgr.GetFactionByID(64);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
        BodyType = 6;
        Realm = ERealm.None;
        ++MageCount;

        BloodMageBrain adds = new BloodMageBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Blood Mage