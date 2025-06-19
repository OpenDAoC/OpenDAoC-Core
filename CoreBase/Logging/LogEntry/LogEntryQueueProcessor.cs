using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

namespace DOL.Logging
{
    public class LogEntryQueueProcessor
    {
        private const int CAPACITY = 10000;

        private Thread _thread;
        private BlockingCollection<LogEntry> _loggingQueue;
        private CancellationTokenSource _cancellationTokenSource;
        private Logger _logger;
        private bool _running;

        public void Start()
        {
            if (Interlocked.CompareExchange(ref _running, true, false))
                return;

            _logger = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
            _cancellationTokenSource = new();
            _loggingQueue = new(new ConcurrentQueue<LogEntry>(), CAPACITY);
            _thread = new Thread(new ThreadStart(Run))
            {
                Name = nameof(LogEntryQueueProcessor),
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _thread.Start();
        }

        public void Stop()
        {
            if (!Interlocked.CompareExchange(ref _running, false, true))
                return;

            _cancellationTokenSource.Cancel();

            if (Thread.CurrentThread != _thread && _thread.IsAlive)
                _thread.Join();

            _loggingQueue.Dispose();
            _cancellationTokenSource.Dispose();
        }

        public void EnqueueMessage(LogEntry logEntry)
        {
            try
            {
                _loggingQueue.Add(logEntry);
            }
            catch (ObjectDisposedException) { }
        }

        public void TryEnqueueMessage(LogEntry logEntry)
        {
            try
            {
                _loggingQueue.TryAdd(logEntry);
            }
            catch (ObjectDisposedException) { }
        }

        private void Run()
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _loggingQueue.Take(cancellationToken).Log();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ThreadInterruptedException)
                {
                    if (_logger.IsWarnEnabled)
                        _logger.Warn($"Thread \"{Thread.CurrentThread.Name}\" was interrupted");

                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Critical error encountered in {nameof(LogEntryQueueProcessor)}. Disabling logging:{Environment.NewLine}{e}");
                    return;
                }
            }

            if (_logger.IsInfoEnabled)
                _logger.Info($"Thread \"{Thread.CurrentThread.Name}\" is stopping");

            while (_loggingQueue.TryTake(out LogEntry logEntry))
                logEntry.Log();
        }
    }
}
