using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

public class ElderCouncilBirghir : GameEpicBoss
{
    public ElderCouncilBirghir() : base()
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
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
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
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160391);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Giant;

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 19, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
        ElderCouncilBirghirBrain.IsTargetPicked = false;
        ElderCouncilBirghirBrain.message1 = false;
        ElderCouncilBirghirBrain.IsPulled = false;

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        ElderCouncilBirghirBrain sbrain = new ElderCouncilBirghirBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
}