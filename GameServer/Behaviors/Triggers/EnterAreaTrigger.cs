using System;
using System.Reflection;
using Core.Events;
using Core.GS.Behaviour.Attributes;
using log4net;

namespace Core.GS.Behaviour.Triggers
{	
    /// <summary>
    /// A trigger defines the circumstances under which a certain QuestAction is fired.
    /// This can be eTriggerAction.Interact, eTriggerAction.GiveItem, eTriggerAction.Attack, etc...
    /// Additional there are two variables to add the needed parameters for the triggertype (Item to give for GiveItem, NPC to interact for Interact, etc...). To fire a QuestAction at least one of the added triggers must be fulfilled. 
    /// </summary>
    [Trigger(Global = true,TriggerType = ETriggerType.EnterArea)]
    public class EnterAreaTrigger : ATrigger<Unused, IArea>
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
        /// </summary>
        /// <param name="defaultNPC"></param>
        /// <param name="notifyHandler"></param>
        /// <param name="k">keyword (K), meaning depends on triggertype</param>
        /// <param name="i">variable (I), meaning depends on triggertype</param>
        public EnterAreaTrigger(GameNpc defaultNPC, CoreEventHandler notifyHandler,  Object k, Object i)
            : base(defaultNPC, notifyHandler, ETriggerType.EnterArea, k, i)
        { }

        /// <summary>
        /// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
        /// </summary>
        /// <param name="defaultNPC"></param>
        /// <param name="notifyHandler"></param>
        /// <param name="i">variable (I), meaning depends on triggertype</param>
        public EnterAreaTrigger(GameNpc defaultNPC, CoreEventHandler notifyHandler, IArea i)
            : this(defaultNPC,notifyHandler,  (object)null,(object) i)
        { }

        /// <summary>
        /// Checks the trigger, this method is called whenever a event associated with this questparts quest
        /// or a manualy associated eventhandler is notified.
        /// </summary>
        /// <param name="e">DolEvent of notify call</param>
        /// <param name="sender"></param>
        /// <param name="args">EventArgs of notify call</param>
        /// <returns>true if QuestPart should be executes, else false</returns>
        public override bool Check(CoreEvent e, object sender, EventArgs args)
        {
            bool result = false;

            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

            if (e == AreaEvent.PlayerEnter)
            {                
                AreaEventArgs aArgs = (AreaEventArgs)args;
                result = aArgs.GameObject == player && I == aArgs.Area;
            }
            
            return result;
        }

		/// <summary>
		/// Registers the needed EventHandler for this Trigger
		/// </summary>
		/// <remarks>
		/// This method will be called multiple times, so use AddHandlerUnique to make
		/// sure only one handler is actually registered
		/// </remarks>
        public override void Register()
        {            
            GameEventMgr.AddHandler(I, AreaEvent.PlayerEnter, NotifyHandler);                                
        }

		/// <summary>
		/// Unregisters the needed EventHandler for this Trigger
		/// </summary>
		/// <remarks>
		/// Don't remove handlers that will be used by other triggers etc.
		/// This is rather difficult since we don't know which events other triggers use.
		/// </remarks>
        public override void Unregister()
        {

            GameEventMgr.RemoveHandler(I, AreaEvent.PlayerEnter, NotifyHandler);
            
        }		
    }
}