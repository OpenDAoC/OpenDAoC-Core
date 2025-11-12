using System;

namespace DOL.Logging
{
    public static class LogEntryFactory
    {
        public static LogEntry Create(Logger logger, ELogLevel level, string message)
        {
            LogEntry logEntry = new();
            logEntry.Initialize(logger, level, message);
            return logEntry;
        }

        public static LogEntry Create(Logger logger, ELogLevel level, string message, Exception exception)
        {
            LogEntry logEntry = new();
            logEntry.Initialize(logger, level, message, exception);
            return logEntry;
        }

        public static LogEntry Create(Logger logger, ELogLevel level, string message, params ReadOnlySpan<object> args)
        {
            LogEntry logEntry = new();
            logEntry.Initialize(logger, level, message, args);
            return logEntry;
        }
    }
}
