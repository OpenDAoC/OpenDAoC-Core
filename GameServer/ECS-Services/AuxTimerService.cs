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
using log4net;

namespace DOL.GS;

public class AuxTimerService
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private const string ServiceName = "Aux Timer Service";

    private static List<AuxECSGameTimer> ActiveTimers;
    private static Stack<AuxECSGameTimer> TimerToRemove;
    private static Stack<AuxECSGameTimer> TimerToAdd;

    private static long debugTick = 0;

    //debugTimer is for outputing Timer count/callback info for debug purposes
    public static bool debugTimer = false;

    //Number of ticks to debug the Timer
    public static int debugTimerTickCount = 0;


    static AuxTimerService()
    {
        EntityManager.AddService(typeof(AuxTimerService));
        ActiveTimers = new List<AuxECSGameTimer>();
        TimerToAdd = new Stack<AuxECSGameTimer>();
        TimerToRemove = new Stack<AuxECSGameTimer>();
    }

    public static void Tick(long tick)
    {
        
        // Diagnostics.StartPerfCounter(ServiceName);
        
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
            if (timer != null && timer.NextTick < AuxGameLoop.GameLoopTime)
            {
                long startTick = GameTimer.GetTickCount();
                timer.Tick();
                long stopTick = GameTimer.GetTickCount();
                if((stopTick - startTick)  > 25 )
                    log.Warn($"Long AuxTimerService.Tick for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.TimerOwner?.Name} Time: {stopTick - startTick}ms");
            }
        });


        //Output Debug info
        if(debugTimer && TimerToRemoveCallbacks != null && TimerToAddCallbacks != null)
        {
            log.Debug($"==== AuxTimerService Debug - Total ActiveTimers: {ActiveTimers.Count} ====");

            log.Debug($"==== AuxTimerService RemoveTimer Top 5 Callback Methods. Total TimerToRemove Count: {TimerToRemoveCount} ====");
             
            foreach (var callbacks in TimerToRemoveCallbacks.OrderByDescending(callback => callback.Value).Take(5))
            {
                log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");
            }

            log.Debug($"==== AuxTimerService AddTimer Top 5 Callback Methods. Total TimerToAdd Count: {TimerToAddCount} ====");
            foreach (var callbacks in TimerToAddCallbacks.OrderByDescending(callback => callback.Value).Take(5))
            {
                log.Debug($"Callback Name: {callbacks.Key} Occurences: {callbacks.Value}");
            }

            log.Debug("---------------------------------------------------------------------------");
             
            if(debugTimerTickCount > 1)
                debugTimerTickCount --;
            else
            {
                debugTimer = false;
                debugTimerTickCount = 0;
            }

        }
        
        // Diagnostics.StopPerfCounter(ServiceName);
    }

    private static readonly object _addTimerLockObject = new object();
    public static void AddTimer(AuxECSGameTimer newTimer)
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
    public static void AddExistingTimer(AuxECSGameTimer newTimer)
    {
        lock (_addTimerLockObject)
        {
            TimerToAdd?.Push(newTimer);
        }
    }

    private static readonly object _removeTimerLockObject = new object();
    public static void RemoveTimer(AuxECSGameTimer timerToRemove)
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

    public static bool HasActiveTimer(AuxECSGameTimer timer)
    {
        return ActiveTimers.Contains(timer) || TimerToAdd.Contains(timer);
    }
}

public class AuxECSGameTimer
{
    /// <summary>
    /// This delegate is the callback function for the ECS Timer
    /// </summary>
    public delegate int AuxECSTimerCallback(AuxECSGameTimer timer);

    public AuxECSTimerCallback Callback;
    public int Interval;
    public long StartTick;
    public long NextTick => StartTick + Interval;

    public GameObject TimerOwner;
    //public GameTimer.TimeManager GameTimeOwner;
    public bool IsAlive => AuxTimerService.HasActiveTimer(this);
    
    /// <summary>
    /// Holds properties for this region timer
    /// </summary>
    private PropertyCollection m_properties;

    public AuxECSGameTimer(GameObject target)
    {
        TimerOwner = target;
    }

    public AuxECSGameTimer(GameObject target, AuxECSTimerCallback callback, int interval)
    {
        TimerOwner = target;
        Callback = callback;
        Interval = interval;
        this.Start();
    }
    
    public AuxECSGameTimer(GameObject target, AuxECSTimerCallback callback)
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
        StartTick = AuxGameLoop.GameLoopTime;
        Interval = interval;
        AuxTimerService.AddTimer(this);
    }

    public void StartExistingTimer(int interval)
    {
        StartTick = AuxGameLoop.GameLoopTime;
        Interval = interval;
        AuxTimerService.AddExistingTimer(this);
    }
    
    public void Stop()
    {
        AuxTimerService.RemoveTimer(this);
    }

    public void Tick()
    {
        StartTick = AuxGameLoop.GameLoopTime;
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
            return (int)((this.StartTick + Interval) - AuxGameLoop.GameLoopTime);
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