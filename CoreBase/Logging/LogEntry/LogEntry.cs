using System;
using System.Buffers;

namespace DOL.Logging
{
    public class LogEntry
    {
        private Action<LogEntry> _logAction;
        private int _argsCount;

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
            Exception = null;
            Args = null;
            _logAction = static e => e.Logger.Log(e.Level, e.Message);
            _argsCount = 0;
            return this;
        }

        public LogEntry Initialize(Logger logger, ELogLevel level, string message, Exception ex)
        {
            Logger = logger;
            Level = level;
            Message = message;
            Exception = ex;
            Args = null;
            _logAction = static e => e.Logger.Log(e.Level, e.Message, e.Exception);
            _argsCount = 0;
            return this;
        }

        public LogEntry Initialize(Logger logger, ELogLevel level, string message, params ReadOnlySpan<object> args)
        {
            Logger = logger;
            Level = level;
            Message = message;
            Exception = null;

            // Rent an array from the pool to hold the args until the background thread logs them.
            _argsCount = args.Length;
            Args = ArrayPool<object>.Shared.Rent(_argsCount);
            args.CopyTo(Args);

            _logAction = static e =>
            {
                try
                {
                    // Make sure to pass only the valid portion of the args array.
                    e.Logger.Log(e.Level, e.Message, e.Args.AsSpan(0, e._argsCount));
                }
                finally
                {
                    e.ReturnRentedArgs();
                }
            };

            return this;
        }

        public void Log()
        {
            _logAction(this);
        }

        public void ReturnRentedArgs()
        {
            // This method is for cases where the LogEntry is created but never processed, allowing for manual cleanup.
            if (Args != null)
            {
                ArrayPool<object>.Shared.Return(Args);
                Args = null;
            }
        }
    }
}
