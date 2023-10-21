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
[Requirement(RequirementType=ERequirementType.Class)]
public class ClassRequirement : ARequirement<int,bool>
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
    /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC"></param>
	/// <param name="n"></param>
	/// <param name="v"></param>
	/// <param name="comp"></param>
    public ClassRequirement(GameNpc defaultNPC,  Object n, Object v, EComparator comp)
        : base(defaultNPC, ERequirementType.Class, n, v, comp)
	{   			
	}

    /// <summary>
	/// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC">Parent defaultNPC of this Requirement</param>		
	/// <param name="n">First Requirement Variable, meaning depends on RequirementType</param>			
    public ClassRequirement(GameNpc defaultNPC,  int n)
        : this(defaultNPC, (object)n, true, EComparator.None)
	{   			
	}

	/// <summary>
	/// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC">Parent defaultNPC of this Requirement</param>		
	/// <param name="n">First Requirement Variable, meaning depends on RequirementType</param>			
	public ClassRequirement(GameNpc defaultNPC, EPlayerClass c)
		: this(defaultNPC, (int)c, true, EComparator.None)
	{
	}

	/// <summary>
	/// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC">Parent defaultNPC of this Requirement</param>		
	/// <param name="n">First Requirement Variable, meaning depends on RequirementType</param>			
	public ClassRequirement(GameNpc defaultNPC, EPlayerClass c, bool notThisClass)
		: this(defaultNPC, (int)c, notThisClass, EComparator.None)
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

		if (V)
			result = (player.PlayerClass.ID != N);
		else result = (player.PlayerClass.ID == N);

		return result;
	}
}