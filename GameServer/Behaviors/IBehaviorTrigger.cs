using System;
using Core.Events;
using Core.GS.Events;

namespace Core.GS.Behaviors;

/// <summary>
/// A trigger defines the circumstances under which a certain QuestAction is fired.
/// This can be eTriggerAction.Interact, eTriggerAction.GiveItem, eTriggerAction.Attack, etc...
/// Additional there are two variables to add the needed parameters for the triggertype (Item to give for GiveItem, NPC to interact for Interact, etc...). To fire a QuestAction at least one of the added triggers must be fulfilled. 
/// </summary>        
public interface IBehaviorTrigger
{
    /// <summary>
    /// Checks the trigger, this method is called whenever a event associated with this questparts quest
    /// or a manualy associated eventhandler is notified.
    /// </summary>
    /// <param name="e">DolEvent of notify call</param>
    /// <param name="sender">Sender of notify call</param>
    /// <param name="args">EventArgs of notify call</param>        
    /// <returns>true if QuestPart should be executes, else false</returns>
    bool Check(CoreEvent e, object sender, EventArgs args);

    /// <summary>
    /// Registers the trigger within dol to be notified if possible trigger event occurs        
    /// </summary>
    /// <remarks>
    /// This method will be called multiple times, so use AddHandlerUnique to make
    /// sure only one handler is actually registered
    /// </remarks>        
    void Register();
    
    /// <summary>
    /// Unregister all added triggers that are no longer needed.
    /// </summary>
    /// <remarks>
    /// Don't remove handlers that will be used by other triggers etc.
    /// This is rather difficult since we don't know which events other triggers use.
    /// </remarks>        
    void Unregister();
}