using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.ECS;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.Timer)]
public class TimerAction : AAction<string,int>
{
    /// <summary>
    /// Constant used to store timerid in RegionTimer.Properties
    /// </summary>
    const string TIMER_ID = "timerid";
    /// <summary>
    /// Constant used to store GameLiving Source in RegionTimer.Properties
    /// </summary>
    const string TIMER_SOURCE = "timersource";


    public TimerAction(GameNpc defaultNPC,  Object p, Object q)
        : base(defaultNPC, EActionType.Timer, p, q)
    { 
        
    }


    public TimerAction(GameNpc defaultNPC,   string timerID, int delay)
        : this(defaultNPC, (object)timerID,(object) delay) { }
    


    public override void Perform(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

        EcsGameTimer timer = new EcsGameTimer(player, new EcsGameTimer.EcsTimerCallback(QuestTimerCallBack));
        timer.Properties.SetProperty(TIMER_ID, P);
        timer.Properties.SetProperty(TIMER_SOURCE, player);
        timer.Start(Q);
    }

    /// <summary>
    /// Callback for quest internal timers used via eActionType.Timer and eTriggerType.Timer
    /// </summary>
    /// <param name="callingTimer"></param>
    /// <returns>0</returns>
    private static int QuestTimerCallBack(EcsGameTimer callingTimer)
    {
        string timerid = callingTimer.Properties.GetProperty<string>(TIMER_ID, null);
        if (timerid == null)
            throw new ArgumentNullException("TimerId out of Range", "timerid");

        GameLiving source = callingTimer.Properties.GetProperty<GameLiving>(TIMER_SOURCE, null);
        if (source == null)
            throw new ArgumentNullException("TimerSource null", "timersource");


        TimerEventArgs args = new TimerEventArgs(source, timerid);
        source.Notify(GameLivingEvent.Timer, source, args);

        return 0;
    }
}