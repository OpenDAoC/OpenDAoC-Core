using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS;

namespace Core.GS;

#region Blue Lady
public class BlueLady : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Blue Lady initialized..");
    }
    public BlueLady()
        : base()
    {
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (IsOutOfTetherRange)
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
        get { return 450; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 30; // dmg reduction for rest resists
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
        get { return 30000; }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8818);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 380, 54, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 379, 54);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 381, 54, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 382, 54, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 443, 54, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 468, 43, 91);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        // humanoid
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        IsCloakHoodUp = true;
        BlueLadyBrain sBrain = new BlueLadyBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(this.CurrentRegionID))
        {
            if (npc.Brain is BlueLadyAddBrain)
            {
                npc.RemoveFromWorld();
            }
        }
        base.Die(killer);
    }
}
#endregion Blue Lady

#region Blue Lady Weapon adds
public class BlueLadySwordAdd : GameNpc
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BlueLadySwordAdd()
        : base()
    {
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 200;
    }
    public override int MaxHealth
    {
        get { return 500; }
    }
    public override int AttackRange
    {
        get { return 450; }
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
    public static int SwordCount = 0;
    public override void Die(GameObject killer)
    {
        --SwordCount;
        base.Die(killer);
    }
    public override long ExperienceValue => 0;
    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 125; }
    public override short Strength { get => base.Strength; set => base.Strength = 50; }
    public override bool AddToWorld()
    {
        BlueLadySwordAdd blueLady = new BlueLadySwordAdd();
        Model = 665;
        Name = "summoned sword";
        Size = 60;
        Level = (byte)Util.Random(50, 55);
        Realm = 0;

        ++SwordCount;
        RespawnInterval = -1;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

        Gender = EGender.Neutral;
        MeleeDamageType = EDamageType.Slash;

        GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
        templateHib.AddNPCEquipment(EInventorySlot.RightHandWeapon, 5);
        Inventory = templateHib.CloseTemplate();
        VisibleActiveWeaponSlots = (byte)EActiveWeaponSlot.Standard;

        BodyType = 6;
        BlueLadyAddBrain sBrain = new BlueLadyAddBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        base.AddToWorld();
        return true;
    }
}
public class BlueLadyAxeAdd : GameNpc
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BlueLadyAxeAdd()
        : base()
    {
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 200;
    }
    public override int MaxHealth
    {
        get { return 500; }
    }
    public override int AttackRange
    {
        get { return 450; }
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
    public static int AxeCount = 0;
    public override void Die(GameObject killer)
    {
        --AxeCount;
        base.Die(killer);
    }
    public override long ExperienceValue => 0;
    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 125; }
    public override short Strength { get => base.Strength; set => base.Strength = 50; }
    public override bool AddToWorld()
    {
        BlueLadyAxeAdd blueLady = new BlueLadyAxeAdd();
        Model = 665;
        Name = "summoned axe";
        Size = 60;
        Level = (byte)Util.Random(50, 55);
        Realm = 0;

        GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
        templateHib.AddNPCEquipment(EInventorySlot.RightHandWeapon, 316);
        Inventory = templateHib.CloseTemplate();
        VisibleActiveWeaponSlots = (byte)EActiveWeaponSlot.Standard;

        ++AxeCount;
        RespawnInterval = -1;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

        Gender = EGender.Neutral;
        MeleeDamageType = EDamageType.Slash;

        BodyType = 6;
        BlueLadyAddBrain sBrain = new BlueLadyAddBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        base.AddToWorld();
        return true;
    }
}
#endregion Blue Lady Weapon adds