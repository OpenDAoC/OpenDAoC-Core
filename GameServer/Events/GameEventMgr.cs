using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;

namespace DOL.Events
{
	/// <summary>
	/// Manages per-object and global event handlers.
	/// </summary>
	public class GameEventMgr
	{
		private static GameEventMgr soleInstance = new GameEventMgr();

		public static void LoadTestDouble(GameEventMgr testDouble) { soleInstance = testDouble; }

		/// <summary>
		/// How long to wait for a lock acquisition before failing.
		/// </summary>
		private const int LOCK_TIMEOUT = 3000;

		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Holds a list of event handler collections for single gameobjects
		/// </summary>
		protected static Dictionary<object, CoreEventHandlerCollection> m_gameObjectEventCollections;

		public static int NumObjectHandlers
		{
			get
			{
				int numHandlers = 0;
				Dictionary<object, CoreEventHandlerCollection> cloneGameObjectEventCollections = new Dictionary<object,CoreEventHandlerCollection>(m_gameObjectEventCollections);

				foreach (CoreEventHandlerCollection handler in cloneGameObjectEventCollections.Values)
				{
					numHandlers += handler.Count;
				}

				return numHandlers;
			}
		}

		private readonly CoreEventHandlerCollection m_globalHandlerCollection;

		public static CoreEventHandlerCollection GlobalHandlerCollection => soleInstance.m_globalHandlerCollection;

		/// <summary>
		/// Get the number of global event handlers
		/// </summary>
		public static int NumGlobalHandlers
		{
			get { return GlobalHandlerCollection.Count; }
		}

		/// <summary>
		/// A lock used to access the event collections of livings
		/// </summary>
		private readonly ReaderWriterLockSlim m_lock;

		private static ReaderWriterLockSlim Lock => soleInstance.m_lock;

		protected GameEventMgr()
		{
			m_lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
			m_gameObjectEventCollections = new Dictionary<object, CoreEventHandlerCollection>();
			m_globalHandlerCollection = new CoreEventHandlerCollection();
		}

		/// <summary>
		/// Registers some global events that are specified by attributes
		/// </summary>
		/// <param name="asm">The assembly to search through</param>
		/// <param name="attribute">The custom attribute to search for</param>
		/// <param name="e">The event to register in case the custom attribute is found</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		public static void RegisterGlobalEvents(Assembly asm, Type attribute, CoreEvent e)
		{
			if(asm == null)
				throw new ArgumentNullException("asm", "No assembly given to search for global event handlers!");

			if(attribute == null)
				throw new ArgumentNullException("attribute", "No attribute given!");

			if(e == null)
				throw new ArgumentNullException("e", "No event type given!");

			foreach(Type type in asm.GetTypes())
			{
				if(!type.IsClass) continue;
				foreach(MethodInfo mInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
				{
					object[] myAttribs = mInfo.GetCustomAttributes(attribute, false);
					if(myAttribs.Length != 0)
					{
						try
						{
							GlobalHandlerCollection.AddHandler(e, (CoreEventHandler) Delegate.CreateDelegate(typeof(CoreEventHandler), mInfo));
						}
						catch(Exception ex)
						{
							if(log.IsErrorEnabled)
								log.Error("Error registering global event. Method: " + type.FullName + "." + mInfo.Name, ex);
						}
					}
				}
			}
		}

		/// <summary>
		/// Registers a single global event handler.
		/// The global event handlers will be called for ALL events,
		/// so use them wisely, they might incure a big performance
		/// hit if used unwisely.
		/// </summary>
		/// <param name="e">The event type to register for</param>
		/// <param name="del">The event handler to register for this event type</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		public static void AddHandler(CoreEvent e, CoreEventHandler del)
		{
			AddHandler(e, del, false);
		}

		/// <summary>
		/// Registers a single global event handler.
		/// The global event handlers will be called for ALL events,
		/// so use them wisely, they might incure a big performance
		/// hit if used unwisely.
		/// If an equal handler has already been added, nothing will be done
		/// </summary>
		/// <param name="e">The event type to register for</param>
		/// <param name="del">The event handler to register for this event type</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		public static void AddHandlerUnique(CoreEvent e, CoreEventHandler del)
		{
			AddHandler(e, del, true);
		}

		/// <summary>
		/// Registers a single object event handler.
		/// The global event handlers will be called for ALL events,
		/// so use them wisely, they might incure a big performance
		/// hit if used unwisely.
		/// If an equal handler has already been added, nothing will be done
		/// </summary>
		/// <param name="e">The event type to register for</param>
		/// <param name="del">The event handler to register for this event type</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		public static void AddHandlerUnique(object obj, CoreEvent e, CoreEventHandler del)
		{
			AddHandler(obj, e, del, true);
		}
		
		/// <summary>
		/// Registers a single global event handler.
		/// The global event handlers will be called for ALL events,
		/// so use them wisely, they might incure a big performance
		/// hit if used unwisely.
		/// </summary>
		/// <param name="e">The event type to register for</param>
		/// <param name="del">The event handler to register for this event type</param>
		/// <param name="unique">Flag wether event shall be added unique or not</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		private static void AddHandler(CoreEvent e, CoreEventHandler del, bool unique)
		{
			if(e == null)
				throw new ArgumentNullException("e", "No event type given!");

			if(del == null)
				throw new ArgumentNullException("del", "No event handler given!");

			if(unique)
			{
				GlobalHandlerCollection.AddHandlerUnique(e, del);
			}
			else
			{
				GlobalHandlerCollection.AddHandler(e, del);
			}
		}

		/// <summary>
		/// Registers a single local event handler.
		/// Local event handlers will only be called if the
		/// "sender" parameter in the Notify method equals
		/// the object for which a local event handler was
		/// registered.
		/// </summary>
		/// <remarks>
		/// Certain events will never have a local event handler.
		/// This happens if the Notify method is called without
		/// a sender parameter for example!
		/// </remarks>
		/// <param name="obj">The object that needs to be the sender of events</param>
		/// <param name="e">The event type to register for</param>
		/// <param name="del">The event handler to register for this event type</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		public static void AddHandler(object obj, CoreEvent e, CoreEventHandler del)
		{
			AddHandler(obj, e, del, false);
		}

		/// <summary>
		/// Registers a single local event handler.
		/// Local event handlers will only be called if the
		/// "sender" parameter in the Notify method equals
		/// the object for which a local event handler was
		/// registered.
		/// </summary>
		/// <remarks>
		/// Certain events will never have a local event handler.
		/// This happens if the Notify method is called without
		/// a sender parameter for example!
		/// </remarks>
		/// <param name="obj">The object that needs to be the sender of events</param>
		/// <param name="e">The event type to register for</param>
		/// <param name="del">The event handler to register for this event type</param>
		/// <param name="unique">Flag wether event shall be added unique or not</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		private static void AddHandler(object obj, CoreEvent e, CoreEventHandler del, bool unique)
		{
			if(obj == null)
				throw new ArgumentNullException("obj", "No object given!");

			if(e == null)
				throw new ArgumentNullException("e", "No event type given!");

			if(del == null)
				throw new ArgumentNullException("del", "No event handler given!");

			if(!e.IsValidFor(obj))
				throw new ArgumentException("Object is not valid for this event type", "obj");

			if(Lock.TryEnterUpgradeableReadLock(LOCK_TIMEOUT))
			{
				try
				{
					CoreEventHandlerCollection col;

					if(!m_gameObjectEventCollections.TryGetValue(obj, out col))
					{
						col = new CoreEventHandlerCollection();

						if (Lock.TryEnterWriteLock(LOCK_TIMEOUT))
						{
							try
							{
								m_gameObjectEventCollections.Add(obj, col);
							}
							finally
							{
								Lock.ExitWriteLock();
							}
						}
						else
						{
							log.ErrorFormat("Timeout exceeded on attempt to AddHandler for object: {0}, event: {1}", obj.ToString(), e.Name);
						}
					}

					if(unique)
					{
						col.AddHandlerUnique(e, del);
					}
					else
					{
						col.AddHandler(e, del);
					}
				}
				finally
				{
					Lock.ExitUpgradeableReadLock();
				}
			}
		}

		/// <summary>
		/// Removes a global event handler.
		/// You need to have registered the event before being able to remove it.
		/// </summary>
		/// <param name="e">The event type from which to deregister</param>
		/// <param name="del">The event handler to deregister for this event type</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		public static void RemoveHandler(CoreEvent e, CoreEventHandler del)
		{
			if(e == null)
				throw new ArgumentNullException("e", "No event type given!");

			if(del == null)
				throw new ArgumentNullException("del", "No event handler given!");

			GlobalHandlerCollection.RemoveHandler(e, del);
		}

		/// <summary>
		/// Removes a single event handler from an object.
		/// You need to have registered the event before being
		/// able to remove it.
		/// </summary>
		/// <param name="obj">The object that needs to be the sender of events</param>
		/// <param name="e">The event type from which to deregister</param>
		/// <param name="del">The event handler to deregister for this event type</param>
		/// <exception cref="ArgumentNullException">If one of the parameters is null</exception>
		public static void RemoveHandler(object obj, CoreEvent e, CoreEventHandler del)
		{
			if(obj == null)
				throw new ArgumentNullException("obj", "No object given!");

			if(e == null)
				throw new ArgumentNullException("e", "No event type given!");

			if(del == null)
				throw new ArgumentNullException("del", "No event handler given!");

			CoreEventHandlerCollection col = null;

			if (Lock.TryEnterReadLock(LOCK_TIMEOUT))
			{
				try
				{
					m_gameObjectEventCollections.TryGetValue(obj, out col);
				}
				finally
				{
					Lock.ExitReadLock();
				}
			}
			else
			{
				log.ErrorFormat("Timeout exceeded on attempt to RemoveHandler for object: {0}, event: {1}", obj.ToString(), e.Name);
			}

			if (col != null)
			{
				col.RemoveHandler(e, del);
			}
		}

		public static void RemoveAllHandlersForObject(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj", "No object given!");
			}

			CoreEventHandlerCollection col = null;

			if (Lock.TryEnterReadLock(LOCK_TIMEOUT))
			{
				try
				{
					m_gameObjectEventCollections.TryGetValue(obj, out col);
				}
				finally
				{
					Lock.ExitReadLock();
				}
			}
			else
			{
				log.ErrorFormat("Timeout exceeded (Read) on attempt to RemoveAllHandlersForObject: {0}", obj.ToString());
			}

			if (col != null)
			{
				col.RemoveAllHandlers();

				if (Lock.TryEnterWriteLock(LOCK_TIMEOUT))
				{
					try
					{
						m_gameObjectEventCollections.Remove(obj);
					}
					finally
					{
						Lock.ExitWriteLock();
					}
				}
				else
				{
					log.ErrorFormat("Timeout exceeded (Write) on attempt to RemoveAllHandlersForObject: {0}", obj.ToString());
				}
			}
		}

		/// <summary>
		/// Removes all event handlers
		/// </summary>
		/// <param name="deep">Specifies if all local registered event handlers
		/// should also be removed</param>
		public static void RemoveAllHandlers(bool deep)
		{
			if(deep)
			{
				if(Lock.TryEnterWriteLock(LOCK_TIMEOUT))
				{
					try
					{
						m_gameObjectEventCollections.Clear();
					}
					finally
					{
						Lock.ExitWriteLock();
					}
				}
				else
				{
					log.ErrorFormat("Timeout exceeded on attempt to RemoveAllHandlers.");
				}
			}

			GlobalHandlerCollection.RemoveAllHandlers();
		}

		/// <summary>
		/// Notifies all global event handlers of the occurance of a specific
		/// event type.
		/// </summary>
		/// <param name="e">The event type that occured</param>
		/// <exception cref="ArgumentNullException">If no event type given</exception>
		public static void Notify(CoreEvent e)
		{
			Notify(e, null, null);
		}

		/// <summary>
		/// Notifies all global and local event handlers of the occurance
		/// of a specific event type.
		/// </summary>
		/// <param name="e">The event type that occured</param>
		/// <param name="sender">The sender of this event</param>
		/// <exception cref="ArgumentNullException">If no event type given</exception>
		public static void Notify(CoreEvent e, object sender)
		{
			Notify(e, sender, null);
		}

		/// <summary>
		/// Notifies all global event handlers of the occurance
		/// of a specific event type with some event arguments.
		/// </summary>
		/// <param name="e">The event type that occured</param>
		/// <param name="args">The event arguments</param>
		/// <exception cref="ArgumentNullException">If no event type given</exception>
		public static void Notify(CoreEvent e, EventArgs args)
		{
			Notify(e, null, args);
		}

		/// <summary>
		/// Notifies all global and local event handlers of the occurance 
		/// of a specific event type with some event arguments!
		/// </summary>
		/// <param name="e">The event type that occured</param>
		/// <param name="sender">The sender of this event</param>
		/// <param name="eArgs">The event arguments</param>
		/// <exception cref="ArgumentNullException">If no event type given</exception>
		/// <remarks>Overwrite the EventArgs class to set own arguments</remarks>
		public static void Notify(CoreEvent e, object sender, EventArgs eArgs)
		{
			if(e == null)
				throw new ArgumentNullException("e", "No event type given!");

			/// [Takii - Atlas] Start Record cost of event for profiling.
			ECS.Debug.Diagnostics.BeginGameEventMgrNotify();

			// notify handlers bounded specifically to the sender
			if (sender != null)
			{
				CoreEventHandlerCollection col;

				if(Lock.TryEnterReadLock(LOCK_TIMEOUT))
				{
					try
					{
						m_gameObjectEventCollections.TryGetValue(sender, out col);
					}
					finally
					{
						Lock.ExitReadLock();
					}

					if(col != null)
						col.Notify(e, sender, eArgs);
				}
				else
				{
					log.ErrorFormat("Timeout exceeded on attempt to Notify event: {0} for object: {1}", e.Name, sender.ToString());
				}
			}

			// notify globally-bound handler
			GlobalHandlerCollection.Notify(e, sender, eArgs);


            /// [Takii - Atlas] Stop Record cost of event for profiling.
            ECS.Debug.Diagnostics.EndGameEventMgrNotify(e);
        }
    }
}
