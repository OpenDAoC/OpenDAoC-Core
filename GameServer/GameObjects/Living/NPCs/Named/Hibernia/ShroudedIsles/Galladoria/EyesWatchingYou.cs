using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;

namespace Core.GS;

#region Eyes Watching You Effect
public class EyesWatchingYouInit : GameNpc
{
    public EyesWatchingYouInit() : base() { }
    public override int MaxHealth
    {
        get { return 10000; }
    }
    public override bool IsVisibleToPlayers => true;//this make dragon think all the time, no matter if player is around or not
    public override bool AddToWorld()
    {
        EyesWatchingYouInitBrain.RandomTarget = null;
        EyesWatchingYouInitBrain.Pick_randomly_Target = false;
        EyesWatchingYouInitBrain sbrain = new EyesWatchingYouInitBrain();
        SetOwnBrain(sbrain);
        base.AddToWorld();
        return true;
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Eyes Watching You Initializator", 191, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Eyes Watching You Initializator not found, creating it...");

            log.Warn("Initializing Eyes Watching You Initializator...");
            EyesWatchingYouInit CO = new EyesWatchingYouInit();
            CO.Name = "Eyes Watching You Initializator";
            CO.GuildName = "DO NOT REMOVE!";
            CO.RespawnInterval = 5000;
            CO.Model = 665;
            CO.Realm = 0;
            CO.Level = 50;
            CO.Size = 50;
            CO.CurrentRegionID = 191;
            CO.Flags ^= ENpcFlags.CANTTARGET;
            CO.Flags ^= ENpcFlags.FLYING;
            CO.Flags ^= ENpcFlags.DONTSHOWNAME;
            CO.Flags ^= ENpcFlags.PEACE;
            CO.Faction = FactionMgr.GetFactionByID(64);
            CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            CO.X = 37278;
            CO.Y = 63018;
            CO.Z = 12706;
            EyesWatchingYouInitBrain ubrain = new EyesWatchingYouInitBrain();
            CO.SetOwnBrain(ubrain);
            CO.AddToWorld();
            CO.SaveIntoDatabase();
            CO.Brain.Start();
        }
        else
            log.Warn("Eyes Watching You Initializator exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Eyes Watching You

#region Eyes Watching You Effect
public class EyesWatchingYouEffect : GameNpc
{
    public EyesWatchingYouEffect() : base() { }
    public override int MaxHealth
    {
        get { return 10000; }
    }
    public override bool AddToWorld()
    {
        Model = 665;
        Name = "Eyes Watching You";
        Size = 100;
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
                    player.Out.SendSpellEffectAnimation(this, this, 6177, 0, false, 0x01);
            }
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(RemoveMob), 5000);
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
#endregion Eyes Watching You Effect