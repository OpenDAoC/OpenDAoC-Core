using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS.Scripts;

public class BaneOfHope : GameEpicBoss
{
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
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 350;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * 30 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override short MaxSpeedBase => (short) (191 + Level * 2);
    public override int MaxHealth => 100000;
    public override int AttackRange
    {
        get => 180;
        set { }
    }
    public override bool AddToWorld()
    {
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158245);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;

        BaneOfHopeBrain adds = new BaneOfHopeBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        //MoveTo(CurrentRegionID, 31154, 30913, 13950, 3043);
        base.Die(killer);
    }       
}