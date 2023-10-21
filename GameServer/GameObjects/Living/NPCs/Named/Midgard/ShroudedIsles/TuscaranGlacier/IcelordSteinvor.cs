using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;

namespace Core.GS;

#region Icelord Steinvor
public class IcelordSteinvor : GameEpicBoss
{
    public IcelordSteinvor() : base()
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
    public override void Die(GameObject killer) //on kill generate orbs
    {
        SpawnSeers();
        base.Die(killer);
    }
    public void SpawnSeers()
    {
        for (int i = 0; i < 2; i++)
        {
            HrimthursaSeer Add1 = new HrimthursaSeer();
            Add1.X = 29996 + Util.Random(-100, 100);
            Add1.Y = 52911 + Util.Random(-100, 100);
            Add1.Z = 11890;
            Add1.CurrentRegion = this.CurrentRegion;
            Add1.Heading = 2032;
            Add1.PackageID = "SteinvorDeathAdds";
            Add1.RespawnInterval = -1;
            Add1.AddToWorld();
        }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162350);
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
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Giant;
        IcelordSteinvorBrain.PlayerX = 0;
        IcelordSteinvorBrain.PlayerY = 0;
        IcelordSteinvorBrain.PlayerZ = 0;
        IcelordSteinvorBrain.RandomTarget = null;
        IcelordSteinvorBrain.PickedTarget = false;

        IcelordSteinvorBrain sbrain = new IcelordSteinvorBrain();
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
        npcs = WorldMgr.GetNPCsByNameFromRegion("Icelord Steinvor", 160, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Icelord Steinvor not found, creating it...");

            log.Warn("Initializing Icelord Steinvor ...");
            IcelordSteinvor TG = new IcelordSteinvor();
            TG.Name = "Icelord Steinvor";
            TG.Model = 918;
            TG.Realm = 0;
            TG.Level = 80;
            TG.Size = 70;
            TG.CurrentRegionID = 160; //tuscaran glacier
            TG.MeleeDamageType = EDamageType.Crush;
            TG.RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                60000; //1min is 60000 miliseconds
            TG.Faction = FactionMgr.GetFactionByID(140);
            TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            TG.BodyType = (ushort)EBodyType.Giant;

            TG.X = 25405;
            TG.Y = 57241;
            TG.Z = 11359;
            TG.Heading = 1939;
            IcelordSteinvorBrain ubrain = new IcelordSteinvorBrain();
            TG.SetOwnBrain(ubrain);
            TG.AddToWorld();
            TG.SaveIntoDatabase();
            TG.Brain.Start();
        }
        else
            log.Warn("Icelord Steinvor exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Icelord Steinvor

#region Mob adds
public class HrimthursaSeer : GameEpicNPC
{
    public HrimthursaSeer() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 35; // dmg reduction for melee dmg
            case EDamageType.Crush: return 35; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
            default: return 35; // dmg reduction for rest resists
        }
    }

    public override void ReturnToSpawnPoint(short speed)
    {
        return;
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
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override int MaxHealth
    {
        get { return 20000; }
    }
    public override void Die(GameObject killer)
    {
        base.Die(killer);
    }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 150; }
    public override bool AddToWorld()
    {
        Model = 918;
        MeleeDamageType = EDamageType.Crush;
        Name = "hrimthursa seer";
        MaxDistance = 3500;
        TetherRange = 3800;
        Size = 60;
        Level = (byte)Util.Random(73, 75);
        MaxSpeedBase = 270;

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        BodyType = (ushort)EBodyType.Giant;
        Realm = ERealm.None;
        RespawnInterval = -1;

        HrimthursaSeerBrain.walkto_point = false;
        HrimthursaSeerBrain adds = new HrimthursaSeerBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Mob adds

#region Effect Mob
public class EffectMob : GameEpicNPC
{
    public EffectMob() : base()
    {
    }
    public override void StartAttack(GameObject target)
    {
    }
    public int Show_Effect(EcsGameTimer timer)
    {
        if (IsAlive)
        {
            foreach (GamePlayer player in this.GetPlayersInRadius(8000))
            {
                if (player != null)
                    player.Out.SendSpellEffectAnimation(this, this, 177, 0, false, 0x01);
            }
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoCast), 500);
        }
        return 0;
    }

    public int DoCast(EcsGameTimer timer)
    {
        if (IsAlive)
        {
            GroundTarget.X = X;
            GroundTarget.Y = Y;
            GroundTarget.Z = Z;
            CastSpell(Icelord_Gtaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        return 0;
    }

    public override bool AddToWorld()
    {
        Model = 665;
        Size = 70;
        MaxSpeedBase = 0;
        Name = "Pillar of Ice";
        Level = 80;
        Flags = ENpcFlags.DONTSHOWNAME;
        Flags = ENpcFlags.CANTTARGET;
        Flags = ENpcFlags.STATUE;

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        Realm = ERealm.None;
        RespawnInterval = -1;

        EffectMobBrain adds = new EffectMobBrain();
        SetOwnBrain(adds);
        bool success = base.AddToWorld();
        if (success)
        {
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 3000);
        }
        return success;
    }

    private Spell m_Icelord_Gtaoe;
    private Spell Icelord_Gtaoe
    {
        get
        {
            if (m_Icelord_Gtaoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 10;
                spell.ClientEffect = 208;
                spell.Icon = 208;
                spell.TooltipId = 234;
                spell.Damage = 750;
                spell.Name = "Pillar of Frost";
                spell.Radius = 350;
                spell.Range = 1800;
                spell.SpellID = 11747;
                spell.Target = "Area";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_Icelord_Gtaoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Icelord_Gtaoe);
            }
            return m_Icelord_Gtaoe;
        }
    }
}
#endregion Effect Mob