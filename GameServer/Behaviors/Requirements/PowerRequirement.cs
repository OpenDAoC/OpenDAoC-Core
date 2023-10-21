using System;
using System.Reflection;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;
using log4net;

namespace Core.GS.Behaviors;

/// <summary>
/// Requirements describe what must be true to allow a QuestAction to fire.
/// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
/// additional parameters. To fire a QuestAction ALL requirements must be fulfilled.         
/// </summary>
[Requirement(RequirementType=ERequirementType.Mana)]
public class PowerRequirement : ARequirement<int,Unused>
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
    /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC"></param>
	/// <param name="n"></param>
	/// <param name="v"></param>
	/// <param name="comp"></param>
    public PowerRequirement(GameNpc defaultNPC,  Object n, Object v, EComparator comp)
        : base(defaultNPC, ERequirementType.Mana, n, v, comp)
	{   			
	}

    /// <summary>
	/// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC">Parent defaultNPC of this Requirement</param>		
	/// <param name="n">First Requirement Variable, meaning depends on RequirementType</param>		
	/// <param name="comp">Comparator used if some values are veeing compared</param>
    public PowerRequirement(GameNpc defaultNPC, int n, EComparator comp)
        : this(defaultNPC, (object)n, (object)null, comp)
	{   			
	}

	/// <summary>
    /// Checks the added requirement whenever a trigger associated with this defaultNPC fires.(returns true)
	/// </summary>
	/// <param name="e"></param>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public override bool Check(CoreEvent e, object sender, EventArgs args)
	{
		bool result = true;
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

        result = compare(player.Mana, N, Comparator);

		return result;
	}
}