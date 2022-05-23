using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
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

    private static long debugTick = 0;

    //debugTimer is for outputing Timer count/callback info for debug purposes
    public static bool debugTimer = false;

    //Number of ticks to debug the Timer
    public static int debugTimerTickCount = 0;


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
        
        //debug variables
        Dictionary<String, int> TimerToRemoveCallbacks = null;
        Dictionary<String, int> TimerToAddCallbacks = null;
        int TimerToRemoveCount = 0;
        int TimerToAddCount = 0;

        //check if need to debug, then setup vars.
        if(debugTimer && debugTimerTickCount > 0)
        {
            TimerToRemoveCount = TimerToRemove.Count;
            TimerToAddCount = TimerToAdd.Count;
            TimerToRemoveCallbacks = new Dictionary<String, int>();
            TimerToAddCallbacks = new Dictionary<String, int>();
        }
        
        while (TimerToRemove.Count > 0)
        {
            lock (_removeTimerLockObject)
            {
                if(debugTimer && TimerToRemoveCallbacks != null && TimerToRemove.Peek()!=null && TimerToRemove.Peek().Callback != null)
                {
                    String callbackMethodName = TimerToRemove.Peek().Callback.Method.Name;
                    if(TimerToRemoveCallbacks.ContainsKey(callbackMethodName))
                        TimerToRemoveCallbacks[callbackMethodName]++;
                    else
                        TimerToRemoveCallbacks.Add(callbackMethodName, 1);
                }

                if(ActiveTimers.Contains(TimerToRemove.Peek()))
                    ActiveTimers.Remove(TimerToRemove.Pop());
                else
                {
                    TimerToRemove.Pop();
                }
            }
        }

        while (TimerToAdd.Count > 0)
        {
            lock (_addTimerLockObject)
            {
                if(debugTimer && TimerToAddCallbacks != null && TimerToAdd.Peek()!=null && TimerToAdd.Peek().Callback != null)
                {
                    String callbackMethodName = TimerToAdd.Peek().Callback.Method.Name;
                    if(TimerToAddCallbacks.ContainsKey(callbackMethodName))
                        TimerToAddCallbacks[callbackMethodName]++;
                    else
                        TimerToAddCallbacks.Add(callbackMethodName, 1);
                }

                if (!ActiveTimers.Contains(TimerToAdd.Peek()))
                    ActiveTimers.Add(TimerToAdd.Pop());
                else
                    TimerToAdd.Pop();
            }
        }

        //Console.WriteLine($"timer size {ActiveTimers.Count}");
        /*
        if (debugTick + 1000 < tick)
        {
            Console.WriteLine($"timer size {ActiveTimers.Count}");
            debugTick = tick;
        }*/

        Parallel.ForEach(ActiveTimers, timer =>
        {
            if (timer != null && timer.NextTick < GameLoop.GameLoopTime)
                timer.Tick();
        });


        //Output Debug info
        if(debugTimer && TimerToRemoveCallbacks != null && TimerToAddCallbacks != null)
        {
            Console.WriteLine($"==== TimerService Callback Methods ====");

            Console.WriteLine($"==== TimerService RemoveTimer Top 5 Callback Methods. Total TimerToRemove Count: {TimerToRemoveCount} ====");
             
            foreach (var callbacks in TimerToRemoveCallbacks.OrderByDescending(callback => callback.Value).Take(5))
            {
                Console.WriteLine($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");
            }

            Console.WriteLine($"==== TimerService AddTimer Top 5 Callback Methods. Total TimerToAdd Count: {TimerToAddCount} ====");
            foreach (var callbacks in TimerToAddCallbacks.OrderByDescending(callback => callback.Value).Take(5))
            {
                Console.WriteLine($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");
            }

            Console.WriteLine("---------------------------------------------------------------------------");
             
            if(debugTimerTickCount > 1)
                debugTimerTickCount --;
            else
            {
                debugTimer = false;
                debugTimerTickCount = 0;
            }

        }
        
        Diagnostics.StopPerfCounter(ServiceName);
    }

    private static readonly object _addTimerLockObject = new object();
    public static void AddTimer(ECSGameTimer newTimer)
    {
        //  if (!ActiveTimers.Contains(newTimer))
      //  {
      lock (_addTimerLockObject)
      {
          TimerToAdd?.Push(newTimer);
      }
      //Console.WriteLine($"added {newTimer.Callback.GetMethodInfo()}");
      //  }
    }

    //Adds timer to the TimerToAdd Stack without checking it already exists. Helpful if the timer is being removed and then added again in same tick.
    //The Tick() method will still check for duplicate timer in ActiveTimers
    public static void AddExistingTimer(ECSGameTimer newTimer)
    {
        lock (_addTimerLockObject)
        {
            TimerToAdd?.Push(newTimer);
        }
    }

    private static readonly object _removeTimerLockObject = new object();
    public static void RemoveTimer(ECSGameTimer timerToRemove)
    {
        lock (_removeTimerLockObject)
        {
            if (ActiveTimers.Contains(timerToRemove))
            {
                TimerToRemove?.Push(timerToRemove);
                //Console.WriteLine($"removed {timerToRemove.Callback.GetMethodInfo()}");
            }
        }
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
    public int Interval;
    public long StartTick;
    public long NextTick => StartTick + Interval;

    public GameObject TimerOwner;
    //public GameTimer.TimeManager GameTimeOwner;
    public bool IsAlive => TimerService.HasActiveTimer(this);
    
    /// <summary>
    /// Holds properties for this region timer
    /// </summary>
    private PropertyCollection m_properties;

    public ECSGameTimer(GameObject target)
    {
        TimerOwner = target;
    }

    public ECSGameTimer(GameObject target, ECSTimerCallback callback, int interval)
    {
        TimerOwner = target;
        Callback = callback;
        Interval = interval;
        this.Start();
    }
    
    public ECSGameTimer(GameObject target, ECSTimerCallback callback)
    {
        TimerOwner = target;
        Callback = callback;
    }

    public void Start()
    {
        if(Interval <= 0)
            Start(500); //use half-second intervals by default
        else
        {
            Start((int)Interval);
        }
    }

    public void Start(int interval)
    {
        StartTick = GameLoop.GameLoopTime;
        Interval = interval;
        TimerService.AddTimer(this);
    }

    public void StartExistingTimer(int interval)
    {
        StartTick = GameLoop.GameLoopTime;
        Interval = interval;
        TimerService.AddExistingTimer(this);
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
            Interval = (int) Callback.Invoke(this);
        }
        
        if(Interval == 0) Stop();
    }
    /*
    /// <summary>
    /// Stores the time where the timer was inserted
    /// </summary>
    private long m_targetTime = -1;
    */
    
    /// <summary>
    /// Gets the time left until this timer fires, in milliseconds.
    /// </summary>
    public int TimeUntilElapsed
    {
        get
        {
            return (int)((this.StartTick + Interval) - GameLoop.GameLoopTime);
        }
    }

    /// <summary>
    /// Gets the properties of this timer
    /// </summary>
    public PropertyCollection Properties
    {
        get
        {
            if (m_properties == null)
            {
                lock (this)
                {
                    if (m_properties == null)
                    {
                        PropertyCollection properties = new PropertyCollection();
                        Thread.MemoryBarrier();
                        m_properties = properties;
                    }
                }
            }
            return m_properties;
        }
    }
    
}