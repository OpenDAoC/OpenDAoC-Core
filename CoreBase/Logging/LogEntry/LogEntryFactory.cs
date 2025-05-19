using System;
using Microsoft.Extensions.ObjectPool;

namespace DOL.Logging
{
    public static class LogEntryFactory
    {
        private static ObjectPool<LogEntry> _pool = new DefaultObjectPool<LogEntry>(new DefaultPooledObjectPolicy<LogEntry>());

        public static LogEntry Create(Logger logger, ELogLevel level, string message)
        {
            return _pool.Get().Initialize(logger, level, message);
        }

        public static LogEntry Create(Logger logger, ELogLevel level, string message, Exception exception)
        {
            return _pool.Get().Initialize(logger, level, message, exception);
        }

        public static LogEntry Create(Logger logger, ELogLevel level, string message, params object[] args)
        {
            return _pool.Get().Initialize(logger, level, message, args);
        }

        public static void Return(LogEntry logEntry)
        {
            _pool.Return(logEntry);
        }
    }
}
