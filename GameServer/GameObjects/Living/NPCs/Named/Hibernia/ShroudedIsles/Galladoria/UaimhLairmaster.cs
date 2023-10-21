using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.PacketHandler;

namespace Core.GS.Scripts;

public class UaimhLairmaster : GameEpicBoss
{
    protected String m_FleeingAnnounce;
    public static bool IsFleeing = true;

    public UaimhLairmaster() : base()
    {
        m_FleeingAnnounce = "{0} starts fleeing!";
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
    public override bool AddToWorld()
    {
        Model = 844;
        Name = "Uaimh Lairmaster";
        Size = 60;
        Level = 81;
        Gender = EGender.Neutral;

        BodyType = 6; // Humanoid
        RoamingRange = 0;
        MaxSpeedBase = 300;

        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167362);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        SaveIntoDatabase();
        LoadedFromScript = false;
        base.AddToWorld();
        base.SetOwnBrain(new UaimhLairmasterBrain());
        return true;
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }

    public override int MaxHealth
    {
        get { return 100000; }
    }

    public override int AttackRange
    {
        get { return 450; }
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

    /// <summary>
    /// Take some amount of damage inflicted by another GameObject.
    /// </summary>
    /// <param name="source">The object inflicting the damage.</param>
    /// <param name="damageType">The type of damage.</param>
    /// <param name="damageAmount">The amount of damage inflicted.</param>
    /// <param name="criticalAmount">The critical amount of damage inflicted</param>
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        Brain.Notify(GameObjectEvent.TakeDamage, this,
            new TakeDamageEventArgs(source, damageType, damageAmount, criticalAmount));
    }

    /// <summary>
    /// Take action upon someone healing the enemy.
    /// </summary>
    /// <param name="enemy">The living that was healed.</param>
    /// <param name="healSource">The source of the heal.</param>
    /// <param name="changeType">The way the living was healed.</param>
    /// <param name="healAmount">The amount that was healed.</param>
    public override void EnemyHealed(GameLiving enemy, GameObject healSource, EHealthChangeType changeType,
        int healAmount)
    {
        base.EnemyHealed(enemy, healSource, changeType, healAmount);
        Brain.Notify(GameLivingEvent.EnemyHealed, this,
            new EnemyHealedEventArgs(enemy, healSource, changeType, healAmount));
    }

    #region Tether

    /// <summary>
    /// Return to spawn point, Uaimh Lairmaster can't be attacked while it's
    /// on it's way.
    /// </summary>
    public override void ReturnToSpawnPoint(short speed)
    {
        UaimhLairmasterBrain brain = new UaimhLairmasterBrain();
        StopAttack();
        StopFollowing();
        brain.AggroTable.Clear();
        base.ReturnToSpawnPoint(MaxSpeed);
    }

    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsReturningToSpawnPoint)
            return;

        base.OnAttackedByEnemy(ad);
    }

    #region Broadcast Message

    /// <summary>
    /// Broadcast relevant messages to the raid.
    /// </summary>
    /// <param name="message">The message to be broadcast.</param>
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }

    #endregion

    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);

        if (e == GameObjectEvent.TakeDamage)
        {
            if (CheckHealth())
                return;
        }
    }

    #endregion

    #region Health Check

    /// <summary>
    /// Actions to be taken into consideration when health drops.
    /// </summary>
    /// <returns>Whether any action was taken.</returns>
    public bool CheckHealth()
    {
        if (HealthPercent <= 60 && IsFleeing)
        {
            BroadcastMessage(string.Format(m_FleeingAnnounce, Name));
            ReturnToSpawnPoint(NpcMovementComponent.DEFAULT_WALK_SPEED);
            IsFleeing = false;
            return true;
        }

        return false;
    }

    #endregion

    public override void Die(GameObject killer)
    {
        IsFleeing = true;
        base.Die(killer);
    }
}