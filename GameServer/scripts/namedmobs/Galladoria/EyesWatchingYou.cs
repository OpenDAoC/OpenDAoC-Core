using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
    public class EyesWatchingYouInit : GameNPC
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
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Eyes Watching You Initializator", 191, (eRealm)0);
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
                CO.Flags ^= eFlags.CANTTARGET;
                CO.Flags ^= eFlags.FLYING;
                CO.Flags ^= eFlags.DONTSHOWNAME;
                CO.Flags ^= eFlags.PEACE;
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
}
namespace DOL.AI.Brain
{
    public class EyesWatchingYouInitBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public EyesWatchingYouInitBrain()
            : base()
        {
            ThinkInterval = 1000;
        }
        public static List<GamePlayer> PlayersInGalla = new List<GamePlayer>();
        public static bool Pick_randomly_Target = false;
        private bool allowTimer = false;
        private int TimerDoStuff(ECSGameTimer timer)
        {
            PickPlayer();
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(EndTimerDoStuff), 5000);
            return 0;
        }
        private int EndTimerDoStuff(ECSGameTimer timer)
        {
            allowTimer = false;
            return 0;
        }
        public void DoStuff()
        {
            if (Body.IsAlive)
            {
                foreach (GameClient client in WorldMgr.GetClientsOfRegion(191))
                {
                    if (client != null)
                    {
                        if (client.Player.IsAlive && client.Account.PrivLevel == 1 && !PlayersInGalla.Contains(client.Player))
                            PlayersInGalla.Add(client.Player);//add players to list from whole galladoria
                    }
                }
            }
        }
        public static GamePlayer randomtarget = null;
        public static GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public void PickPlayer()
        {
            if (Body.IsAlive)
            {
                if (PlayersInGalla.Count > 0)
                {
                    GamePlayer ptarget = PlayersInGalla[Util.Random(1, PlayersInGalla.Count) - 1];
                    RandomTarget = ptarget;
                    if (RandomTarget != null && RandomTarget.Client.Account.PrivLevel == 1 && RandomTarget.IsAlive && RandomTarget.CurrentRegionID == 191)
                    {
                        //create mob only for visual purpose
                        EyesWatchingYouEffect mob = new EyesWatchingYouEffect();
                        mob.X = RandomTarget.X;
                        mob.Y = RandomTarget.Y;
                        mob.Z = RandomTarget.Z;
                        mob.CurrentRegion = RandomTarget.CurrentRegion;
                        mob.Heading = RandomTarget.Heading;
                        mob.AddToWorld();
                    }
                    RandomTarget = null;
                    Pick_randomly_Target = false;
                    if (PlayersInGalla.Count > 0)
                        PlayersInGalla.Clear();
                }
            }
        }
        public override void Think()
        {
            if (Body.IsAlive)
                DoStuff();
            if (!allowTimer)
            {
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TimerDoStuff), Util.Random(25000, 45000));
                allowTimer = true;
            }
            base.Think();
        }
    }
}
////////////////////////////////////////////////////////////////////////////Effect Mob/////////////////////
namespace DOL.GS
{
    public class EyesWatchingYouEffect : GameNPC
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
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Flags ^= eFlags.CANTTARGET;

            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            OlcasgeanEffectBrain adds = new OlcasgeanEffectBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
            }
            return success;
        }
        protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                foreach (GamePlayer player in this.GetPlayersInRadius(8000))
                {
                    if (player != null)
                        player.Out.SendSpellEffectAnimation(this, this, 6177, 0, false, 0x01);
                }
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(RemoveMob), 5000);
            }
            return 0;
        }
        public int RemoveMob(ECSGameTimer timer)
        {
            if (IsAlive)
                RemoveFromWorld();
            return 0;
        }
    }
}
namespace DOL.AI.Brain
{
    public class EyesWatchingYouEffectBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public EyesWatchingYouEffectBrain()
            : base()
        {
            ThinkInterval = 2000;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}