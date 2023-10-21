using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Core.GS.Enums;
using log4net;

namespace Core.GS.ECS;

public class AuxTimerService
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private const string SERVICE_NAME = nameof(AuxTimerService);

    public static void Tick(long tick)
    {
        // Diagnostics.StartPerfCounter(SERVICE_NAME);

        List<AuxEcsGameTimer> list = EntityMgr.UpdateAndGetAll<AuxEcsGameTimer>(EEntityType.AuxTimer, out int lastValidIndex);

        Parallel.For(0, lastValidIndex + 1, i =>
        {
            AuxEcsGameTimer timer = list[i];

            try
            {
                if (timer?.EntityManagerId.IsSet != true)
                    return;

                if (timer.NextTick < tick)
                {
                    long startTick = GameLoop.GetCurrentTime();
                    timer.Tick();
                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > 25)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for Timer Callback: {timer.Callback?.Method?.DeclaringType}:{timer.Callback?.Method?.Name}  Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
                }
            }
            catch (Exception e)
            {
                ServiceUtil.HandleServiceException(e, SERVICE_NAME, timer, timer.Owner);
            }
        });

        // Diagnostics.StopPerfCounter(SERVICE_NAME);
    }
}

public class AuxEcsGameTimer : IManagedEntity
{
    /// <summary>
    /// This delegate is the callback function for the ECS Timer
    /// </summary>
    public delegate int AuxECSTimerCallback(AuxEcsGameTimer timer);

    public GameObject Owner { get; set; }
    public AuxECSTimerCallback Callback { get; set; }
    public int Interval { get; set; }
    public long StartTick { get; set; }
    public long NextTick => StartTick + Interval;
    public bool IsAlive { get; set; }
    public int TimeUntilElapsed => (int) (StartTick + Interval - AuxGameLoop.GameLoopTime);
    public EntityManagerId EntityManagerId { get; set; } = new(EEntityType.AuxTimer, false);
    private PropertyCollection _properties;

    public AuxEcsGameTimer(GameObject owner)
    {
        Owner = owner;
    }

    public AuxEcsGameTimer(GameObject owner, AuxECSTimerCallback callback)
    {
        Owner = owner;
        Callback = callback;
    }

    public AuxEcsGameTimer(GameObject owner, AuxECSTimerCallback callback, int interval)
    {
        Owner = owner;
        Callback = callback;
        Interval = interval;
        Start();
    }

    public void Start()
    {
        // Use half-second intervals by default.
        Start(Interval <= 0 ? 500 : Interval);
    }

    public void Start(int interval)
    {
        StartTick = AuxGameLoop.GameLoopTime;
        Interval = interval;

        if (EntityMgr.Add(this))
            IsAlive = true;
    }

    public void Stop()
    {
        if (EntityMgr.Remove(this))
           IsAlive = false;
    }

    public void Tick()
    {
        StartTick = AuxGameLoop.GameLoopTime;

        if (Callback != null)
            Interval = Callback.Invoke(this);

        if (Interval == 0)
            Stop();
    }

    public PropertyCollection Properties
    {
        get
        {
            if (_properties == null)
            {
                lock (this)
                {
                    if (_properties == null)
                    {
                        PropertyCollection properties = new PropertyCollection();
                        Thread.MemoryBarrier();
                        _properties = properties;
                    }
                }
            }

            return _properties;
        }
    }
}

public abstract class AuxEcsGameTimerWrapperBase : AuxEcsGameTimer
{
    public AuxEcsGameTimerWrapperBase(GameObject owner) : base(owner)
    {
        Owner = owner;
        Callback = new AuxECSTimerCallback(OnTick);
    }

    protected abstract int OnTick(AuxEcsGameTimer timer);
}