using System;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using log4net;

namespace Core.GS.Behaviors;

/// <summary>
/// A trigger defines the circumstances under which a certain QuestAction is fired.
/// This can be eTriggerAction.Interact, eTriggerAction.GiveItem, eTriggerAction.Attack, etc...
/// Additional there are two variables to add the needed parameters for the triggertype (Item to give for GiveItem, NPC to interact for Interact, etc...). To fire a QuestAction at least one of the added triggers must be fulfilled. 
/// </summary>
[Trigger(TriggerType=ETriggerType.ItemUsed,DefaultValueI=EDefaultValueConstants.NPC)]
public class ItemUsedTrigger : ATrigger<Unused,DbItemTemplate>
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
    /// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
	/// </summary>
	/// <param name="defaultNPC"></param>
	/// <param name="notifyHandler"></param>
	/// <param name="k"></param>
	/// <param name="i"></param>
    public ItemUsedTrigger(GameNpc defaultNPC, CoreEventHandler notifyHandler,  Object k, Object i)
        : base(defaultNPC, notifyHandler, ETriggerType.ItemUsed, k, i)
    { }

    /// <summary>
    /// Creates a new questtrigger and does some simple triggertype parameter compatibility checking
    /// </summary>
    /// <param name="defaultNPC"></param>
    /// <param name="notifyHandler"></param>
    /// <param name="i"></param>
    public ItemUsedTrigger(GameNpc defaultNPC, CoreEventHandler notifyHandler, DbItemTemplate i)
        : this(defaultNPC,notifyHandler, (object)null,(object) i)
    { }

    /// <summary>
    /// Checks the trigger, this method is called whenever a event associated with this questparts quest
    /// or a manualy associated eventhandler is notified.
    /// </summary>
    /// <param name="e">DolEvent of notify call</param>
    /// <param name="sender">Sender of notify call</param>
    /// <param name="args">EventArgs of notify call</param>        
    /// <returns>true if QuestPart should be executes, else false</returns>
    public override bool Check(CoreEvent e, object sender, EventArgs args)
    {
        bool result = false;
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

        if (e == GamePlayerEvent.UseSlot)
        {
            UseSlotEventArgs uArgs = (UseSlotEventArgs)args;
            DbInventoryItem item = player.Inventory.GetItem((EInventorySlot)uArgs.Slot) as DbInventoryItem;
			if (item != null && I != null)
				result = I.Name == item.Name;
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