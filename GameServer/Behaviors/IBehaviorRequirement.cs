using System;
using Core.Events;
using Core.GS.Events;

namespace Core.GS.Behaviors;

/// <summary>
/// Requirements describe what must be true to allow a QuestAction to fire.
/// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
/// additional parameters. To fire a QuestAction ALL requirements must be fulfilled.         
/// </summary>
public interface IBehaviorRequirement
{
    /// <summary>
    /// Checks the requirement whenever a trigger associated with this questpart fires.(returns true)
    /// </summary>
    /// <param name="e"></param>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    bool Check(CoreEvent e, object sender, EventArgs args);
}