using System;
using System.Threading;
using System.Xml;

namespace DOL.Logging
{
    public abstract class LoggerManager
    {
        private static Lock _lock = new();
        private static LogLibrary _loggingLibrary;
        private static ILoggerFactory _loggerFactory;
        private static LogEntryQueueProcessor _queueProcessor;
        private static bool _initialized;

        public static bool Initialize(string configurationFilePath)
        {
            return InitializeWithExplicitLibrary(configurationFilePath, GuessLibraryFromFile());

            LogLibrary GuessLibraryFromFile()
            {
                if (string.IsNullOrEmpty(configurationFilePath))
                    return LogLibrary.Console;

                using XmlReader reader = XmlReader.Create(configurationFilePath);

                while (reader.Read())
                {
                    if (reader.NodeType is XmlNodeType.Element)
                    {
                        if (reader.Name.Equals("nlog", StringComparison.OrdinalIgnoreCase))
                            return LogLibrary.NLog;
                    }
                }

                return LogLibrary.Console;
            }
        }

        public static bool InitializeWithExplicitLibrary(string configurationFilePath, LogLibrary loggingLibrary)
        {
            lock (_lock)
            {
                if (_initialized)
                    return false;

                _loggingLibrary = loggingLibrary;

                try
                {
                    Console.WriteLine($"Initializing Logging Manager (file: '{configurationFilePath}') (library: '{_loggingLibrary}')");

                    _loggerFactory = _loggingLibrary switch
                    {
                        LogLibrary.None => new NullLoggerFactory(),
                        LogLibrary.Console => new ConsoleLoggerFactory(),
                        LogLibrary.NLog => new NLogLoggerFactory(configurationFilePath),
                        _ => throw new NotImplementedException($"{_loggingLibrary}")
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Couldn't initialize Logging Manager (path: {configurationFilePath}){Environment.NewLine}{e}");
                    return false;
                }
            }

            _initialized = true;
            _queueProcessor = new();
            _queueProcessor.Start();
            return true;
        }

        public static void Stop()
        {
            lock (_lock)
            {
                if (!_initialized)
                    return;

                _queueProcessor.Stop();
                _loggerFactory.Shutdown();
                _loggingLibrary = LogLibrary.None;
            }
        }

        public static Logger Create(Type type)
        {
            return Create(type.ToString());
        }

        public static Logger Create(string name)
        {
            return !_initialized ?  new NullLogger(null) : _loggerFactory.Create(name);
        }

        public interface ILoggerFactory
        {
            Logger Create(string name);
            void Shutdown();
        }

        private class NullLoggerFactory : ILoggerFactory
        {
            public Logger Create(string name)
            {
                return new NullLogger(_queueProcessor);
            }

            public void Shutdown() { }
        }

        private class ConsoleLoggerFactory : ILoggerFactory
        {
            public Logger Create(string name)
            {
                return new ConsoleLogger(name, _queueProcessor);
            }

            public void Shutdown() { }
        }

        private class NLogLoggerFactory : ILoggerFactory
        {
            public NLogLoggerFactory(string configurationFilePath)
            {
                NLog.Config.XmlLoggingConfiguration config = new(configurationFilePath);
                NLog.LogManager.Configuration = config;
            }

            public Logger Create(string name)
            {
                return new NLogLogger(name, _queueProcessor);
            }

            public void Shutdown()
            {
                NLog.LogManager.Shutdown();
            }
        }
    }
}
