using System;
using Core.AI.Brain;
using Core.Database;

namespace Core.GS;

#region Elder Icelord Suttung
public class ElderIcelordSuttung : GameEpicBoss
{
    public ElderIcelordSuttung() : base()
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
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
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
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160395);
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

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 573, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.Standard);
        ElderIcelordSuttungBrain.message1 = false;
        ElderIcelordSuttungBrain.message2 = false;
        SuttungCount = 1;

        VisibleActiveWeaponSlots = 16;
        ElderIcelordSuttungBrain sbrain = new ElderIcelordSuttungBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = true; 
        base.AddToWorld();
        return true;
    }
    public static int SuttungCount = 0;
    public override void Die(GameObject killer)
    {
        SuttungCount = 0;
        base.Die(killer);
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (Util.Chance(15))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle) && !ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
                CastSpell(SuttungDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
    }
    private Spell m_SuttungDisease;
    private Spell SuttungDisease
    {
        get
        {
            if (m_SuttungDisease == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 731;
                spell.Icon = 731;
                spell.Name = "Valnir Mordeth's Plague";
                spell.Message1 = "You are diseased!";
                spell.Message2 = "{0} is diseased!";
                spell.Message3 = "You look healthy.";
                spell.Message4 = "{0} looks healthy again.";
                spell.TooltipId = 731;
                spell.Range = 400;
                spell.Duration = 60;
                spell.SpellID = 11928;
                spell.Target = "Enemy";
                spell.Type = "Disease";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
                m_SuttungDisease = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SuttungDisease);
            }
            return m_SuttungDisease;
        }
    }
}
#endregion Elder Icelord Suttung

#region Hjalmar and Suttung Controller
public class HjalmarSuttungController : GameNpc
{
    public HjalmarSuttungController() : base()
    {
    }
    public override bool IsVisibleToPlayers => true;
    public override bool AddToWorld()
    {
        Name = "HjalmarSuttung Controller";
        GuildName = "DO NOT REMOVE";
        Level = 50;
        Model = 665;
        RespawnInterval = 5000;
        Flags = (ENpcFlags)28;
        SpawnBoss();

        HjalmarSuttungControllerBrain sbrain = new HjalmarSuttungControllerBrain();
        SetOwnBrain(sbrain);
        base.AddToWorld();
        return true;
    }
    private void SpawnBoss()
    {
        switch (Util.Random(1, 2))
        {
            case 1: SpawnSuttung(); break;
            case 2: SpawnHjalmar(); break;
        }
    }
    private void SpawnSuttung()
    {
        if (ElderIcelordSuttung.SuttungCount == 0)
        {
            ElderIcelordSuttung boss = new ElderIcelordSuttung();
            boss.X = 32055;
            boss.Y = 54253;
            boss.Z = 11883;
            boss.Heading = 2084;
            boss.CurrentRegion = CurrentRegion;
            boss.AddToWorld();
            HjalmarSuttungControllerBrain.Spawn_Boss = false;
        }
    }
    private void SpawnHjalmar()
    {
        if (ElderIcelordHjalmar.HjalmarCount == 0)
        {
            ElderIcelordHjalmar boss = new ElderIcelordHjalmar();
            boss.X = 32079;
            boss.Y = 53415;
            boss.Z = 11885;
            boss.Heading = 21;
            boss.CurrentRegion = CurrentRegion;
            boss.AddToWorld();
            HjalmarSuttungControllerBrain.Spawn_Boss = false;
        }
    }
}
#endregion Hjalmar and Suttung Controller