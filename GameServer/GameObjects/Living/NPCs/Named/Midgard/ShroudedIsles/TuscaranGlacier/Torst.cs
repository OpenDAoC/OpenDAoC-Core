using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

#region Torst
public class Torst : GameEpicBoss
{
    public Torst() : base()
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
    public override void ReturnToSpawnPoint(short speed)
    {
        return;
    }
    #region Stats
    public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 350; }
    #endregion
    public override bool AddToWorld()
    {
        Name = "Torst";
        Level = 80;
        Size = 90;
        Model = 696;
        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        MaxSpeedBase = 250;
        Flags = ENpcFlags.FLYING;
        RespawnInterval =ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        TorstBrain sbrain = new TorstBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(5000))
        {
            if (npc != null && npc.IsAlive && npc.Brain is TorstEddiesBrain)
                npc.RemoveFromWorld();
        }
        base.Die(killer);
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (Util.Chance(20))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
                CastSpell(TorstDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
    }
    public Spell m_TorstDD;
    public Spell TorstDD
    {
        get
        {
            if (m_TorstDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = Util.Random(25, 45);
                spell.ClientEffect = 228;
                spell.Icon = 208;
                spell.TooltipId = 479;
                spell.Damage = 550;
                spell.Range = 500;
                spell.Radius = 400;
                spell.SpellID = 11743;
                spell.Target = "Enemy";
                spell.Type = "DirectDamageNoVariance";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_TorstDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TorstDD);
            }
            return m_TorstDD;
        }
    }
}
#endregion Torst

#region Torst edds
public class TorstEddies : GameNpc
{
    public TorstEddies() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 15;// dmg reduction for melee dmg
            case EDamageType.Crush: return 15;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 15;// dmg reduction for melee dmg
            default: return 15;// dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override int MaxHealth
    {
        get { return 10000; }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.10;
    }
    public override void StopFollowing()
    {
        if (IsAlive)
            return;
        base.StopFollowing();
    }
    public override void Follow(GameObject target, int minDistance, int maxDistance)
    {
        if (IsAlive)
            return;
        base.Follow(target, minDistance, maxDistance);
    }
    public override void ReturnToSpawnPoint(short speed)
    {
        if (IsAlive)
            return;
        base.ReturnToSpawnPoint(speed);
    }
    public override void StartAttack(GameObject target)
    {
    }
    #region Stats
    public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    #endregion
    public override bool AddToWorld()
    {
        Model = 665;
        Name = "eddie";
        Level = (byte)Util.Random(55, 58);
        Size = 50;
        RespawnInterval = -1;
        Flags = (ENpcFlags)44;//noname notarget flying
        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        MaxSpeedBase = 300;

        LoadedFromScript = true;
        TorstEddiesBrain sbrain = new TorstEddiesBrain();
        SetOwnBrain(sbrain);
        bool success = base.AddToWorld();
        if (success)
        {
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
        }
        return success;
    }

    protected int Show_Effect(EcsGameTimer timer)
    {
        if (IsAlive)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player?.Out.SendSpellEffectAnimation(this, this, 4168, 0, false, 0x01);

            return 1600;
        }

        return 0;
    }

    public override void Die(GameObject killer)
    {
        base.Die(killer);
    }
}
#endregion Torst adds