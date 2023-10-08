using System;
using System.Reflection;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using log4net;

namespace DOL.GS.Behaviour
{
    /// <summary>
    /// A trigger defines the circumstances under which a certain QuestAction is fired.
    /// This can be eTriggerAction.Interact, eTriggerAction.GiveItem, eTriggerAction.Attack, etc...
    /// Additional there are two variables to add the needed parameters for the triggertype (Item to give for GiveItem, NPC to interact for Interact, etc...). To fire a QuestAction at least one of the added triggers must be fulfilled. 
    /// </summary>        
    public abstract class ATrigger<TypeK, TypeI> : IBehaviorTrigger
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private TypeK k; //trigger keyword 
        private TypeI i;        
        private ETriggerType triggerType; // t## : trigger type, see following description (NONE:no trigger)        
		private GameLiving defaultNPC;
		private CoreEventHandler notifyHandler;		

        /// <summary>
        /// Trigger Keyword
        /// </summary>
        public TypeK K
        {
            get { return k; }
			set { k = value; }
        }

        /// <summary>
        /// Trigger Variable
        /// </summary>
        public TypeI I
        {
            get { return i; }
			set { i = value; }
        }

        /// <summary>
        /// Triggertype
        /// </summary>
        public ETriggerType TriggerType
        {
            get { return triggerType; }
            set { triggerType = value; }
        }

    	/// <summary>
        /// returns the NPC of the trigger
        /// </summary>
        public GameLiving NPC
        {
            get { return defaultNPC; }
            set { defaultNPC = value; }
        }

		public CoreEventHandler NotifyHandler
		{
			get { return notifyHandler; }
            set { notifyHandler = value; }
		}
		   
 	    /// <summary>
        /// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
 	    /// </summary>
 	    /// <param name="defaultNPC"></param>
 	    /// <param name="notifyHandler"></param>
 	    /// <param name="type"></param>
        public ATrigger(GameLiving defaultNPC, CoreEventHandler notifyHandler, ETriggerType type)
        {
            this.defaultNPC = defaultNPC;
            this.notifyHandler = notifyHandler;
            this.triggerType = type;
        }

		/// <summary>
		/// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
		/// </summary>
		/// <param name="defaultNPC"></param>
		/// <param name="notifyHandler"></param>
		/// <param name="type">Triggertype</param>
		/// <param name="k">keyword (K), meaning depends on triggertype</param>
		/// <param name="i">variable (I), meaning depends on triggertype</param>
		public ATrigger(GameLiving defaultNPC,CoreEventHandler notifyHandler, ETriggerType type, object k, object i) : this(defaultNPC,notifyHandler,type)
		{
            TriggerAttribute attr = BehaviorMgr.getTriggerAttribute(this.GetType());

            // handle parameter K
            object defaultValueK = GetDefaultValue(attr.DefaultValueK);
            this.k = (TypeK)BehaviorUtil.ConvertObject(k, defaultValueK, typeof(TypeK));
            CheckParameter(K, attr.IsNullableK, typeof(TypeK));

            // handle parameter I
            object defaultValueI = GetDefaultValue(attr.DefaultValueI);
            this.i = (TypeI)BehaviorUtil.ConvertObject(i, defaultValueI, typeof(TypeI));
            CheckParameter(I, attr.IsNullableI, typeof(TypeI));
		}

        protected virtual object GetDefaultValue(Object defaultValue)
        {
            if (defaultValue != null)
            {
                if (defaultValue is EDefaultValueConstants)
                {
                    switch ((EDefaultValueConstants)defaultValue)
                    {                        
                        case EDefaultValueConstants.NPC:
                            defaultValue = NPC;
                            break;
                    }
                }
            }
            return defaultValue;
        }

        protected virtual bool CheckParameter(object value, Boolean isNullable, Type destinationType)
        {
            if (destinationType == typeof(Unused))
            {
                if (value != null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn("Parameter is not used for =" + this.GetType().Name + ".\n The recieved parameter " + value + " will not be used for anthing. Check your quest code for inproper usage of parameters!");
                        return false;
                    }
                }
            }
            else
            {
                if (!isNullable && value == null)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("Not nullable parameter was null, expected type is " + destinationType.Name + "for =" + this.GetType().Name + ".\nRecived parameter was " + value);
                        return false;
                    }
                }
                if (value != null && !(destinationType.IsInstanceOfType(value)))
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("Parameter was not of expected type, expected type is " + destinationType.Name + "for " + this.GetType().Name + ".\nRecived parameter was " + value);
                        return false;
                    }
                }
            }

            return true;
        }


    	/// <summary>
        /// Checks the trigger, this method is called whenever a event associated with this questparts quest
        /// or a manualy associated eventhandler is notified.
        /// </summary>
        /// <param name="e">DolEvent of notify call</param>
        /// <param name="sender">Sender of notify call</param>
        /// <param name="args">EventArgs of notify call</param>        
        /// <returns>true if QuestPart should be executes, else false</returns>
        public abstract bool Check(CoreEvent e, object sender, EventArgs args);

		/// <summary>
		/// Registers the needed EventHandler for this Trigger
		/// </summary>
		/// <remarks>
		/// This method will be called multiple times, so use AddHandlerUnique to make
		/// sure only one handler is actually registered
		/// </remarks>
        public abstract void Register();

		/// <summary>
		/// Unregisters the needed EventHandler for this Trigger
		/// </summary>
		/// <remarks>
		/// Don't remove handlers that will be used by other triggers etc.
		/// This is rather difficult since we don't know which events other triggers use.
		/// </remarks>
        public abstract void Unregister();
    }
}