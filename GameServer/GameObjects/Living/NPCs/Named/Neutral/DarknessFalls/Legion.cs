using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

namespace Core.GS.Scripts;
    
#region Legion
public class Legion : GameEpicBoss
{
private static new readonly log4net.ILog log =
    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

private static IArea legionArea = null;

[ScriptLoadedEvent]
public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
{
    const int radius = 650;
    Region region = WorldMgr.GetRegion(249);
    legionArea = region.AddArea(new Area.Circle("Legion's Lair", 45000, 51700, 15468, radius));
    log.Debug("Legion's Lair created with radius " + radius + " at 45000 51700 15468");
    //legionArea.RegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));

    //GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));

    if (log.IsInfoEnabled)
        log.Info("Legion initialized..");
}

[ScriptUnloadedEvent]
public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
{
    //legionArea.UnRegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
    WorldMgr.GetRegion(249).RemoveArea(legionArea);

    //GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));
}

public Legion()
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
        default: return 40; // dmg reduction for rest resists
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
public override int MaxHealth
{
    get { return 300000; }
}

public override bool AddToWorld()
{
    INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13333);
    LoadTemplate(npcTemplate);

    Size = 120;
    Strength = npcTemplate.Strength;
    Constitution = npcTemplate.Constitution;
    Dexterity = npcTemplate.Dexterity;
    Quickness = npcTemplate.Quickness;
    Empathy = npcTemplate.Empathy;
    Piety = npcTemplate.Piety;
    Intelligence = npcTemplate.Intelligence;
    LegionBrain.CanThrow = false;
    LegionBrain.RemoveAdds = false;
    LegionBrain.IsCreatingSouls = false;

    // demon
    BodyType = 2;
    RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
    Faction = FactionMgr.GetFactionByID(191);
    Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

    LegionBrain sBrain = new LegionBrain();
    SetOwnBrain(sBrain);
    SaveIntoDatabase();
    base.AddToWorld();
    return true;
}

public override double AttackDamage(DbInventoryItem weapon)
{
    return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
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
public override void Die(GameObject killer)
{
    foreach (GameNpc npc in GetNPCsInRadius(5000))
    {
        if (npc.Brain is LegionAddBrain)
        {
            npc.RemoveFromWorld();
        }
    }

    bool canReportNews = true;

    // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    {
        player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

        if (!canReportNews || GameServer.ServerRules.CanGenerateNews(player) != false) continue;
        if (player.Client.Account.PrivLevel == (int) EPrivLevel.Player)
            canReportNews = false;
    }

    var throwPlayer = TempProperties.GetProperty<EcsGameTimer>("legion_throw");//cancel teleport
    if (throwPlayer != null)
    {
        throwPlayer.Stop();
        TempProperties.RemoveProperty("legion_throw");
    }

    var castaoe = TempProperties.GetProperty<EcsGameTimer>("legion_castaoe");//cancel cast aoe
    if (castaoe != null)
    {
        castaoe.Stop();
        TempProperties.RemoveProperty("legion_castaoe");
    }

    base.Die(killer);

    if (canReportNews)
    {
        ReportNews(killer);
    }
}
public void BroadcastMessage(String message)
{
    foreach (GamePlayer player in GetPlayersInRadius(3000))
    {
        player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
    }
}
public override void EnemyKilled(GameLiving enemy)
{
    if (enemy != null && enemy is GamePlayer)
    {
        BroadcastMessage("Legion says, \"Your soul give me new strength.\"");
        Health += MaxHealth / 40; //heals if boss kill enemy player for 2.5% of his max health
    }
    base.EnemyKilled(enemy);
}
/*  private static void PlayerEnterLegionArea(DOLEvent e, object sender, EventArgs args)
{
    AreaEventArgs aargs = args as AreaEventArgs;
    GamePlayer player = aargs?.GameObject as GamePlayer;

    if (player == null)
        return;

    var mobsInArea = player.GetNPCsInRadius(2500);

    if (mobsInArea == null)
        return;

    foreach (GameNPC mob in mobsInArea)
    {
        if (mob is not Legion || !mob.InCombat) continue;

        if (Util.Chance(33))
        {
            foreach (GamePlayer nearbyPlayer in mob.GetPlayersInRadius(2500))
            {
                nearbyPlayer.Out.SendMessage("Legion doesn't like enemies in his lair", eChatType.CT_Broadcast,
                    eChatLoc.CL_ChatWindow);
                nearbyPlayer.Out.SendSpellEffectAnimation(mob, player, 5933, 0, false, 1);
            }

            //player.Die(mob);
        }
       / else
        {
            foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
            {
                playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
                playerNearby.BroadcastUpdate();
            }

            player.MoveTo(249, 48200, 49566, 20833, 1028);
        }

       // player.BroadcastUpdate();
    }
}*/
/* private static void PlayerKilledByLegion(DOLEvent e, object sender, EventArgs args)
{
    GamePlayer player = sender as GamePlayer;

    if (player == null)
        return;

    DyingEventArgs eArgs = args as DyingEventArgs;

    if (eArgs?.Killer?.Name != "Legion")
        return;

    foreach (GameNPC mob in player.GetNPCsInRadius(2000))
    {
        if (mob is not Legion) continue;
        mob.Health += player.MaxHealth;
        mob.UpdateHealthManaEndu();
    }

    foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
    {
        playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
        playerNearby.BroadcastUpdate();
    }
}*/
public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
{
    //possible AttackRange
    int distance = 1400;
    
    if (source is GamePlayer || source is GameSummonedPet)
    {
        if (!source.IsWithinRadius(this, distance)) //take no damage from source that is not in radius 1000
        {
            GamePlayer truc;
            if (source is GamePlayer)
                truc = (source as GamePlayer);
            else
                truc = ((source as GameSummonedPet).Owner as GamePlayer);
            if (truc != null)
                truc.Out.SendMessage(Name + " is not attackable from this range and is immune to your damage!", EChatType.CT_System,
                    EChatLoc.CL_ChatWindow);

            base.TakeDamage(source, damageType, 0, 0);
        }
        else //take dmg
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
    }
}
private void ReportNews(GameObject killer)
{
    int numPlayers = AwardLegionKillPoint();
    String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
    NewsMgr.CreateNews(message, killer.Realm, ENewsType.PvE, true);

    if (Properties.GUILD_MERIT_ON_LEGION_KILL <= 0) return;
    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    {
        if (player.IsEligibleToGiveMeritPoints)
        {
            GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_LEGION_KILL);
        }
    }
}
private int AwardLegionKillPoint()
{
    int count = 0;
    foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    {
        player.KillsLegion++;
        player.Achieve(AchievementUtil.AchievementName.Legion_Kills);
        player.RaiseRealmLoyaltyFloor(1);
        count++;
    }
    return count;
}
public override void DealDamage(AttackData ad)
{
    if (ad != null && ad.DamageType == EDamageType.Body)
        Health += ad.Damage / 4;
    base.DealDamage(ad);
}
}
#endregion Legion

#region Legion adds
public class LegionAdd : GameNpc
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LegionAdd()
        : base()
    {
    }
    public override int MaxHealth
    {
        get { return 1200; }
    }

    public override int AttackRange
    {
        get { return 450; }
        set { }
    }
    public override void DropLoot(GameObject killer)
    {
    }
    public override long ExperienceValue => 0;
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 150;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.10;
    }

    public override bool AddToWorld()
    {
        Model = 660;
        Name = "graspering soul";
        Size = 50;
        Realm = 0;

        Strength = 60;
        Intelligence = 60;
        Piety = 60;
        Dexterity = 60;
        Constitution = 60;
        Quickness = 60;
        RespawnInterval = -1;

        Gender = EGender.Neutral;
        MeleeDamageType = EDamageType.Slash;

        BodyType = 2;
        LegionAddBrain sBrain = new LegionAddBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 800;
        base.AddToWorld();
        return true;
    }
}
#endregion Legion adds