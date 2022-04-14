using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Quests;
using ECS.Debug;

namespace DOL.GS;

public class TimerService
{
    private const string ServiceName = "Timer Service";

    private static List<ECSGameTimer> ActiveTimers;
    private static Stack<ECSGameTimer> TimerToRemove;
    private static Stack<ECSGameTimer> TimerToAdd;


    static TimerService()
    {
        EntityManager.AddService(typeof(TimerService));
        ActiveTimers = new List<ECSGameTimer>();
        TimerToAdd = new Stack<ECSGameTimer>();
        TimerToRemove = new Stack<ECSGameTimer>();
    }

    public static void Tick(long tick)
    {
        
        Diagnostics.StartPerfCounter(ServiceName);
        
        while(TimerToAdd.Count > 0) ActiveTimers.Add(TimerToAdd.Pop());

        while (TimerToRemove.Count > 0) ActiveTimers.Remove(TimerToRemove.Pop());
        
        //Console.WriteLine($"timer size {ActiveTimers.Count}");

        foreach (var timer in ActiveTimers)
        {
            if (timer != null && timer.NextTick < GameLoop.GameLoopTime)
                timer.Tick();
        }
        
        Diagnostics.StopPerfCounter(ServiceName);
    }

    public static void AddTimer(ECSGameTimer newTimer)
    {
        if(!ActiveTimers.Contains(newTimer)) 
            TimerToAdd.Push(newTimer);
    }

    public static void RemoveTimer(ECSGameTimer timerToRemove)
    {
        if (ActiveTimers.Contains(timerToRemove)) 
            TimerToRemove.Push(timerToRemove);
    }

    public static bool HasActiveTimer(ECSGameTimer timer)
    {
        return ActiveTimers.Contains(timer);
    }
}

public class ECSGameTimer
{
    /// <summary>
    /// This delegate is the callback function for the ECS Timer
    /// </summary>
    public delegate int ECSTimerCallback(ECSGameTimer timer);

    public ECSTimerCallback Callback;
    public long Interval;
    public long StartTick;
    public long NextTick => StartTick + Interval;

    public GameLiving TimerOwner;
    public bool IsAlive => TimerService.HasActiveTimer(this);

    public ECSGameTimer(GameLiving living)
    {
        TimerOwner = living;
    }

    public void Start()
    {
        Start(500); //use half-second intervals by default
    }

    public void Start(long interval)
    {
        StartTick = GameLoop.GameLoopTime;
        Interval = interval;
        TimerService.AddTimer(this);
    }
    
    public void Stop()
    {
        TimerService.RemoveTimer(this);
    }

    public void Tick()
    {
        StartTick = GameLoop.GameLoopTime;
        if (Callback != null)
        {
            Interval = (long) Callback.Invoke(this);
        }
        
        if(Interval == 0) Stop();
    }

    
    
}