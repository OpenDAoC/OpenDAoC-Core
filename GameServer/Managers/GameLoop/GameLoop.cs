using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.GS.ServerProperties;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public static class GameLoop
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const string THREAD_NAME = "GameLoop";

        private static Thread _gameLoopThread;
        private static TickExecutor _tickExecutor;
        private static GameLoopTickPacer _tickPacer;
        private static bool _init;
        private static bool _running;
        private static List<TickState> _tickSequence;

        public static int DegreeOfParallelism { get; } = Environment.ProcessorCount;
        public static double TickDuration { get; private set; }
        public static long GameLoopTime { get; private set; }
        public static IGameService ActiveService { get; private set; }

        public static bool Init()
        {
            if (Volatile.Read(ref _running) || Interlocked.CompareExchange(ref _init, true, false))
                return false;

            TickDuration = 1000.0 / Properties.GAME_LOOP_TICK_RATE;

            _gameLoopThread = new(new ThreadStart(Run))
            {
                Name = THREAD_NAME,
                IsBackground = true
            };

            _tickPacer = new(TickDuration);

            TickExecutorInitParams initParams = new(
                static _ => GameServer.Instance.Stop(),
                static () => GameLoopTime);

            if (DegreeOfParallelism == 1)
                _tickExecutor = new SerialTickExecutor(initParams);
            else
                _tickExecutor = new ParallelTickExecutor(DegreeOfParallelism, THREAD_NAME, initParams);

            BuildTickSequence();
            return true;
        }

        public static void Start()
        {
            if (!Volatile.Read(ref _init))
                throw new InvalidOperationException("Game loop is not initialized.");

            if (Interlocked.CompareExchange(ref _running, true, false))
                throw new InvalidOperationException("Game loop is already running.");

            _gameLoopThread.Start();
            _tickPacer.Start();
        }

        public static void Stop()
        {
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            if (Thread.CurrentThread != _gameLoopThread && _gameLoopThread.IsAlive)
                _gameLoopThread.Join();

            _tickPacer.Stop();
            _tickExecutor.Dispose();
        }

        public static List<(int, double)> GetAverageTps()
        {
            return _tickPacer.Stats.GetAverageTicks();
        }

        public static void ExecuteView<T>(in ServiceObjectView<T> view, Action<T> action) where T : class, IServiceObject
        {
            _tickExecutor.ExecuteView(view, action);
        }

        public static void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action)
        {
            _tickExecutor.ExecuteForEach(items, toExclusive, action);
        }

        public static void ExecuteForEachSharded<T>(List<T>[] shards, int[] shardStartIndices, int totalCount, Action<T> action)
        {
            _tickExecutor.ExecuteForEachSharded(shards, shardStartIndices, totalCount, action);
        }

        public static T GetObjectForTick<T>() where T : ITickPooledObject<T>, new()
        {
            return _tickExecutor != null ? _tickExecutor.GetObjectForTick<T>() : new();
        }

        public static List<T> GetListForTick<T>() where T : ITickPooledList<T>
        {
            return _tickExecutor != null ? _tickExecutor.GetListForTick<T>() : new();
        }

        private static void Run()
        {
            _tickExecutor.Start(); // Must be done from the game loop thread.

            while (Volatile.Read(ref _running))
            {
                try
                {
                    TickServices();
                    GameLoopTime = _tickPacer.WaitForNextTick();
                }
                catch (Exception e)
                {
                    if (log.IsFatalEnabled)
                        log.Fatal($"Critical error encountered in {nameof(GameLoop)}: {e}");

                    GameServer.Instance.Stop();
                    break;
                }
            }

            if (log.IsInfoEnabled)
                log.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");
        }

        private static void TickServices()
        {
            Diagnostics.StartPerfCounter(THREAD_NAME);

            for (int i = 0; i < _tickSequence.Count; i++)
            {
                TickState tickState = _tickSequence[i];
                SynchronizationContext prevCtx = SynchronizationContext.Current;
                GameServiceSynchronizationContext newCtx = tickState.ServiceContext;
                SynchronizationContext.SetSynchronizationContext(newCtx);
                ActiveService = newCtx.TargetService;
                Diagnostics.StartPerfCounter(tickState.ProfileKey);

                try
                {
                    tickState.TickAction();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(prevCtx);
                    Diagnostics.StopPerfCounter(tickState.ProfileKey);
                    ActiveService = null;
                }
            }

            Diagnostics.StopPerfCounter(THREAD_NAME);
            Diagnostics.Tick();
        }

        private static void BuildTickSequence()
        {
            _tickSequence = new();

            AddStep(GameLoopService.Instance, GameLoopService.Instance.Tick);
            AddStep(TimerService.Instance, TimerService.Instance.Tick);
            AddStep(ClientService.Instance, ClientService.Instance.BeginTick);
            AddStep(NpcService.Instance, NpcService.Instance.Tick);
            AddStep(AttackService.Instance, AttackService.Instance.Tick);
            AddStep(CastingService.Instance, CastingService.Instance.Tick);
            AddStep(EffectService.Instance, EffectService.Instance.Tick);
            AddStep(EffectListService.Instance, EffectListService.Instance.BeginTick);
            AddStep(EffectListService.Instance, EffectListService.Instance.EndTick);
            AddStep(MovementService.Instance, MovementService.Instance.Tick);
            AddStep(CraftingService.Instance, CraftingService.Instance.Tick);
            AddStep(ReaperService.Instance, ReaperService.Instance.Tick);
            AddStep(ZoneService.Instance, ZoneService.Instance.Tick);
            AddStep(ClientService.Instance, ClientService.Instance.EndTick);
            AddStep(RolloverSchedulerService.Instance, RolloverSchedulerService.Instance.Tick);

            // The following services tick via RolloverSchedulerService.
            PeriodicQuestService.Initialize();
            HouseRentService.Initialize();
            GravestoneService.Initialize();

            static void AddStep(IGameService service, Action action)
            {
                string methodName = action.Method.Name;
                string profileKey = methodName == nameof(IGameService.Tick) ? service.ServiceName : $"{service.ServiceName}.{methodName}";
                GameServiceSynchronizationContext serviceContext = GameServiceContext.GetContextFor(service);
                _tickSequence.Add(new(action, profileKey, serviceContext));
            }
        }

        private sealed class TickState
        {
            public readonly Action TickAction;
            public readonly string ProfileKey;
            public readonly GameServiceSynchronizationContext ServiceContext;

            public TickState(Action tickAction, string profileKey, GameServiceSynchronizationContext serviceContext)
            {
                TickAction = tickAction;
                ProfileKey = profileKey;
                ServiceContext = serviceContext;
            }
        }
    }
}
