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
        private Lock _lock = new();
        private bool _running;

        public void Start()
        {
            lock (_lock)
            {
                if (_running)
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
                _running = true;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_running)
                    return;

                if (Thread.CurrentThread != _thread)
                {
                    _cancellationTokenSource.Cancel();
                    _thread.Join();
                }

                _running = false;
                _thread = null;
                _loggingQueue.Dispose();
                _loggingQueue = null;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                _logger = null;
            }
        }

        public void EnqueueMessage(LogEntry logEntry)
        {
            if (!_running)
                return;

            try
            {
                _loggingQueue.Add(logEntry);
            }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
        }

        public void TryEnqueueMessage(LogEntry logEntry)
        {
            if (!_running)
                return;

            try
            {
                _loggingQueue.TryAdd(logEntry);
            }
            catch (NullReferenceException) { }
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
                    if (_logger.IsInfoEnabled)
                        _logger.Info($"Thread \"{_thread.Name}\" was cancelled");
                }
                catch (ThreadInterruptedException)
                {
                    if (_logger.IsInfoEnabled)
                        _logger.Info($"Thread \"{_thread.Name}\" was interrupted");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Critical error encountered in {nameof(LogEntryQueueProcessor)}. Disabling logging:{Environment.NewLine}{e}");
                    return;
                }
            }

            // Clear the queue.
            while (_loggingQueue.TryTake(out LogEntry logEntry))
                logEntry.Log();
        }
    }
}
