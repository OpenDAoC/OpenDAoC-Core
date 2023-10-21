using System;
using Core.Events;

namespace Core.GS.Behaviors;

/// <summary>
/// If one trigger and all requirements are fulfilled the corresponding actions of
/// a QuestAction will we executed one after another. Actions can be more or less anything:
/// at the moment there are: GiveItem, TakeItem, Talk, Give Quest, Increase Quest Step, FinishQuest,
/// etc....
/// </summary>
public interface IBehaviorAction
{
    /// <summary>
    /// Action performed 
    /// Can be used in subclasses to define special behaviour of actions
    /// </summary>
    /// <param name="e">DolEvent of notify call</param>
    /// <param name="sender">Sender of notify call</param>
    /// <param name="args">EventArgs of notify call</param>        
    void Perform(CoreEvent e, object sender, EventArgs args);
}