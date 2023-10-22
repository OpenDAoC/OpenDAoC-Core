using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Styles;
using Core.GS.World;

namespace Core.GS;

public class AvartackTheChampion : GameEpicBoss
{
    public AvartackTheChampion() : base()
    {
    }
    public static int TauntID = 292;
    public static int TauntClassID = 45;
    public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

    public static int BackStyleID = 304;
    public static int BackStyleClassID = 45;
    public static Style BackStyle = SkillBase.GetStyleByID(BackStyleID, BackStyleClassID);
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
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8820);
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
        RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Humanoid;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 667, 0, 0, 6);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 410, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 409, 0);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 411, 0, 0, 4);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 412, 0, 0, 5);
        template.AddNPCEquipment(EInventorySlot.HeadArmor, 1200, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 678, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 474, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        if (!Styles.Contains(Taunt))
            Styles.Add(Taunt);
        if (!Styles.Contains(BackStyle))
            Styles.Add(BackStyle);
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        AvartackTheChampionBrain sbrain = new AvartackTheChampionBrain();
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
        npcs = WorldMgr.GetNPCsByNameFromRegion("Avartack the Champion", 276, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Avartack the Champion not found, creating it...");

            log.Warn("Initializing Avartack the Champion...");
            AvartackTheChampion HOC = new AvartackTheChampion();
            HOC.Name = "Avartack the Champion";
            HOC.Model = 320;
            HOC.Realm = 0;
            HOC.Level = 65;
            HOC.Size = 50;
            HOC.CurrentRegionID = 276; //marfach caverns
            HOC.RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            HOC.Faction = FactionMgr.GetFactionByID(187);
            HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            HOC.X = 28926;
            HOC.Y = 35755;
            HOC.Z = 15065;
            HOC.Heading = 2552;
            AvartackTheChampionBrain ubrain = new AvartackTheChampionBrain();
            HOC.SetOwnBrain(ubrain);
            HOC.AddToWorld();
            HOC.SaveIntoDatabase();
            HOC.Brain.Start();
        }
        else
            log.Warn("Avartack the Champion exist ingame, remove it and restart server if you want to add by script code.");
    }
}