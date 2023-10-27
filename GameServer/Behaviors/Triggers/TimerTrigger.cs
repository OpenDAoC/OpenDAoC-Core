using System;
using System.Reflection;
using Core.GS.Enums;
using Core.GS.Events;
using log4net;

namespace Core.GS.Behaviors;

/// <summary>
/// A trigger defines the circumstances under which a certain QuestAction is fired.
/// This can be eTriggerAction.Interact, eTriggerAction.GiveItem, eTriggerAction.Attack, etc...
/// Additional there are two variables to add the needed parameters for the triggertype (Item to give for GiveItem, NPC to interact for Interact, etc...). To fire a QuestAction at least one of the added triggers must be fulfilled. 
/// </summary>
[Trigger(TriggerType=ETriggerType.Timer)]
public class TimerTrigger : ATrigger<string,Unused>
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
    /// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
	/// </summary>
	/// <param name="defaultNPC"></param>
	/// <param name="notifyHandler"></param>
	/// <param name="k"></param>
	/// <param name="i"></param>
    public TimerTrigger(GameNpc defaultNPC, CoreEventHandler notifyHandler,  Object k, Object i)
        : base(defaultNPC, notifyHandler, ETriggerType.Timer, k, i)
    { }

    /// <summary>
    /// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
    /// </summary>
    /// <param name="defaultNPC"></param>
    /// <param name="notifyHandler"></param>
    /// <param name="k"></param>
    public TimerTrigger(GameNpc defaultNPC, CoreEventHandler notifyHandler, string k)
        : this(defaultNPC,notifyHandler,  (object)k, (object) null)
    { }

    /// <summary>
    /// Checks the trigger, this method is called whenever a event associated with this questparts quest
    /// or a manualy associated eventhandler is notified.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public override bool Check(CoreEvent e, object sender, EventArgs args)
    {
        bool result = false;
        
        if (e == GameLivingEvent.Timer)
        {
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
            TimerEventArgs tArgs = (TimerEventArgs)args;
            result = K == tArgs.TimerID && tArgs.Source == player;
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
    }		
}