using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Fornfrusenen
public class Fornfrusenen : GameEpicBoss
{
    public Fornfrusenen() : base()
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
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
        }
    }
    public override void Die(GameObject killer) //on kill generate orbs
    {
        foreach (GameNpc npc in GetNPCsInRadius(4000))
        {
            if (npc != null && npc.IsAlive && npc.Brain is FornShardBrain)
                npc.RemoveFromWorld();
        }
        BroadcastMessage(String.Format("The frosty glows in {0}'s eyes abruptly blinks out. {0}'s form slowly fades into the ice. The shard swiftly evaporate leaving no trace of their corporeal existence behind!", Name));
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161047);
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
        MaxSpeedBase = 0;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds


        FornfrusenenBrain sbrain = new FornfrusenenBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
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
            {
                player.Out.SendSpellEffectAnimation(this, this, 6160, 0, false, 0x01);//left hand glow
                player.Out.SendSpellEffectAnimation(this, this, 6161, 0, false, 0x01);//right hand glow
            }

            return 3000;
        }

        return 0;
    }

    //boss does not move so he will not take damage if enemys hit him from far away
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (!source.IsWithinRadius(this, 200)) //take no damage
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
            else //take dmg
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
}
#endregion Fornfrusenen

#region Fornfrusenen Shards
public class FornfrusenenShard : GameNpc
{
    public FornfrusenenShard() : base()
    {
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        Point3D point = new Point3D(49617, 32874, 10859);
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (!source.IsWithinRadius(point, 400)) //take no damage
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System,
                        EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
            else //take dmg
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
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
        return base.AttackDamage(weapon) * Strength / 80 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }

    public override int AttackRange
    {
        get { return 350; }
        set { }
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
        get { return 50000; }
    }

    public override void Die(GameObject killer)
    {
        foreach (GameNpc boss in GetNPCsInRadius(3000))
        {
            if (boss != null && boss.IsAlive && boss.Brain is FornfrusenenBrain)
            {
                if (boss.HealthPercent <= 100 && boss.HealthPercent > 35) //dont dmg boss if is less than 35%
                    boss.Health -= boss.MaxHealth / 4; //deal dmg to boss if this is killed
            }
        }
        base.Die(killer);
    }
    #region Stats
    public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 100; }
    public override short Strength { get => base.Strength; set => base.Strength = 120; }
    #endregion
    public override bool AddToWorld()
    {
        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        Name = "Fornfrusenen Shard";
        Level = 75;
        Model = 920;
        Realm = 0;
        Size = (byte) Util.Random(30, 40);
        MeleeDamageType = EDamageType.Cold;

        RespawnInterval = -1;
        MaxSpeedBase = 200; 

        FornShardBrain sbrain = new FornShardBrain();
        SetOwnBrain(sbrain);
        base.AddToWorld();
        return true;
    }
    public override void OnAttackEnemy(AttackData ad) //on enemy actions
    {
        if (Util.Chance(20))
        {
            if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
                CastSpell(FornShardDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
    }
    private Spell m_FornShardDD;
    public Spell FornShardDD
    {
        get
        {
            if (m_FornShardDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.Power = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 14323;
                spell.Icon = 11266;
                spell.Damage = 300;
                spell.DamageType = (int)EDamageType.Cold;
                spell.Name = "Frost Shock";
                spell.Range = 500;
                spell.Radius = 300;
                spell.SpellID = 11924;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                m_FornShardDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FornShardDD);
            }
            return m_FornShardDD;
        }
    }
}
#endregion