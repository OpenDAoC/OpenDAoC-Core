using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Styles;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS;

public class LordGildas : GameEpicBoss
{
    public LordGildas() : base()
    {
    }
    public static int TauntID = 66;
    public static int TauntClassID = 2;
    public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

    public static int SlamID = 228;
    public static int SlamClassID = 2;
    public static Style slam = SkillBase.GetStyleByID(SlamID, SlamClassID);

    public static int BackStyleID = 113;
    public static int BackStyleClassID = 2;
    public static Style BackStyle = SkillBase.GetStyleByID(BackStyleID, BackStyleClassID);

    public static int AfterStyleID = 97;
    public static int AfterStyleClassID = 2;
    public static Style AfterStyle = SkillBase.GetStyleByID(AfterStyleID, AfterStyleClassID);

    public static int PoleAnytimerID = 93;
    public static int PoleAnytimerClassID = 2;
    public static Style PoleAnytimer = SkillBase.GetStyleByID(PoleAnytimerID, PoleAnytimerClassID);

    public static int Taunt2hID = 103;
    public static int Taunt2hClassID = 2;
    public static Style Taunt2h = SkillBase.GetStyleByID(Taunt2hID, Taunt2hClassID);
    public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
    {
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        base.OnAttackEnemy(ad);
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
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7719);
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
        RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Humanoid;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 4);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 5);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
        template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1077, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.Standard);
        LordGildasBrain.Stage2 = false;
        LordGildasBrain.CanWalk = false;
        LordGildasBrain.Reset_Gildas = false;
        if (!Styles.Contains(taunt))
            Styles.Add(taunt);
        if (!Styles.Contains(slam))
            Styles.Add(slam);
        if (!Styles.Contains(BackStyle))
            Styles.Add(BackStyle);
        if (!Styles.Contains(AfterStyle))
            Styles.Add(AfterStyle);
        if (!Styles.Contains(PoleAnytimer))
            Styles.Add(PoleAnytimer);
        if (!Styles.Contains(Taunt2h))
            Styles.Add(Taunt2h);
        VisibleActiveWeaponSlots = 16;
        MeleeDamageType = EDamageType.Slash;
        LordGildasBrain sbrain = new LordGildasBrain();
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
        npcs = WorldMgr.GetNPCsByNameFromRegion("Lord Gildas", 277, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Lord Gildas found, creating it...");

            log.Warn("Initializing Lord Gildas...");
            LordGildas HOC = new LordGildas();
            HOC.Name = "Lord Gildas";
            HOC.Model = 40;
            HOC.Realm = 0;
            HOC.Level = 75;
            HOC.Size = 50;
            HOC.CurrentRegionID = 277; //hall of the corrupt
            HOC.MeleeDamageType = EDamageType.Slash;
            HOC.RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            HOC.Faction = FactionMgr.GetFactionByID(187);
            HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            HOC.X = 29015;
            HOC.Y = 41910;
            HOC.Z = 12933;
            HOC.Heading = 2063;
            LordGildasBrain ubrain = new LordGildasBrain();
            HOC.SetOwnBrain(ubrain);
            HOC.AddToWorld();
            HOC.SaveIntoDatabase();
            HOC.Brain.Start();
        }
        else
            log.Warn("Lord Gildas exist ingame, remove it and restart server if you want to add by script code.");
    }
}