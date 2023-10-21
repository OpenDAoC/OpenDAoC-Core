using System.Collections.Generic;

namespace Core.GS.AI.Brains;

#region Eyes Watching You
public class EyesWatchingYouInitBrain : APlayerVicinityBrain
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
    private int TimerDoStuff(EcsGameTimer timer)
    {
        PickPlayer();
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(EndTimerDoStuff), 5000);
        return 0;
    }
    private int EndTimerDoStuff(EcsGameTimer timer)
    {
        allowTimer = false;
        return 0;
    }
    public void DoStuff()
    {
        if (Body.IsAlive && Body.CurrentRegionID == 191)
        {
            foreach (GamePlayer player in ClientService.GetPlayersOfRegion(Body.CurrentRegion))
            {
                if (player.IsAlive && player.Client.Account.PrivLevel == 1 && !PlayersInGalla.Contains(player))
                    PlayersInGalla.Add(player);//add players to list from whole galladoria
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
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TimerDoStuff), Util.Random(25000, 45000));
            allowTimer = true;
        }
    }

    public override void KillFSM()
    {
        
    }
}
#endregion Eyes Watching You

#region Eyes Watching You Effect
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
#endregion Eyes Watching You Effect