using System;

namespace DOL.Logging
{
    public class Logger
    {
        // In the unlikely event that the queue is full:
        // Trace, Info, Debug, and Warn messages will be discarded.
        // Error, and Fatal will instead block.

        private LogEntryQueueProcessor _queueProcessor;

        public virtual bool IsTraceEnabled { get; }
        public virtual bool IsInfoEnabled { get; }
        public virtual bool IsDebugEnabled { get; }
        public virtual bool IsWarnEnabled { get; }
        public virtual bool IsErrorEnabled { get; }
        public virtual bool IsFatalEnabled { get; }

        public Logger(LogEntryQueueProcessor queueProcessor)
        {
            _queueProcessor = queueProcessor;
        }

        public virtual void Log(ELogLevel level, string message) { }
        public virtual void Log(ELogLevel level, string message, Exception exception) { }
        public virtual void Log(ELogLevel level, string message, params ReadOnlySpan<object> args) { }

        public void Trace(string message)
        {
            TryEnqueueMessage(ELogLevel.Trace, message);
        }

        public void Trace(Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Trace, exception);
        }

        public void Trace(string message, Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Trace, message, exception);
        }

        public void TraceFormat(string message, params ReadOnlySpan<object> args)
        {
            TryEnqueueMessage(ELogLevel.Trace, message, args);
        }

        public void Info(string message)
        {
            TryEnqueueMessage(ELogLevel.Info, message);
        }

        public void Info(Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Info, exception);
        }

        public void Info(string message, Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Info, message, exception);
        }

        public void InfoFormat(string message, params ReadOnlySpan<object> args)
        {
            TryEnqueueMessage(ELogLevel.Info, message, args);
        }

        public void Debug(string message)
        {
            TryEnqueueMessage(ELogLevel.Debug, message);
        }

        public void Debug(Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Debug, exception);
        }

        public void Debug(string message, Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Debug, message, exception);
        }

        public void DebugFormat(string message, params ReadOnlySpan<object> args)
        {
            TryEnqueueMessage(ELogLevel.Debug, message, args);
        }

        public void Warn(string message)
        {
            TryEnqueueMessage(ELogLevel.Warning, message);
        }

        public void Warn(Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Warning, exception);
        }

        public void Warn(string message, Exception exception)
        {
            TryEnqueueMessage(ELogLevel.Warning, message, exception);
        }

        public void WarnFormat(string message, params ReadOnlySpan<object> args)
        {
            TryEnqueueMessage(ELogLevel.Warning, message, args);
        }

        public void Error(string message)
        {
            EnqueueMessage(ELogLevel.Error, message);
        }

        public void Error(Exception exception)
        {
            EnqueueMessage(ELogLevel.Error, exception);
        }

        public void Error(string message, Exception exception)
        {
            EnqueueMessage(ELogLevel.Error, message, exception);
        }

        public void ErrorFormat(string message, params ReadOnlySpan<object> args)
        {
            EnqueueMessage(ELogLevel.Error, message, args);
        }

        public void Fatal(string message)
        {
            EnqueueMessage(ELogLevel.Fatal, message);
        }

        public void Fatal(Exception exception)
        {
            EnqueueMessage(ELogLevel.Fatal, exception);
        }

        public void Fatal(string message, Exception exception)
        {
            EnqueueMessage(ELogLevel.Fatal, message, exception);
        }

        public void FatalFormat(string message, params ReadOnlySpan<object> args)
        {
            EnqueueMessage(ELogLevel.Fatal, message, args);
        }

        private void EnqueueMessage(ELogLevel level, string message)
        {
            _queueProcessor.EnqueueMessage(LogEntryFactory.Create(this, level, message));
        }

        private void EnqueueMessage(ELogLevel level, Exception exception)
        {
            _queueProcessor.EnqueueMessage(LogEntryFactory.Create(this, level, string.Empty, exception));
        }

        private void EnqueueMessage(ELogLevel level, string message, Exception exception)
        {
            _queueProcessor.EnqueueMessage(LogEntryFactory.Create(this, level, message, exception));
        }

        private void EnqueueMessage(ELogLevel level, string message, params ReadOnlySpan<object> args)
        {
            _queueProcessor.EnqueueMessage(LogEntryFactory.Create(this, level, message, args));
        }

        private void TryEnqueueMessage(ELogLevel level, string message)
        {
            _queueProcessor.TryEnqueueMessage(LogEntryFactory.Create(this, level, message));
        }

        private void TryEnqueueMessage(ELogLevel level, Exception exception)
        {
            _queueProcessor.TryEnqueueMessage(LogEntryFactory.Create(this, level, string.Empty, exception));
        }

        private void TryEnqueueMessage(ELogLevel level, string message, Exception exception)
        {
            _queueProcessor.TryEnqueueMessage(LogEntryFactory.Create(this, level, message, exception));
        }

        private void TryEnqueueMessage(ELogLevel level, string message, params ReadOnlySpan<object> args)
        {
            _queueProcessor.TryEnqueueMessage(LogEntryFactory.Create(this, level, message, args));
        }
    }

    public class NullLogger : Logger
    {
        public override bool IsTraceEnabled => false;
        public override bool IsInfoEnabled => false;
        public override bool IsDebugEnabled => false;
        public override bool IsWarnEnabled => false;
        public override bool IsErrorEnabled => false;
        public override bool IsFatalEnabled => false;

        public NullLogger(LogEntryQueueProcessor queueProcessor) : base(queueProcessor) { }

        public override void Log(ELogLevel level, string message) { }
        public override void Log(ELogLevel level, string message, Exception exception) { }
        public override void Log(ELogLevel level, string message, params ReadOnlySpan<object> args) { }
    }

    public class ConsoleLogger : Logger
    {
        private string _name;

        public override bool IsTraceEnabled => true;
        public override bool IsInfoEnabled => true;
        public override bool IsDebugEnabled => true;
        public override bool IsWarnEnabled => true;
        public override bool IsErrorEnabled => true;
        public override bool IsFatalEnabled => true;

        public ConsoleLogger(string name, LogEntryQueueProcessor queueProcessor) : base(queueProcessor)
        {
            _name = name;
        }

        public override void Log(ELogLevel level, string message)
        {
            Console.WriteLine($"{level,-5} | {_name} | {message}");
        }

        public override void Log(ELogLevel level, string message, Exception exception)
        {
            Log(level, $"{message}{Environment.NewLine}{exception}");
        }

        public override void Log(ELogLevel level, string message, params ReadOnlySpan<object> args)
        {
            Log(level, string.Format(message, args));
        }
    }

    public class NLogLogger : Logger
    {
        private NLog.Logger _logger;

        public override bool IsTraceEnabled => _logger.IsTraceEnabled;
        public override bool IsInfoEnabled => _logger.IsInfoEnabled;
        public override bool IsDebugEnabled => _logger.IsDebugEnabled;
        public override bool IsWarnEnabled => _logger.IsWarnEnabled;
        public override bool IsErrorEnabled => _logger.IsErrorEnabled;
        public override bool IsFatalEnabled => _logger.IsFatalEnabled;

        public NLogLogger(string name, LogEntryQueueProcessor queueProcessor) : base(queueProcessor)
        {
            _logger = NLog.LogManager.GetLogger(name);
        }

        public override void Log(ELogLevel level, string message)
        {
            switch (level)
            {
                case ELogLevel.Trace:
                {
                    _logger.Trace(message);
                    break;
                }
                case ELogLevel.Info:
                {
                    _logger.Info(message);
                    break;
                }
                case ELogLevel.Debug:
                {
                    _logger.Debug(message);
                    break;
                }
                case ELogLevel.Warning:
                {
                    _logger.Warn(message);
                    break;
                }
                case ELogLevel.Error:
                {
                    _logger.Error(message);
                    break;
                }
                case ELogLevel.Fatal:
                {
                    _logger.Fatal(message);
                    break;
                }
            }
        }

        public override void Log(ELogLevel level, string message, Exception exception)
        {
            switch (level)
            {
                case ELogLevel.Trace:
                {
                    _logger.Trace(exception, message);
                    break;
                }
                case ELogLevel.Info:
                {
                    _logger.Info(exception, message);
                    break;
                }
                case ELogLevel.Debug:
                {
                    _logger.Debug(exception, message);
                    break;
                }
                case ELogLevel.Warning:
                {
                    _logger.Warn(exception, message);
                    break;
                }
                case ELogLevel.Error:
                {
                    _logger.Error(exception, message);
                    break;
                }
                case ELogLevel.Fatal:
                {
                    _logger.Fatal(exception, message);
                    break;
                }
            }
        }

        public override void Log(ELogLevel level, string message, params ReadOnlySpan<object> args)
        {
            switch (level)
            {
                case ELogLevel.Trace:
                {
                    _logger.Trace(message, args);
                    break;
                }
                case ELogLevel.Info:
                {
                    _logger.Info(message, args);
                    break;
                }
                case ELogLevel.Debug:
                {
                    _logger.Debug(message, args);
                    break;
                }
                case ELogLevel.Warning:
                {
                    _logger.Warn(message, args);
                    break;
                }
                case ELogLevel.Error:
                {
                    _logger.Error(message, args);
                    break;
                }
                case ELogLevel.Fatal:
                {
                    _logger.Fatal(message, args);
                    break;
                }
            }
        }
    }
}
