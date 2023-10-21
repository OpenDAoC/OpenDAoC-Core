using System;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.AI.Brain;
using Core.Database.Tables;
using Core.GS.AI.Brains;

namespace Core.GS;

#region Aidon The Archwizard
public class AidonTheArchwizard : GameEpicBoss
{
    public AidonTheArchwizard() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 30; // dmg reduction for melee dmg
            case EDamageType.Crush: return 30; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 30; // dmg reduction for melee dmg
            default: return 40; // dmg reduction for rest resists
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
        get { return 40000; }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (this.IsOutOfTetherRange)
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                    damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                    damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                    || damageType == EDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);
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
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7721);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        RespawnInterval =
            ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Humanoid;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        AidonTheArchwizardBrain.IsPulled = false;
        AidonTheArchwizardBrain.CanCast = false;

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        AidonTheArchwizardBrain sbrain = new AidonTheArchwizardBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Aidon the Archwizard", 277, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Aidon the Archwizard found, creating it...");

            log.Warn("Initializing Aidon the Archwizard...");
            AidonTheArchwizard HOC = new AidonTheArchwizard();
            HOC.Name = "Aidon the Archwizard";
            HOC.Model = 61;
            HOC.Realm = 0;
            HOC.Level = 75;
            HOC.Size = 60;
            HOC.CurrentRegionID = 277; //hall of the corrupt
            HOC.MeleeDamageType = EDamageType.Crush;
            HOC.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            HOC.Faction = FactionMgr.GetFactionByID(187);
            HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            HOC.X = 31353;
            HOC.Y = 37634;
            HOC.Z = 14873;
            HOC.Heading = 2070;
            AidonTheArchwizardBrain ubrain = new AidonTheArchwizardBrain();
            HOC.SetOwnBrain(ubrain);
            HOC.AddToWorld();
            HOC.SaveIntoDatabase();
            HOC.Brain.Start();
        }
        else
            log.Warn("Aidon the Archwizard exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Aidon The Archwizard

#region Aidon Fire Copy
public class AidonCopyFire : GameNpc
{
    public AidonCopyFire() : base()
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
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public static int CopyCountFire = 0;
    public override void Die(GameObject killer)
    {
        --CopyCountFire;
        base.Die(killer);
    }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override bool AddToWorld()
    {
        Model = 61;
        MeleeDamageType = EDamageType.Crush;
        Name = "Illusion of Aidon the Archwizard";
        RespawnInterval = -1;
        Flags = ENpcFlags.GHOST;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        ++CopyCountFire;

        Size = 55;
        Level = 75;
        MaxSpeedBase = 0;//copies not moves

        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        BodyType = 6;
        Realm = ERealm.None;
        AidonCopyFireBrain adds = new AidonCopyFireBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Aidon Fire Copy

#region Aidon Ice Copy
public class AidonCopyIce : GameNpc
{
    public AidonCopyIce() : base()
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
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public static int CopyCountIce = 0;
    public override void Die(GameObject killer)
    {
        --CopyCountIce;
        base.Die(killer);
    }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override bool AddToWorld()
    {
        Model = 61;
        MeleeDamageType = EDamageType.Crush;
        Name = "Illusion of Aidon the Archwizard";
        RespawnInterval = -1;
        Flags = ENpcFlags.GHOST;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        ++CopyCountIce;

        Size = 55;
        Level = 75;
        MaxSpeedBase = 0;//copies not moves

        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        BodyType = 6;
        Realm = ERealm.None;
        AidonCopyIceBrain adds = new AidonCopyIceBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Aidon Ice Copy

#region Aidon Air Copy
public class AidonCopyAir : GameNpc
{
    public AidonCopyAir() : base()
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
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public static int CopyCountAir = 0;
    public override void Die(GameObject killer)
    {
        --CopyCountAir;
        base.Die(killer);
    }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override bool AddToWorld()
    {
        Model = 61;
        MeleeDamageType = EDamageType.Crush;
        Name = "Illusion of Aidon the Archwizard";
        RespawnInterval = -1;
        Flags = ENpcFlags.GHOST;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        ++CopyCountAir;

        Size = 55;
        Level = 75;
        MaxSpeedBase = 0;//copies not moves

        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        BodyType = 6;
        Realm = ERealm.None;
        AidonCopyAirBrain adds = new AidonCopyAirBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Aidon Air Copy

#region Aidon Earth Copy
public class AidonCopyEarth : GameNpc
{
    public AidonCopyEarth() : base()
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
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public static int CopyCountEarth = 0;
    public override void Die(GameObject killer)
    {
        --CopyCountEarth;
        base.Die(killer);
    }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 200; }
    public override bool AddToWorld()
    {
        Model = 61;
        MeleeDamageType = EDamageType.Crush;
        Name = "Illusion of Aidon the Archwizard";
        RespawnInterval = -1;
        Flags = ENpcFlags.GHOST;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 141, 54, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 140, 54);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 142, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 143, 67, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 1166, 0, 94);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        ++CopyCountEarth;

        Size = 55;
        Level = 75;
        MaxSpeedBase = 0;//copies not moves

        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        BodyType = 6;
        Realm = ERealm.None;
        AidonCopyEarthBrain adds = new AidonCopyEarthBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Aidon Earth Copy