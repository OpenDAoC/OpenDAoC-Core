using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Scurceol Hyrde
public class ScurceolHyrdeBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ScurceolHyrdeBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            if ((LyftMihtOne.Orb1Count > 0 || LyftMihtTwo.Orb2Count > 0 || LyftMihtThree.Orb3Count > 0 || LyftMihtFour.Orb4Count > 0))
                Body.Strength = 320;
            else
                Body.Strength = 260;
        }
        base.Think();
    }
}
#endregion Scurceol Hyrde

#region 1st Orb (Lyft Miht)
public class LyftMihtBrain1 : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public LyftMihtBrain1() : base()
    {
        AggroLevel = 100;
        AggroRange = 250;
        ThinkInterval = 1500;
    }
    private protected static bool IsTargetPicked = false;
    private protected static GamePlayer randomtarget = null;
    private protected static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    private protected int ResetPort(EcsGameTimer timer)
    {
        RandomTarget = null;//reset random target to null
        IsTargetPicked = false;
        return 0;
    }
    private protected int TeleportPlayer(EcsGameTimer timer)
    {
        if (RandomTarget.IsAlive && RandomTarget != null)
        {
            switch (Util.Random(1, 4))
            {
                case 1: RandomTarget.MoveTo(61, 50986, 20031, 16964, 3091); break;
                case 2: RandomTarget.MoveTo(61, 51936, 21012, 16964, 2093); break;
                case 3: RandomTarget.MoveTo(61, 52784, 20019, 16964, 982); break;
                case 4: RandomTarget.MoveTo(61, 51940, 18968, 16964, 26); break;
            }
            RandomTarget.TakeDamage(RandomTarget, EDamageType.Falling, RandomTarget.MaxHealth / 5, 0);
            RandomTarget.Out.SendMessage("You take falling damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetPort), 1500);
        return 0;
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (ad != null && Util.Chance(25) && IsTargetPicked == false && ad.Attacker.IsAlive && ad.Attacker != null && ad.Attacker is GamePlayer)
        {
            RandomTarget = ad.Attacker as GamePlayer;
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), Util.Random(8000, 15000));
            IsTargetPicked = true;
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion 1st Orb (Lyft Miht)

#region 2nd Orb (Lyft Miht)
public class LyftMihtBrain2 : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public LyftMihtBrain2() : base()
    {
        AggroLevel = 100;
        AggroRange = 250;
        ThinkInterval = 1500;
    }
    private protected static bool IsTargetPicked = false;
    private protected static GamePlayer randomtarget = null;
    private protected static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    private protected int ResetPort(EcsGameTimer timer)
    {
        RandomTarget = null;//reset random target to null
        IsTargetPicked = false;
        return 0;
    }
    private protected int TeleportPlayer(EcsGameTimer timer)
    {
        if (RandomTarget.IsAlive && RandomTarget != null)
        {
            switch (Util.Random(1, 4))
            {
                case 1: RandomTarget.MoveTo(61, 50986, 20031, 16964, 3091); break;
                case 2: RandomTarget.MoveTo(61, 51936, 21012, 16964, 2093); break;
                case 3: RandomTarget.MoveTo(61, 52784, 20019, 16964, 982); break;
                case 4: RandomTarget.MoveTo(61, 51940, 18968, 16964, 26); break;
            }
            RandomTarget.TakeDamage(RandomTarget, EDamageType.Falling, RandomTarget.MaxHealth / 5, 0);
            RandomTarget.Out.SendMessage("You take falling damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetPort), 1500);
        return 0;
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (ad != null && Util.Chance(25) && IsTargetPicked == false && ad.Attacker.IsAlive && ad.Attacker != null && ad.Attacker is GamePlayer)
        {
            RandomTarget = ad.Attacker as GamePlayer;
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), Util.Random(8000, 15000));
            IsTargetPicked = true;
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion 2nd Orb (Lyft Miht)

#region 3rd Orb (Lyft Miht)
public class LyftMihtBrain3 : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public LyftMihtBrain3() : base()
    {
        AggroLevel = 100;
        AggroRange = 250;
        ThinkInterval = 1500;
    }
    private protected static bool IsTargetPicked = false;
    private protected static GamePlayer randomtarget = null;
    private protected static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    private protected int ResetPort(EcsGameTimer timer)
    {
        RandomTarget = null;//reset random target to null
        IsTargetPicked = false;
        return 0;
    }
    private protected int TeleportPlayer(EcsGameTimer timer)
    {
        if (RandomTarget.IsAlive && RandomTarget != null)
        {
            switch (Util.Random(1, 4))
            {
                case 1: RandomTarget.MoveTo(61, 50986, 20031, 16964, 3091); break;
                case 2: RandomTarget.MoveTo(61, 51936, 21012, 16964, 2093); break;
                case 3: RandomTarget.MoveTo(61, 52784, 20019, 16964, 982); break;
                case 4: RandomTarget.MoveTo(61, 51940, 18968, 16964, 26); break;
            }
            RandomTarget.TakeDamage(RandomTarget, EDamageType.Falling, RandomTarget.MaxHealth / 5, 0);
            RandomTarget.Out.SendMessage("You take falling damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetPort), 1500);
        return 0;
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (ad != null && Util.Chance(25) && IsTargetPicked == false && ad.Attacker.IsAlive && ad.Attacker != null && ad.Attacker is GamePlayer)
        {
            RandomTarget = ad.Attacker as GamePlayer;
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), Util.Random(8000, 15000));
            IsTargetPicked = true;
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion 3rd Orb (Lyft Miht)

#region 4th Orb (Lyft Miht)
public class LyftMihtBrain4 : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public LyftMihtBrain4() : base()
    {
        AggroLevel = 100;
        AggroRange = 250;
        ThinkInterval = 1500;
    }
    private protected static bool IsTargetPicked = false;
    private protected static GamePlayer randomtarget = null;
    private protected static GamePlayer RandomTarget
    {
        get { return randomtarget; }
        set { randomtarget = value; }
    }
    private protected int ResetPort(EcsGameTimer timer)
    {
        RandomTarget = null;//reset random target to null
        IsTargetPicked = false;
        return 0;
    }
    private protected int TeleportPlayer(EcsGameTimer timer)
    {
        if (RandomTarget.IsAlive && RandomTarget != null)
        {
            switch (Util.Random(1, 4))
            {
                case 1: RandomTarget.MoveTo(61, 50986, 20031, 16964, 3091); break;
                case 2: RandomTarget.MoveTo(61, 51936, 21012, 16964, 2093); break;
                case 3: RandomTarget.MoveTo(61, 52784, 20019, 16964, 982); break;
                case 4: RandomTarget.MoveTo(61, 51940, 18968, 16964, 26); break;
            }
            RandomTarget.TakeDamage(RandomTarget, EDamageType.Falling, RandomTarget.MaxHealth / 5, 0);
            RandomTarget.Out.SendMessage("You take falling damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetPort), 1500);
        return 0;
    }
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (ad != null && Util.Chance(25) && IsTargetPicked == false && ad.Attacker.IsAlive && ad.Attacker != null && ad.Attacker is GamePlayer)
        {
            RandomTarget = ad.Attacker as GamePlayer;
            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), Util.Random(8000, 15000));
            IsTargetPicked = true;
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion 4th Orb (Lyft Miht)