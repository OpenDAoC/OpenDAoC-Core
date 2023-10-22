using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;
    
#region Black Lady
public class BlackLady : GameEpicBoss
{
    public BlackLady() : base()
    {
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Black Lady initialized..");
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
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 30; // dmg reduction for melee dmg
            case EDamageType.Crush: return 30; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 30; // dmg reduction for melee dmg
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
        get { return 40000; }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8817);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Gender = EGender.Female;
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        RespawnInterval = ServerProperty.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 43, 0, 0);//modelID,color,effect,extension
        template.AddNPCEquipment(EInventorySlot.ArmsArmor, 380, 43, 0);
        template.AddNPCEquipment(EInventorySlot.LegsArmor, 379, 43);
        template.AddNPCEquipment(EInventorySlot.HandsArmor, 381, 43, 0, 0);
        template.AddNPCEquipment(EInventorySlot.FeetArmor, 382, 43, 0, 0);
        template.AddNPCEquipment(EInventorySlot.Cloak, 443, 43, 0, 0);
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 468, 43, 91);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        // humanoid
        IsOakUp = false;
        if (IsOakUp == false)
        {
            SpawnOak();
            IsOakUp = true;
        }
        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        BodyType = 6;
        IsCloakHoodUp = true;
        Ogress.OgressCount = 0;
        BlackLadyBrain blackladybrain = new BlackLadyBrain();
        SetOwnBrain(blackladybrain);
        base.AddToWorld();
        return true;
    }
    public static bool IsOakUp = false;
    public void SpawnOak()
    {
            AncientBlackOak Add1 = new AncientBlackOak();
            Add1.X = 30091;
            Add1.Y = 37620;
            Add1.Z = 15049;
            Add1.CurrentRegionID = 276;
            Add1.RespawnInterval = -1;
            Add1.Heading = 2053;
            Add1.AddToWorld();
    }
    public override void Die(GameObject killer)
    {
        base.Die(killer);
        foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
        {
            if (npc.Brain is OgressBrain)
            {
                npc.RemoveFromWorld();
                Ogress.OgressCount = 0;
            }
        }
        foreach (GameNpc npc2 in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
        {
            if (npc2.Brain is AncientBlackOakBrain)
            {
                npc2.Die(npc2);
            }
        }
    }
}
#endregion Black Lady

#region Ogress
public class Ogress : GameNpc
{
    public Ogress() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash:
            case EDamageType.Crush:
            case EDamageType.Thrust: return 35;
            default: return 25;
        }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.25;
    }
    public override int MaxHealth
    {
        get { return 2500; }
    }
    public static int OgressCount = 0;
    public override void Die(GameObject killer)
    {
        --OgressCount;
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        Model = 402;
        Level = (byte)Util.Random(50, 55);
        Name = "Ogress";
        Size = (byte)Util.Random(40, 50);
        Faction = FactionMgr.GetFactionByID(187);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
        RespawnInterval = -1;
        MaxSpeedBase = 200;
        Realm = ERealm.None;
        MaxDistance = 0;
        TetherRange = 0;

        ++OgressCount;
        Strength = 50;
        Dexterity = 200;
        Constitution = 100;
        Quickness = 125;
        OgressBrain ogressbrain = new OgressBrain();
        SetOwnBrain(ogressbrain);
        base.AddToWorld();
        return true;
    }
}
#endregion Ogress