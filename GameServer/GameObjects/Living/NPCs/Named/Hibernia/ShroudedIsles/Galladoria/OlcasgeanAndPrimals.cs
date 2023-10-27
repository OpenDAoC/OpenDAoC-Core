using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Players;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

#region Olcasgean Initializor
public class OlcasgeanInitializer : GameNpc
{
    public OlcasgeanInitializer() : base() { }
    public static GameNpc Olcasgean_Initializator = new GameNpc();
    public override int MaxHealth
    {
        get { return 10000; }
    }
    public override void DropLoot(GameObject killer)//no loot
    {
    }
    public override void Die(GameObject killer)
    {
        base.Die(null); // null to not gain experience
    }
    public override bool AddToWorld()
    {
        Name = "Olcasgean Initializator";
        GuildName = "DO NOT REMOVE!";
        Model = 665;
        Realm = 0;
        Level = 50;
        Size = 50;
        CurrentRegionID = 191;//galladoria
        Flags = (ENpcFlags)60;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        X = 41116;
        Y = 64419;
        Z = 12746;
        OlcasgeanInitializerBrain ubrain = new OlcasgeanInitializerBrain();
        SetOwnBrain(ubrain);
        base.AddToWorld();
        return true;
    }
}
#endregion Olcasgean Initializor

#region Olcasgean
public class Olcasgean : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public Olcasgean()
        : base()
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
    public override int MaxHealth
    {
        get { return 250000; }
    }
    public override int AttackRange
    {
        get { return 1500; }
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
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        int damageDealt = damageAmount + criticalAmount;
        foreach (GameNpc copy in GetNPCsInRadius(10000))
        {
            if (copy != null)
            {
                if (copy is Olcasgean2 && copy.IsAlive)
                {
                    copy.Health = Health;
                }
            }
        }
    }
    #region Custom Methods
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }
    protected void ReportNews(GameObject killer)
    {
        int numPlayers = AwardEpicEncounterKillPoint();
        String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
        NewsMgr.CreateNews(message, killer.Realm, ENewsType.PvE, true);

        if (ServerProperty.GUILD_MERIT_ON_DRAGON_KILL > 0)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player.IsEligibleToGiveMeritPoints)
                {
                    GuildEventHandler.MeritForNPCKilled(player, this, ServerProperty.GUILD_MERIT_ON_DRAGON_KILL);
                }
            }
        }
    }
    protected int AwardEpicEncounterKillPoint()
    {
        int count = 0;
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.KillsEpicBoss++;
            player.Achieve(AchievementUtil.AchievementName.Epic_Boss_Kills);
            count++;
        }
        return count;
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(10000))
        {
            if (npc != null)
            {
                if (npc.IsAlive)
                {
                    if (npc is Olcasgean2 boss)
                    {
                        if (boss != null && boss.IsAlive && boss.Brain is OlcasgeanBrain2)
                            boss.RemoveFromWorld();
                    }

                    if (npc.Brain is VortexBrain)
                        npc.RemoveFromWorld();

                    if (npc.Brain is WaterfallAntipassBrain)
                        npc.RemoveFromWorld();
                }
            }
        }
        OlcasgeanInitializerBrain.DeadPrimalsCount = 0;

        bool canReportNews = true;
        // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

            if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
            {
                if (player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
                    canReportNews = false;
            }
        }
        if (canReportNews)
        {
            if (killer is not Olcasgean or Olcasgean2)
                ReportNews(killer);
        }
        base.Die(killer);
    }
    #endregion
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164624);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;            

        X = 39237;
        Y = 62644;
        Z = 11685;
        Heading = 102;
        CurrentRegionID = 191;         

        Flags = (ENpcFlags)156;
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
        OlcasgeanInitializerBrain.startevent = false;
        OlcasgeanInitializerBrain.DeadPrimalsCount = 0;
        OlcasgeanBrain.setbossflags = false;
        OlcasgeanBrain.wake_up_boss = false;
        OlcasgeanBrain.Spawn_Copy = false;
        foreach (GameNpc npc in GetNPCsInRadius(5500))
        {
            if (npc != null && npc.IsAlive)
            {
                if (npc.Brain is WaterPrimalBrain || npc.Brain is AirPrimalBrain || npc.Brain is FirePrimalBrain || npc.Brain is EarthPrimalBrain
                    || npc.Brain is GuardianEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is NaturalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain || npc.Brain is OlcasgeanBrain2)
                {
                    npc.RemoveFromWorld();
                }
            }
        }
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        OlcasgeanBrain sBrain = new OlcasgeanBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }

    public override void OnAttackedByEnemy(AttackData ad)// on Boss being attacked
    {
        if (ad != null && ad.Damage > 0 && ad.Attacker != null && ad.Attacker.IsAlive && ad.Attacker is GamePlayer)
        {
            if (HealthPercent <= 50)
            {
                if (Util.Chance(50))
                    CastSpell(OlcasgeanDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            if (HealthPercent > 50)
                if (Util.Chance(25))
                    CastSpell(OlcasgeanDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackedByEnemy(ad);
    }
    public Spell m_OlcasgeanDD;
    public Spell OlcasgeanDD
    {
        get
        {
            if (m_OlcasgeanDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 1;
                spell.ClientEffect = 11027;
                spell.Icon = 11027;
                spell.TooltipId = 11027;
                spell.Name = "Olcasgean's Root";
                spell.Damage = 450;
                spell.Range = 1800;
                spell.SpellID = 11901;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.DamageType = (int)EDamageType.Matter;
                m_OlcasgeanDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OlcasgeanDD);
            }
            return m_OlcasgeanDD;
        }
    }
}
#endregion Olcasgean

#region Olcasgean 2
public class Olcasgean2 : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public Olcasgean2()
        : base()
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
    public override int MaxHealth
    {
        get { return 250000; }
    }
    public override int AttackRange
    {
        get { return 1500; }
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
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        int damageDealt = damageAmount + criticalAmount;
        foreach (GameNpc copy in GetNPCsInRadius(10000))
        {
            if (copy != null)
            {
                if (copy is Olcasgean && copy.IsAlive)
                {
                    copy.Health = Health;
                }
            }
        }
    }
    #region Custom Methods
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }
    protected void ReportNews(GameObject killer)
    {
        int numPlayers = AwardEpicEncounterKillPoint();
        String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
        NewsMgr.CreateNews(message, killer.Realm, ENewsType.PvE, true);

        if (ServerProperty.GUILD_MERIT_ON_DRAGON_KILL > 0)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player.IsEligibleToGiveMeritPoints)
                {
                    GuildEventHandler.MeritForNPCKilled(player, this, ServerProperty.GUILD_MERIT_ON_DRAGON_KILL);
                }
            }
        }
    }
    protected int AwardEpicEncounterKillPoint()
    {
        int count = 0;
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.KillsEpicBoss++;
            player.Achieve(AchievementUtil.AchievementName.Epic_Boss_Kills);
            count++;
        }
        return count;
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(10000))
        {
            if (npc != null)
            {
                if (npc.IsAlive)
                {
                    if (npc is Olcasgean boss)
                    {
                        if (boss != null && boss.IsAlive && boss.Brain is OlcasgeanBrain)
                            boss.Die(boss);
                    }

                    if (npc.Brain is VortexBrain)
                        npc.RemoveFromWorld();

                    if (npc.Brain is WaterfallAntipassBrain)
                        npc.RemoveFromWorld();
                }
            }
        }

        bool canReportNews = true;
        // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

            if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
            {
                if (player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
                    canReportNews = false;
            }
        }
        if (canReportNews)
        {
            if(killer is not Olcasgean or Olcasgean2)
                ReportNews(killer);
        }
        base.Die(killer);
    }
    #endregion
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164624);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;

        OlcasgeanBrain2.wake_up_boss2 = false;
        Flags = (ENpcFlags)156;
        LoadedFromScript = true;
        RespawnInterval = -1;

        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        OlcasgeanBrain2 sBrain = new OlcasgeanBrain2();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }

    public override void OnAttackedByEnemy(AttackData ad)// on Boss being attacked
    {
        if (ad != null && ad.Damage > 0 && ad.Attacker != null && ad.Attacker.IsAlive && ad.Attacker is GamePlayer)
        {
            if (HealthPercent <= 50)
            {
                if (Util.Chance(50))
                    CastSpell(OlcasgeanDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            if (HealthPercent > 50)
                if (Util.Chance(25))
                    CastSpell(OlcasgeanDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackedByEnemy(ad);
    }
    private Spell m_OlcasgeanDD2;
    private Spell OlcasgeanDD2
    {
        get
        {
            if (m_OlcasgeanDD2 == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 1;
                spell.ClientEffect = 11027;
                spell.Icon = 11027;
                spell.TooltipId = 11027;
                spell.Name = "Olcasgean's Root";
                spell.Damage = 450;
                spell.Range = 1800;
                spell.SpellID = 12011;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.DamageType = (int)EDamageType.Matter;
                m_OlcasgeanDD2 = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OlcasgeanDD2);
            }
            return m_OlcasgeanDD2;
        }
    }
}
#endregion Olcasgean 2

#region Air Primal
public class AirPrimal : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public AirPrimal()
        : base()
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
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GameSummonedPet || source is TurretPet)
        {
            base.TakeDamage(source, damageType, 5, 5);//pets deal less dmg to this primal to avoid being killed to fast
        }
        else//take dmg
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
    }
    public override void StartAttack(GameObject target)
    {
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
        get
        {
            return 900;//low health, as source says 1 volcanic pillar 5 could one shot it
        }
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override void ReturnToSpawnPoint(short speed)
    {
        if (IsAlive)
            return;
        base.ReturnToSpawnPoint(speed);
    }
    public override void Follow(GameObject target, int minDistance, int maxDistance)
    {
    }
    public override void StopFollowing()
    {
    }      
    public override void Die(GameObject killer)
    {
        ++OlcasgeanInitializerBrain.DeadPrimalsCount;
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159435);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        RespawnInterval = -1;//will not respawn
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        Flags = ENpcFlags.FLYING;

        AirPrimalBrain sBrain = new AirPrimalBrain();
        SetOwnBrain(sBrain);
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Air Primal

#region Water Primal
public class WaterPrimal : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public WaterPrimal()
        : base()
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
    public override void Die(GameObject killer)
    {
        ++OlcasgeanInitializerBrain.DeadPrimalsCount;
        base.Die(killer);
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
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
        get
        {
            return 125000;
        }
    }

    public override int AttackRange
    {
        get
        {
            return 350;
        }
        set
        {
        }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (WaterPrimalBrain.dontattack)//dont take any dmg 
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                    || damageType == EDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(this.Name + " is under waterfall effect!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            else//take dmg
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159438);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;

        WaterPrimalBrain.message = false;
        WaterPrimalBrain.lowhealth1 = false;
        WaterPrimalBrain.dontattack = false;
        WaterPrimalBrain.TeleportTarget = null;
        WaterPrimalBrain.IsTargetTeleported = false;

        CurrentRegionID = 191;//galladoria
        Flags ^= ENpcFlags.GHOST;//ghost

        RespawnInterval = -1;//will not respawn
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        WaterPrimalBrain sBrain = new WaterPrimalBrain();
        SetOwnBrain(sBrain);
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Water Primal

#region Fire Primal
public class FirePrimal : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public FirePrimal()
        : base()
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
    public override void StartAttack(GameObject target)
    {
    }
    public override void ReturnToSpawnPoint(short speed)
    {
        return;
    }
    public override void Die(GameObject killer)
    {
        ++OlcasgeanInitializerBrain.DeadPrimalsCount;
        base.Die(killer);
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
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
        get
        {
            return 125000;
        }
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159437);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        FirePrimalBrain.CanSpawnFire = false;

        Flags ^= ENpcFlags.FLYING;//flying
        RespawnInterval = -1;//will not respawn
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

        FirePrimalBrain sBrain = new FirePrimalBrain();
        SetOwnBrain(sBrain);
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Fire Primal

#region Trail of Fire
public class TrailOfFire : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public TrailOfFire()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 99; // dmg reduction for melee dmg
            case EDamageType.Crush: return 99; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 99; // dmg reduction for melee dmg
            default: return 99; // dmg reduction for rest resists
        }
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 800;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.55;
    }
    public override int MaxHealth
    {
        get
        {
            return 10000;
        }
    }

    private int Show_Effect(EcsGameTimer timer)
    {
        if (IsAlive)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(this, this, 5906, 0, false, 0x01);

            SetGroundTarget(X, Y, Z);

            if (!IsCasting)
                CastSpell(FireGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);

            return 2000;
        }

        return 0;
    }

    private int RemoveFire(EcsGameTimer timer)
    {
        if (IsAlive)
            RemoveFromWorld();
        return 0;
    }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
    public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
    public override bool AddToWorld()
    {
        Model = 2000;
        Name = "trail of fire";
        Flags ^= ENpcFlags.DONTSHOWNAME;
        Flags ^= ENpcFlags.CANTTARGET;
        //Flags ^= eFlags.STATUE;
        MaxSpeedBase = 0;
        Level = 80;
        Size = 10;

        RespawnInterval = -1;//will not respawn
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

        TrailOfFireBrain sBrain = new TrailOfFireBrain();
        SetOwnBrain(sBrain);
        //Brain.Start();
        bool success = base.AddToWorld();
        if (success)
        {
            SetGroundTarget(X, Y, Z);
            if (!IsCasting)
                CastSpell(FireGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(RemoveFire), 6000);
        }
        return success;
    }
    private Spell m_FireGroundDD;
    private Spell FireGroundDD
    {
        get
        {
            if (m_FireGroundDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 2;
                spell.ClientEffect = 368;
                spell.Icon = 368;
                spell.TooltipId = 368;
                spell.Damage = 220;
                spell.Range = 1200;
                spell.Radius = 450;
                spell.SpellID = 11866;
                spell.Target = "Area";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Heat;
                m_FireGroundDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireGroundDD);
            }
            return m_FireGroundDD;
        }
    }
}
#endregion Trail of Fire

#region Earth Primal
public class EarthPrimal : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public EarthPrimal()
        : base()
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
    public override void Die(GameObject killer)
    {
        ++OlcasgeanInitializerBrain.DeadPrimalsCount;
        foreach (GameNpc npc in GetNPCsInRadius(8000))
        {
            if (npc != null)
            {
                if (npc.IsAlive)
                {
                    if (npc.Brain is GuardianEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is NaturalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain)
                        npc.Die(null);
                }
            }
        }
        base.Die(killer);
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
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
        get { return 125000; }
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159436);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        TetherRange = 890;

        RespawnInterval = -1;//will not respawn
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

        EarthPrimalBrain sBrain = new EarthPrimalBrain();
        SetOwnBrain(sBrain);
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Earth Primal

#region Guardian Earthmender
public class GuardianEarthmender : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public GuardianEarthmender()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer)
        {
            GamePlayer truc = source as GamePlayer;

            if (truc.PlayerClass.ID == 43 || truc.PlayerClass.ID == 44 || truc.PlayerClass.ID == 45 || truc.PlayerClass.ID == 56 || truc.PlayerClass.ID == 55)// bm,hero,champ,vw,ani
            {
                if (source is GamePlayer)
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
            else
            {
                truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
        if (source is GameSummonedPet)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get
        {
            return 60000;
        }
    }
    public override bool AddToWorld()
    {
        Model = 951;
        Name = "Guardian Earthmender";
        Size = 150;
        Level = 73;
        Realm = 0;
        CurrentRegionID = 191;//galladoria
        MaxSpeedBase = 0;

        RespawnInterval = -1;//will not respawn
        Gender = EGender.Neutral;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        MeleeDamageType = EDamageType.Slash;
        BodyType = 5;

        GuardianEarthmenderBrain sBrain = new GuardianEarthmenderBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Guardian Earthmender

#region Magical Earthmender
public class MagicalEarthmender : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public MagicalEarthmender()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer)
        {
            GamePlayer truc = source as GamePlayer;

            if (truc.PlayerClass.ID == 40 || truc.PlayerClass.ID == 41 || truc.PlayerClass.ID == 42 || truc.PlayerClass.ID == 56 || truc.PlayerClass.ID == 55)// eld,ench,menta,vw,ani
            {
                if (source is GamePlayer)
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
            else
            {
                truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
        if (source is GameSummonedPet)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get
        {
            return 60000;
        }
    }
    public override bool AddToWorld()
    {
        Model = 951;
        Name = "Magical Earthmender";
        Size = 150;
        Level = 73;
        Realm = 0;
        CurrentRegionID = 191;//galladoria
        MaxSpeedBase = 0;


        RespawnInterval = -1;//will not respawn
        Gender = EGender.Neutral;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        MeleeDamageType = EDamageType.Slash;
        BodyType = 5;

        MagicalEarthmenderBrain sBrain = new MagicalEarthmenderBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Magical Earthmender

#region Natural Earthmender
public class NaturalEarthmender : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public NaturalEarthmender()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer)
        {
            GamePlayer truc = source as GamePlayer;

            if (truc.PlayerClass.ID == 48 || truc.PlayerClass.ID == 47 || truc.PlayerClass.ID == 46 || truc.PlayerClass.ID == 56 || truc.PlayerClass.ID == 55)// bard,druid,warden,ani,vw
            {
                if (source is GamePlayer)
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
            else
            {
                truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
        if (source is GameSummonedPet)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get
        {
            return 60000;
        }
    }
    public override bool AddToWorld()
    {
        Model = 951;
        Name = "Natural Earthmender";
        Size = 150;
        Level = 73;
        Realm = 0;
        CurrentRegionID = 191;//galladoria
        MaxSpeedBase = 0;

        RespawnInterval = -1;//will not respawn
        Gender = EGender.Neutral;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        MeleeDamageType = EDamageType.Slash;
        BodyType = 5;

        NaturalEarthmenderBrain sBrain = new NaturalEarthmenderBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Natural Earthmender

#region Shadowy Earthmender
public class ShadowyEarthmender : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ShadowyEarthmender()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 60; // dmg reduction for rest resists
        }
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer)
        {
            GamePlayer truc = source as GamePlayer;

            if (truc.PlayerClass.ID == 49 || truc.PlayerClass.ID == 50 || truc.PlayerClass.ID == 56 || truc.PlayerClass.ID == 55)// ns,ranger,vw,ani
            {
                if (source is GamePlayer)
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
            else
            {
                truc.Out.SendMessage(Name + " is immune to your damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
        }
        if (source is GameSummonedPet)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get
        {
            return 60000;
        }
    }
    public override bool AddToWorld()
    {
        Model = 951;
        Name = "Shadowy Earthmender";
        Size = 150;
        Level = 73;
        Realm = 0;
        CurrentRegionID = 191;//galladoria
        MaxSpeedBase = 0;

        RespawnInterval = -1;//will not respawn
        Gender = EGender.Neutral;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        MeleeDamageType = EDamageType.Slash;
        BodyType = 5;

        ShadowyEarthmenderBrain sBrain = new ShadowyEarthmenderBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        Brain.Start();
        base.AddToWorld();
        return true;
    }
}
#endregion Shadowy Earthmender

#region Vortex
public class Vortex : GameNpc
{
    public Vortex() : base() { }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
                || damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                || damageType == EDamageType.Slash)
            {
                GamePlayer truc;
                if (source is GamePlayer)
                    truc = (source as GamePlayer);
                else
                    truc = ((source as GameSummonedPet).Owner as GamePlayer);
                if (truc != null)
                    truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);

                base.TakeDamage(source, damageType, 0, 0);
                return;
            }
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public override int AttackRange
    {
        get { return 200; }
        set { }
    }
    public override void DropLoot(GameObject killer)//no loot
    {
    }
    public override void Die(GameObject killer)
    {
        base.Die(null); // null to not gain experience
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 250 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override bool AddToWorld()
    {
        Model = 1269;
        Name = "Watery Vortex";
        RespawnInterval = 360000;
        Size = 50;
        Level = 87;
        MaxSpeedBase = 0;
        Strength = 15;
        Intelligence = 200;
        Piety = 200;
        Flags ^= ENpcFlags.FLYING;

        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        VortexBrain adds = new VortexBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Vortex

#region Waterfall Anti-Pass
public class WaterfallAntipass : GameNpc
{
    public WaterfallAntipass() : base() { }
    public override bool AddToWorld()
    {
        Model = 665;
        Name = "Waterfall Antipass";
        Size = 50;
        Level = 50;
        MaxSpeedBase = 0;
        Flags ^= ENpcFlags.DONTSHOWNAME;
        Flags ^= ENpcFlags.PEACE;
        Flags ^= ENpcFlags.CANTTARGET;

        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        WaterfallAntipassBrain adds = new WaterfallAntipassBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Waterfall Anti-Pass

#region Visual Effects
public class OlcasgeanEffect : GameNpc
{
    public OlcasgeanEffect() : base() { }
    public override bool AddToWorld()
    {
        Model = 665;
        Name = "Root Effect";
        Size = 70;
        Level = 50;
        MaxSpeedBase = 0;
        Flags ^= ENpcFlags.DONTSHOWNAME;
        Flags ^= ENpcFlags.PEACE;
        Flags ^= ENpcFlags.CANTTARGET;

        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        OlcasgeanEffectBrain adds = new OlcasgeanEffectBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
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
            foreach (GamePlayer player in this.GetPlayersInRadius(8000))
            {
                if (player != null)
                    player.Out.SendSpellEffectAnimation(this, this, 11027, 0, false, 0x01);
            }
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(RemoveMob), 3000);
        }
        return 0;
    }
    public int RemoveMob(EcsGameTimer timer)
    {
        if (IsAlive)
            RemoveFromWorld();
        return 0;
    }
}
#endregion Visual Effects