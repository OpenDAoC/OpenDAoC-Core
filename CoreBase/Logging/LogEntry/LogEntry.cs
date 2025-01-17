using System;

namespace DOL.Logging
{
    public class LogEntry
    {
        public Logger Logger { get; }
        public ELogLevel Level { get; }
        public string Message { get; }

        public LogEntry(Logger logger, ELogLevel level, string message)
        {
            Logger = logger;
            Level = level;
            Message = message;
        }

        public virtual void Log()
        {
            Logger.Log(Level, Message);
        }
    }

    public class LogEntryWithException : LogEntry
    {
        public Exception Exception { get; }

        public LogEntryWithException(Logger logger, ELogLevel level, string message, Exception exception) : base(logger, level, message)
        {
            Exception = exception;
        }

        public override void Log()
        {
            Logger.Log(Level, Message, Exception);
        }
    }

    public class LogEntryWithArgs : LogEntry
    {
        public object[] Args { get; }

        public LogEntryWithArgs(Logger logger, ELogLevel level, string message, params object[] args) : base(logger, level, message)
        {
            Args = args;
        }

        public override void Log()
        {
            Logger.Log(Level, Message, Args);
        }
    }
}
