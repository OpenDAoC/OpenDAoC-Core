using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const bool DYNAMIC_BUSY_WAIT_THRESHOLD = true; // Setting it to false disables busy waiting completely unless a default value is given to '_busyWaitThreshold'.

        private static Thread _gameLoopThread;
        private static GameLoopThreadPool _threadPool;
        private static GameLoopTickPacer _tickPacer;
        private static long _stopwatchFrequencyMilliseconds = Stopwatch.Frequency / 1000;
        private static bool _running;
        private static List<(IGameService Service, Action TickAction, string ProfileKey)> _tickSequence;

        public static long TickDuration { get; private set; }
        public static long GameLoopTime { get; private set; }
        public static string ActiveService { get; set; }

        // This is unrelated to the game loop and should probably be moved elsewhere.
        public static long GetRealTime()
        {
            return Stopwatch.GetTimestamp() / _stopwatchFrequencyMilliseconds;
        }

        public static bool Init()
        {
            if (Interlocked.CompareExchange(ref _running, true, false))
                return false;

            TickDuration = Properties.GAME_LOOP_TICK_RATE;

            _gameLoopThread = new Thread(new ThreadStart(Run))
            {
                Name = THREAD_NAME,
                IsBackground = true
            };
            _gameLoopThread.Start();

            if (DYNAMIC_BUSY_WAIT_THRESHOLD)
            {
                _tickPacer = new(TickDuration);
                _tickPacer.Start();
            }

            return true;
        }

        public static void Exit()
        {
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            if (Thread.CurrentThread != _gameLoopThread && _gameLoopThread.IsAlive)
                _gameLoopThread.Join();

            _tickPacer.Stop();
            _threadPool.Dispose();
        }

        public static List<(int, double)> GetAverageTps()
        {
            return _tickPacer.Stats.GetAverageTicks(GameLoopTime);
        }

        public static void ExecuteForEach<T>(List<T> items, int toExclusive, Action<T> action)
        {
            _threadPool.ExecuteForEach(items, toExclusive, action);
        }

        public static T GetForTick<T>(PooledObjectKey poolKey, Action<T> initializer) where T : IPooledObject<T>, new()
        {
            return _threadPool.GetForTick(poolKey, initializer);
        }

        private static void Run()
        {
            if (Environment.ProcessorCount == 1)
                _threadPool = new GameLoopThreadPoolSingleThreaded();
            else
                _threadPool = new GameLoopThreadPoolMultiThreaded(Environment.ProcessorCount);

            _threadPool.Init(); // Must be done from the game loop thread.
            BuildTickSequence();

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
            Diagnostics.StartPerfCounter(nameof(GameLoop));

            foreach (var (service, tickAction, profileKey) in _tickSequence)
                TickServiceAction(service, tickAction, profileKey);

            Diagnostics.StopPerfCounter(nameof(GameLoop));
            Diagnostics.Tick();

            static void TickServiceAction(IGameService service, Action tickAction, string profileKey)
            {
                ActiveService = profileKey;
                GameServiceContext.Current.Value = service;
                Diagnostics.StartPerfCounter(ActiveService);

                try
                {
                    tickAction();
                }
                finally
                {
                    Diagnostics.StopPerfCounter(ActiveService);
                    GameServiceContext.Current.Value = null;
                    ActiveService = string.Empty;
                }
            }
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
            AddStep(EffectListService.Instance, EffectListService.Instance.BeginTick);
            AddStep(EffectListService.Instance, EffectListService.Instance.EndTick);
            AddStep(MovementService.Instance, MovementService.Instance.Tick);
            AddStep(ZoneService.Instance, ZoneService.Instance.Tick);
            AddStep(CraftingService.Instance, CraftingService.Instance.Tick);
            AddStep(ReaperService.Instance, ReaperService.Instance.Tick);
            AddStep(ClientService.Instance, ClientService.Instance.EndTick);
            AddStep(DailyQuestService.Instance, DailyQuestService.Instance.Tick);
            AddStep(WeeklyQuestService.Instance, WeeklyQuestService.Instance.Tick);
            AddStep(MonthlyQuestService.Instance, MonthlyQuestService.Instance.Tick);

            static void AddStep(IGameService service, Action action)
            {
                string methodName = action.Method.Name;
                string profileKey = methodName == nameof(IGameService.Tick) ? service.ServiceName : $"{service.ServiceName}.{methodName}";
                _tickSequence.Add((service, action, profileKey));
            }
        }
    }
}
