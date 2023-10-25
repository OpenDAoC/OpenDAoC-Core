using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Styles;
using Core.GS.World;

namespace Core.GS;

#region Lady Darra
public class LadyDarra : GameEpicBoss
{
    public LadyDarra() : base()
    {
    }
    public static int TauntID = 66;
    public static int TauntClassID = 1; //pala
    public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

    public static int AfterParryID = 246;
    public static int AfterParryClassID = 44;
    public static Style after_parry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);

    public static int AfterBlockID = 229;
    public static int AfterBlockClassID = 1;//pala
    public static Style after_block = SkillBase.GetStyleByID(AfterBlockID, AfterBlockClassID);
    public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
    {
        if (ad != null && ad.AttackResult == EAttackResult.Blocked)
        {
            styleComponent.NextCombatBackupStyle = taunt;
            styleComponent.NextCombatStyle = after_block; 
        }
        if (ad != null && ad.AttackResult == EAttackResult.Parried)
        {
            styleComponent.NextCombatBackupStyle = taunt; 
            styleComponent.NextCombatStyle = after_parry; 
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
        {
            styleComponent.NextCombatStyle = taunt; //boss hit unstyled/styled so taunt
        }
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
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7720);
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
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 4);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 5);
        template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
        template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1077, 0, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.Standard);
        if(!Styles.Contains(taunt))
            Styles.Add(taunt);
        if(!Styles.Contains(after_block))
            Styles.Add(after_block);
        LadyDarraBrain.reset_darra = false;
        spawn_palas = false;
        
        VisibleActiveWeaponSlots = 16;
        MeleeDamageType = EDamageType.Slash;
        LadyDarraBrain sbrain = new LadyDarraBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        bool success = base.AddToWorld();
        if (success)
        {
            SpawnPaladins();
        }
        return success;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Lady Darra", 277, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Lady Darra found, creating it...");

            log.Warn("Initializing Lady Darra...");
            LadyDarra HOC = new LadyDarra();
            HOC.Name = "Lady Darra";
            HOC.Model = 35;
            HOC.Realm = 0;
            HOC.Level = 68;
            HOC.Size = 50;
            HOC.CurrentRegionID = 277; //hall of the corrupt
            HOC.MeleeDamageType = EDamageType.Slash;
            HOC.RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            HOC.Faction = FactionMgr.GetFactionByID(187);
            HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            HOC.X = 29551;
            HOC.Y = 31554;
            HOC.Z = 13941;
            HOC.Heading = 3014;
            LadyDarraBrain ubrain = new LadyDarraBrain();
            HOC.SetOwnBrain(ubrain);
            HOC.AddToWorld();
            HOC.SaveIntoDatabase();
            HOC.Brain.Start();
        }
        else
            log.Warn("Lady Darra exist ingame, remove it and restart server if you want to add by script code.");
    }
    public static bool spawn_palas = false;
    public void SpawnPaladins()
    {
        if (SpectralPaladin.paladins_count == 0 && spawn_palas==false)
        {
            SpectralPaladin Add1 = new SpectralPaladin();
            Add1.X = 30000;
            Add1.Y = 31057;
            Add1.Z = 13893;
            Add1.CurrentRegionID = 277;
            Add1.Heading = 479;
            Add1.RespawnInterval = -1;
            Add1.AddToWorld();

            SpectralPaladin Add2 = new SpectralPaladin();
            Add2.X = 29134;
            Add2.Y = 31054;
            Add2.Z = 13893;
            Add2.CurrentRegionID = 277;
            Add2.Heading = 3565;
            Add2.RespawnInterval = -1;
            Add2.AddToWorld();

            SpectralPaladin Add3 = new SpectralPaladin();
            Add3.X = 29128;
            Add3.Y = 31924;
            Add3.Z = 13893;
            Add3.CurrentRegionID = 277;
            Add3.Heading = 2552;
            Add3.RespawnInterval = -1;
            Add3.AddToWorld();

            SpectralPaladin Add4 = new SpectralPaladin();
            Add4.X = 30004;
            Add4.Y = 31928;
            Add4.Z = 13893;
            Add4.CurrentRegionID = 277;
            Add4.Heading = 1520;
            Add4.RespawnInterval = -1;
            Add4.AddToWorld();
            spawn_palas = true;
        }
    }
}
#endregion Lady Darra

#region Spectral Paladin
public class SpectralPaladin : GameNpc
{
    public SpectralPaladin() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 25; // dmg reduction for melee dmg
            case EDamageType.Crush: return 25; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 25; // dmg reduction for melee dmg
            default: return 25; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 60;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
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
    public static int paladins_count = 0;
    public override void Die(GameObject killer)
    {
        --paladins_count;
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        RespawnInterval = -1;
        Flags = ENpcFlags.GHOST;
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7710);
        LoadTemplate(npcTemplate);
        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 43, 0, 6);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 43);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 43);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 43, 0, 4);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 43, 0, 5);
        template.AddNPCEquipment(EInventorySlot.HeadArmor, 93, 43, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 57, 430, 0, 0);
        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 43, 0);
        template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1077, 43, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.Standard);
        VisibleActiveWeaponSlots = 16;
        ++paladins_count;

        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        SpectralPaladinBrain adds = new SpectralPaladinBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Spectral Paladin