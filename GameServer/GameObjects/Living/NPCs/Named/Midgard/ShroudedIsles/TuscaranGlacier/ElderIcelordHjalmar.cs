using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Styles;
using Core.GS.World;

namespace Core.GS;

#region Elder Icelord Hjalmar
public class ElderIcelordHjalmar : GameEpicBoss
{
    public ElderIcelordHjalmar() : base()
    {
    }

    public static int TauntID = 292;
    public static int TauntClassID = 44;
    public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

    public static int BackStyleID = 304;
    public static int BackStyleClassID = 44;
    public static Style back_style = SkillBase.GetStyleByID(BackStyleID, BackStyleClassID);

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
    public static int HjalmarCount = 0;
    public override void Die(GameObject killer) //on kill generate orbs
    {
        HjalmarCount = 0;
        base.Die(killer);
    }

    public override void OnAttackEnemy(AttackData ad)
    {
        if (ad != null && (ad.AttackResult == EAttackResult.HitStyle || ad.AttackResult == EAttackResult.HitUnstyled))
        {
            if (Util.Chance(20))
                SpawnAdds();
        }
        base.OnAttackEnemy(ad);
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160394);
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
        RespawnInterval = -1;
        BodyType = (ushort)EBodyType.Giant;
        ElderIcelordHjalmarBrain.message1 = false;
        ElderIcelordHjalmarBrain.message2 = false;
        HjalmarCount = 1;

        if(!Styles.Contains(taunt))
            Styles.Add(taunt);
        if (!Styles.Contains(back_style))
            Styles.Add(back_style);

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 572, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Slash;
        ElderIcelordHjalmarBrain sbrain = new ElderIcelordHjalmarBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = true;
        base.AddToWorld();
        return true;
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public void SpawnAdds()
    {
        BroadcastMessage(Name + " spasms as dark energies swirl around his body!");
        Morkimma npc = new Morkimma();
        npc.X = TargetObject.X + Util.Random(-100, 100);
        npc.Y = TargetObject.Y + Util.Random(-100, 100);
        npc.Z = TargetObject.Z;
        npc.RespawnInterval = -1;
        npc.Heading = Heading;
        npc.CurrentRegion = CurrentRegion;
        npc.AddToWorld();
    }
}
#endregion Elder Icelord Hjalmar

#region Hjalmar adds
public class Morkimma : GameNpc
{
    public Morkimma() : base()
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
        return base.AttackDamage(weapon) * Strength / 50 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }

    protected int Show_Effect(EcsGameTimer timer)
    {
        if (IsAlive)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(this, this, 4323, 0, false, 0x01);

            return 3000;
        }

        return 0;
    }
    
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.25;
    }
    public override int MaxHealth
    {
        get { return 1000; }
    }
    public override bool AddToWorld()
    {
        Model = 665;
        Size = 50;
        Strength = 100;
        Quickness = 100;
        Dexterity = 180;
        Constitution = 100;
        MaxSpeedBase = 220;
        Name = "Morkimma";
        Level = (byte)Util.Random(50, 55);

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        Realm = ERealm.None;
        RespawnInterval = -1;

        MorkimmaBrain adds = new MorkimmaBrain();
        SetOwnBrain(adds);
        bool success = base.AddToWorld();
        if (success)
        {
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
        }
        return success;
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (Util.Chance(15))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle) && HealthPercent < 100)
                CastSpell(MorkimmaHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
    }
    private Spell m_MorkimmaHeal;
    private Spell MorkimmaHeal
    {
        get
        {
            if (m_MorkimmaHeal == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 10;
                spell.ClientEffect = 1340;
                spell.Icon = 1340;
                spell.TooltipId = 1340;
                spell.Value = 200;
                spell.Name = "Morkimma's Heal";
                spell.Range = 1500;
                spell.SpellID = 11930;
                spell.Target = "Self";
                spell.Type = ESpellType.Heal.ToString();
                spell.Uninterruptible = true;
                m_MorkimmaHeal = new Spell(spell, 50);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MorkimmaHeal);
            }
            return m_MorkimmaHeal;
        }
    }
}
#endregion Hjalmar adds