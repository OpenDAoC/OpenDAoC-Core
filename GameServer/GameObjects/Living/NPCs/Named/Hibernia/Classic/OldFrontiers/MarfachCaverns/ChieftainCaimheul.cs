using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Styles;

namespace Core.GS;

public class ChieftainCaimheul : GameEpicBoss
{
    public ChieftainCaimheul() : base()
    {
    }
    public static int TauntID = 247;
    public static int TauntClassID = 44;
    public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

    public static int Taunt2hID = 309;
    public static int Taunt2hClassID = 44; 
    public static Style taunt2h = SkillBase.GetStyleByID(Taunt2hID, Taunt2hClassID);

    public static int SlamID = 228;
    public static int SlamClassID = 44;
    public static Style slam = SkillBase.GetStyleByID(SlamID, SlamClassID);

    public static int SideStyleID = 318;
    public static int SideStyleClassID = 44;
    public static Style SideStyle = SkillBase.GetStyleByID(SideStyleID, SideStyleClassID);

    public static int SideFollowUpID = 319;
    public static int SideFollowUpClassID = 44;
    public static Style SideFollowUp = SkillBase.GetStyleByID(SideFollowUpID, SideFollowUpClassID);

    public static int AfterParryID = 313;
    public static int AfterParryClassID = 44;
    public static Style AfterParry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);
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
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8821);
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
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 667, 0, 0, 6);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 410, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 409, 0);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 411, 0, 0, 4);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 412, 0, 0, 5);
        template.AddNPCEquipment(EInventorySlot.HeadArmor, 1200, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 678, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 446, 0, 0);
        template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1147, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 475, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.Standard);
        if (!Styles.Contains(Taunt))
            Styles.Add(Taunt);
        if (!Styles.Contains(taunt2h))
            Styles.Add(taunt2h);
        if (!Styles.Contains(SideStyle))
            Styles.Add(SideStyle);
        if (!Styles.Contains(SideFollowUp))
            Styles.Add(SideFollowUp);
        if (!Styles.Contains(slam))
            Styles.Add(slam);
        if (!Styles.Contains(AfterParry))
            Styles.Add(AfterParry);

        ChieftainCaimheulBrain.Phase2 = false;
        ChieftainCaimheulBrain.CanWalk = false;
        ChieftainCaimheulBrain.IsPulled = false;
        VisibleActiveWeaponSlots = 16;
        MeleeDamageType = EDamageType.Slash;
        ChieftainCaimheulBrain sbrain = new ChieftainCaimheulBrain();
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
        npcs = WorldMgr.GetNPCsByNameFromRegion("Chieftain Caimheul", 276, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Chieftain Caimheul not found, creating it...");

            log.Warn("Initializing Chieftain Caimheul...");
            ChieftainCaimheul HOC = new ChieftainCaimheul();
            HOC.Name = "Chieftain Caimheul";
            HOC.Model = 354;
            HOC.Realm = 0;
            HOC.Level = 65;
            HOC.Size = 50;
            HOC.CurrentRegionID = 276; //marfach caverns
            HOC.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            HOC.Faction = FactionMgr.GetFactionByID(187);
            HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            HOC.X = 32597;
            HOC.Y = 38903;
            HOC.Z = 15061;
            HOC.Heading = 694;
            ChieftainCaimheulBrain ubrain = new ChieftainCaimheulBrain();
            HOC.SetOwnBrain(ubrain);
            HOC.AddToWorld();
            HOC.SaveIntoDatabase();
            HOC.Brain.Start();
        }
        else
            log.Warn("Chieftain Caimheul exist ingame, remove it and restart server if you want to add by script code.");
    }
}