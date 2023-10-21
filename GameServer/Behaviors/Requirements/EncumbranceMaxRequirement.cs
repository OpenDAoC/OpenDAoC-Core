using System;
using System.Reflection;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Behaviors;

/// <summary>
/// Requirements describe what must be true to allow a QuestAction to fire.
/// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
/// additional parameters. To fire a QuestAction ALL requirements must be fulfilled.         
/// </summary>
[Requirement(RequirementType=ERequirementType.EncumbranceMax)]
public class EncumbranceMaxRequirement : ARequirement<int,Unused>
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
    /// </summary>
    /// <param name="defaultNPC">Parent defaultNPC of this Requirement</param>
    /// <param name="n">First Requirement Variable, meaning depends on RequirementType</param>
    /// <param name="v">Second Requirement Variable, meaning depends on RequirementType</param>
    /// <param name="comp">Comparator used if some values are veeing compared</param>
    public EncumbranceMaxRequirement(GameNpc defaultNPC,  Object n, Object v, EComparator comp)
        : base(defaultNPC, ERequirementType.EncumbranceMax, n, v, comp)
	{   			
	}

    /// <summary>
	/// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC">Parent defaultNPC of this Requirement</param>		
	/// <param name="n">First Requirement Variable, meaning depends on RequirementType</param>		
	/// <param name="comp">Comparator used if some values are veeing compared</param>
    public EncumbranceMaxRequirement(GameNpc defaultNPC, int n, EComparator comp)
        : this(defaultNPC,  (object)n, (object)null, comp)
	{   			
	}

    /// <summary>
    /// Checks the added requirement whenever a trigger associated with this defaultNPC fires.(returns true)
    /// </summary>
    /// <param name="e">DolEvent of notify call</param>
    /// <param name="sender">GamePlayer this call is related to, can be null</param>
    /// <param name="args">EventArgs of notify call</param>
    /// <returns>true if all Requirements fordefaultNPC where fullfilled, else false</returns>
	public override bool Check(CoreEvent e, object sender, EventArgs args)
	{
		bool result = true;
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

        result = compare(player.MaxEncumberance, N, Comparator);

		return result;
	}
}