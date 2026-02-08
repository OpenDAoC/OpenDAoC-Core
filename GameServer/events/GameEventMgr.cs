using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;

namespace DOL.Events
{
    /// <summary>
    /// Manages per-object and global event handlers.
    /// </summary>
    public class GameEventMgr
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static GameEventMgr _soleInstance = new();

        private readonly Dictionary<object, DOLEventHandlerCollection> _gameObjectEventCollections = new();
        private readonly DOLEventHandlerCollection _globalHandlerCollection = new();
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

        private static DOLEventHandlerCollection GlobalHandlerCollection => _soleInstance._globalHandlerCollection;
        private static Dictionary<object, DOLEventHandlerCollection> GameObjectEventCollections => _soleInstance._gameObjectEventCollections;
        private static ReaderWriterLockSlim Lock => _soleInstance._lock;

        public static int NumGlobalHandlers => GlobalHandlerCollection.Count;

        public static int NumObjectHandlers
        {
            get
            {
                int numHandlers = 0;
                Lock.EnterReadLock();

                try
                {
                    foreach (DOLEventHandlerCollection col in GameObjectEventCollections.Values)
                        numHandlers += col.Count;
                }
                finally
                {
                    Lock.ExitReadLock();
                }

                return numHandlers;
            }
        }

        public static void LoadTestDouble(GameEventMgr testDouble)
        {
            _soleInstance = testDouble;
        }

        public static void RegisterGlobalEvents(Assembly asm, Type attribute, DOLEvent e)
        {
            ArgumentNullException.ThrowIfNull(asm);
            ArgumentNullException.ThrowIfNull(attribute);
            ArgumentNullException.ThrowIfNull(e);

            foreach (Type type in asm.GetTypes())
            {
                if (!type.IsClass)
                    continue;

                foreach (MethodInfo mInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    object[] myAttribs = mInfo.GetCustomAttributes(attribute, false);

                    if (myAttribs.Length != 0)
                    {
                        try
                        {
                            GlobalHandlerCollection.AddHandler(e, (DOLEventHandler) Delegate.CreateDelegate(typeof(DOLEventHandler), mInfo));
                        }
                        catch (Exception ex)
                        {
                            if (log.IsErrorEnabled)
                                log.Error($"Error registering global event. Method: {type.FullName}.{mInfo.Name}", ex);
                        }
                    }
                }
            }
        }

        public static void AddHandler(DOLEvent e, DOLEventHandler del)
        {
            AddHandler(e, del, false);
        }

        public static void AddHandlerUnique(DOLEvent e, DOLEventHandler del)
        {
            AddHandler(e, del, true);
        }

        public static void AddHandlerUnique(object obj, DOLEvent e, DOLEventHandler del)
        {
            AddHandler(obj, e, del, true);
        }

        private static void AddHandler(DOLEvent e, DOLEventHandler del, bool unique)
        {
            ArgumentNullException.ThrowIfNull(e);
            ArgumentNullException.ThrowIfNull(del);

            if (unique)
                GlobalHandlerCollection.AddHandlerUnique(e, del);
            else
                GlobalHandlerCollection.AddHandler(e, del);
        }

        public static void AddHandler(object obj, DOLEvent e, DOLEventHandler del)
        {
            AddHandler(obj, e, del, false);
        }

        private static void AddHandler(object obj, DOLEvent e, DOLEventHandler del, bool unique)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(e);
            ArgumentNullException.ThrowIfNull(del);

            if (!e.IsValidFor(obj))
                throw new ArgumentException("Object is not valid for this event type", nameof(obj));

            Lock.EnterUpgradeableReadLock();

            try
            {
                if (!GameObjectEventCollections.TryGetValue(obj, out DOLEventHandlerCollection col))
                {
                    col = new DOLEventHandlerCollection();
                    Lock.EnterWriteLock();

                    try
                    {
                        GameObjectEventCollections.Add(obj, col);
                    }
                    finally
                    {
                        Lock.ExitWriteLock();
                    }
                }

                if (unique)
                    col.AddHandlerUnique(e, del);
                else
                    col.AddHandler(e, del);
            }
            finally
            {
                Lock.ExitUpgradeableReadLock();
            }
        }

        public static void RemoveHandler(DOLEvent e, DOLEventHandler del)
        {
            ArgumentNullException.ThrowIfNull(e);
            ArgumentNullException.ThrowIfNull(del);

            GlobalHandlerCollection.RemoveHandler(e, del);
        }

        public static void RemoveHandler(object obj, DOLEvent e, DOLEventHandler del)
        {
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(e);
            ArgumentNullException.ThrowIfNull(del);

            DOLEventHandlerCollection col = null;
            Lock.EnterReadLock();

            try
            {
                GameObjectEventCollections.TryGetValue(obj, out col);
            }
            finally
            {
                Lock.ExitReadLock();
            }

            col?.RemoveHandler(e, del);
        }

        public static void RemoveAllHandlersForObject(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            Lock.EnterWriteLock();

            try
            {
                GameObjectEventCollections.Remove(obj);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public static void RemoveAllHandlers()
        {
            Lock.EnterWriteLock();

            try
            {
                GameObjectEventCollections.Clear();
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            GlobalHandlerCollection.RemoveAllHandlers();
        }

        public static void Notify(DOLEvent e)
        {
            Notify(e, null, null);
        }

        public static void Notify(DOLEvent e, object sender)
        {
            Notify(e, sender, null);
        }

        public static void Notify(DOLEvent e, EventArgs args)
        {
            Notify(e, null, args);
        }

        public static void Notify(DOLEvent e, object sender, EventArgs eArgs)
        {
            ArgumentNullException.ThrowIfNull(e);

            ECS.Debug.Diagnostics.BeginGameEventMgrNotify();

            if (sender != null)
            {
                DOLEventHandlerCollection col;
                Lock.EnterReadLock();

                try
                {
                    GameObjectEventCollections.TryGetValue(sender, out col);
                }
                finally
                {
                    Lock.ExitReadLock();
                }

                col?.Notify(e, sender, eArgs);
            }

            GlobalHandlerCollection.Notify(e, sender, eArgs);
            ECS.Debug.Diagnostics.EndGameEventMgrNotify(e);
        }
    }
}
