using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS;

public class Silencer : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Silencer()
        : base()
    {
    }
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100  * ServerProperty.EPICS_DMG_MULTIPLIER;
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

    public static List<GamePlayer> attackers = new List<GamePlayer>();
    public static int attackers_count = 0;
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            attackers.Add(source as GamePlayer);
            attackers_count = attackers.Count / 10;

            if (Util.Chance(attackers_count))
            {
                if (resist_timer == false)
                {
                    BroadcastMessage(String.Format(this.Name + " becomes almost immune to any damage for short time!"));
                    new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(ResistTime), 2000);
                    resist_timer = true;
                }
            }
        }
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
    }

    public override int GetResist(EDamageType damageType)
    {
        if (get_resist)
        {
            switch (damageType)
            {
                case EDamageType.Slash:
                case EDamageType.Crush:
                case EDamageType.Thrust: return 99; //99% dmg reduction for melee dmg
                default: return 99; // 99% reduction for rest resists
            }
        }
        else
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 30;
                case EDamageType.Crush: return 30;
                case EDamageType.Thrust: return 30; //30% dmg reduction for melee dmg
                default: return 50; // 50% reduction for rest resists
            }
        }
    }

    public static bool get_resist = false; //set resists
    public static bool resist_timer = false;
    public static bool resist_timer_end = false;
    public static bool spam1 = false;

    public int ResistTime(EcsGameTimer timer)
    {
        get_resist = true;
        spam1 = false;
        if (resist_timer == true && resist_timer_end == false)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(this, this, 9103, 0, false, 0x01);
            }
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(ResistTimeEnd), 20000); //20s resist 99%
            resist_timer_end = true;
        }
        return 0;
    }
    public int ResistTimeEnd(EcsGameTimer timer)
    {
        get_resist = false;
        resist_timer = false;
        resist_timer_end = false;
        attackers.Clear();
        attackers_count = 0;
        if (spam1 == false)
        {
            BroadcastMessage(String.Format(this.Name + " resists fades away!"));
            spam1 = true;
        }
        return 0;
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166029);
        LoadTemplate(npcTemplate);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        attackers_count = 0;
        get_resist = false;
        resist_timer = false;
        resist_timer_end = false;
        spam1 = false;

        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        SilencerBrain adds = new SilencerBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;//load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
}