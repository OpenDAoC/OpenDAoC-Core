using System;
using Microsoft.Extensions.ObjectPool;

namespace DOL.Logging
{
    public class LogEntry : IResettable
    {
        private Action<LogEntry> _logAction;

        public Logger Logger { get; private set; }
        public ELogLevel Level { get; private set; }
        public string Message { get; private set; }
        public Exception Exception { get; private set; }
        public object[] Args { get; private set; }

        public LogEntry Initialize(Logger logger, ELogLevel level, string message)
        {
            Logger = logger;
            Level = level;
            Message = message;
            _logAction = static e => e.Logger.Log(e.Level, e.Message);
            return this;
        }

        public LogEntry Initialize(Logger logger, ELogLevel level, string message, Exception ex)
        {
            Logger = logger;
            Level = level;
            Message = message;
            Exception = ex;
            _logAction = static e => e.Logger.Log(e.Level, e.Message, e.Exception);
            return this;
        }

        public LogEntry Initialize(Logger logger, ELogLevel level, string message, object[] args)
        {
            Logger = logger;
            Level = level;
            Message = message;
            Args = args;
            _logAction = static e => e.Logger.Log(e.Level, e.Message, e.Args);
            return this;
        }

        public void Log()
        {
            _logAction(this);
            LogEntryFactory.Return(this); // Return ourselves to the pool.
        }

        public bool TryReset()
        {
            _logAction = null;
            Logger = null;
            Level = default;
            Message = null;
            Exception = null;
            Args = null;
            return true;
        }
    }
}
