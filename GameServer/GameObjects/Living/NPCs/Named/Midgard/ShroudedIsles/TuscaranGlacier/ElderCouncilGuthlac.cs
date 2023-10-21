using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS;

#region Elder Council Guthlac
public class ElderCouncilGuthlac : GameEpicBoss
{
    public ElderCouncilGuthlac() : base()
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
        foreach (GameNpc npc in this.GetNPCsInRadius(5000))
        {
            if (npc != null)
            {
                if (npc.IsAlive && npc.Brain is FrozenBombBrain)
                    npc.Die(this);
            }
        }
        base.Die(killer);
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160392);
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

        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 19, 0);
        Inventory = template.CloseTemplate();
        SwitchWeapon(EActiveWeaponSlot.TwoHanded);

        ElderCouncilGuthlacBrain.message1 = false;
        ElderCouncilGuthlacBrain.IsBombUp = false;
        ElderCouncilGuthlacBrain.RandomTarget = null;
        ElderCouncilGuthlacBrain.IsPulled2 = false;

        VisibleActiveWeaponSlots = 34;
        MeleeDamageType = EDamageType.Crush;
        ElderCouncilGuthlacBrain sbrain = new ElderCouncilGuthlacBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
}
#endregion Elder Council Guthlac

#region Frost Bomb

public class FrozenBomb : GameNpc
{
    public FrozenBomb() : base()
    {
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 15; // dmg reduction for melee dmg
            case EDamageType.Crush: return 15; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 15; // dmg reduction for melee dmg
            case EDamageType.Cold: return 99; // almost immune to cold dmg
            default: return 15; // dmg reduction for rest resists
        }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }

    protected int Show_Effect(EcsGameTimer timer)
    {
        if (IsAlive)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(this, this, 177, 0, false, 0x01);

            return 3000;
        }

        return 0;
    }

    protected int Explode(EcsGameTimer timer)
    {
        if (IsAlive && TargetObject != null)
        {
            //SetGroundTarget(X, Y, Z);
            CastSpell(GuthlacIceSpike_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(KillBomb), 1000);
        }
        return 0;
    }
    public int KillBomb(EcsGameTimer timer)
    {
        if (IsAlive)
            Die(this);
        return 0;
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
    public static int FrozenBombCount = 0;
    public override void Die(GameObject killer)
    {
        FrozenBombCount = 0;
        base.Die(null);
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public override bool AddToWorld()
    {
        Model = 665;
        Size = 100;
        MaxSpeedBase = 0;
        FrozenBombCount = 1;
        Name = "Ice Spike";
        Level = (byte)Util.Random(62, 66);

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        Realm = ERealm.None;
        RespawnInterval = -1;

        FrozenBombBrain adds = new FrozenBombBrain();
        SetOwnBrain(adds);
        bool success = base.AddToWorld();
        if (success)
        {
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Explode), 25000); //25 seconds until this will explode and deal heavy cold dmg
        }
        return success;
    }

    private Spell m_GuthlacIceSpike_aoe;
    private Spell GuthlacIceSpike_aoe
    {
        get
        {
            if (m_GuthlacIceSpike_aoe == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 20;
                spell.ClientEffect = 208;
                spell.Icon = 208;
                spell.TooltipId = 208;
                spell.Damage = 2500;
                spell.Name = "Ice Bomb";
                spell.Radius = 3000; //very big radius to make them feel pain lol
                spell.Range = 0;
                spell.SpellID = 11751;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_GuthlacIceSpike_aoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GuthlacIceSpike_aoe);
            }
            return m_GuthlacIceSpike_aoe;
        }
    }
}
#endregion