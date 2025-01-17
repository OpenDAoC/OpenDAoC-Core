using System;

namespace DOL.Logging
{
    public static class LogEntryFactory
    {
        public static LogEntry Create(Logger logger, ELogLevel level, string message)
        {
            return new LogEntry(logger, level, message);
        }

        public static LogEntry Create(Logger logger, ELogLevel level, string message, Exception exception)
        {
            return new LogEntryWithException(logger, level, message, exception);
        }

        public static LogEntry Create(Logger logger, ELogLevel level, string message, params object[] args)
        {
            return new LogEntryWithArgs(logger, level, message, args);
        }
    }
}
