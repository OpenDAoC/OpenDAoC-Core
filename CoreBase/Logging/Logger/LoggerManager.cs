﻿using System;
using System.IO;
using System.Threading;
using System.Xml;

namespace DOL.Logging
{
    public abstract class LoggerManager
    {
        private static Lock _lock = new();
        private static ELogLibrary _loggingLibrary;
        private static ILoggerFactory _loggerFactory;
        private static LogEntryQueueProcessor _queueProcessor;
        private static bool _initialized;

        public static bool Initialize(string configurationFilePath)
        {
            return InitializeWithExplicitLibrary(configurationFilePath, GuessLibraryFromFile());

            ELogLibrary GuessLibraryFromFile()
            {
                if (string.IsNullOrEmpty(configurationFilePath))
                    return ELogLibrary.Console;

                using XmlReader reader = XmlReader.Create(configurationFilePath);

                while (reader.Read())
                {
                    if (reader.NodeType is XmlNodeType.Element)
                    {
                        if (reader.Name.Equals("nlog", StringComparison.OrdinalIgnoreCase))
                            return ELogLibrary.NLog;

                        if (reader.Name.Equals("log4net", StringComparison.OrdinalIgnoreCase))
                            return ELogLibrary.Log4net;
                    }
                }

                return ELogLibrary.Console;
            }
        }

        public static bool InitializeWithExplicitLibrary(string configurationFilePath, ELogLibrary loggingLibrary)
        {
            lock (_lock)
            {
                if (_initialized)
                    throw new InvalidOperationException($"Logging Manager has already been initialized (file: {configurationFilePath}) (library: {_loggingLibrary})");

                _loggingLibrary = loggingLibrary;

                try
                {
                    Console.WriteLine($"Initializing Logging Manager (file: '{configurationFilePath}') (library: '{_loggingLibrary}')");

                    _loggerFactory = _loggingLibrary switch
                    {
                        ELogLibrary.None => new NullLoggerFactory(),
                        ELogLibrary.Console => new ConsoleLoggerFactory(),
                        ELogLibrary.NLog => new NLogLoggerFactory(configurationFilePath),
                        ELogLibrary.Log4net => new Log4netLoggerFactory(configurationFilePath),
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
                _initialized = false;

                if (_queueProcessor != null)
                {
                    _queueProcessor.Stop();
                    _queueProcessor = null;
                }

                if (_loggerFactory != null)
                {
                    _loggerFactory.Shutdown();
                    _loggerFactory = null;
                }

                _loggingLibrary = ELogLibrary.None;
            }
        }

        public static Logger Create(Type type)
        {
            return Create(type.ToString());
        }

        public static Logger Create(string name)
        {
            if (!_initialized)
                throw new InvalidOperationException("Logging Manager has not been initialized");

            return _loggerFactory.Create(name);
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

        private class Log4netLoggerFactory : ILoggerFactory
        {
            public Log4netLoggerFactory(string configurationFilePath)
            {
                log4net.Config.XmlConfigurator.Configure(new FileInfo(configurationFilePath));
            }

            public Logger Create(string name)
            {
                return new Log4netLogger(name, _queueProcessor);
            }

            public void Shutdown()
            {
                log4net.LogManager.Shutdown();
            }
        }
    }
}
