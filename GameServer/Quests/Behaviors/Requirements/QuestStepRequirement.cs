using System;
using System.Reflection;
using Core.GS.Behaviors;
using Core.GS.Enums;
using Core.GS.Events;
using log4net;

namespace Core.GS.Quests;

/// <summary>
/// Requirements describe what must be true to allow a QuestAction to fire.
/// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
/// additional parameters. To fire a QuestAction ALL requirements must be fulfilled.         
/// </summary>
[Requirement(RequirementType=ERequirementType.QuestStep)]
public class QuestStepRequirement : ARequirement<Type,int>
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
    /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
	/// </summary>
	/// <param name="defaultNPC"></param>
	/// <param name="n"></param>
	/// <param name="v"></param>
	/// <param name="comp"></param>
    public QuestStepRequirement(GameNpc defaultNPC, Object n, Object v, EComparator comp)
        : base(defaultNPC, ERequirementType.QuestStep, n, v, comp)
	{   			
	}

    /// <summary>
    /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
    /// </summary>
    /// <param name="defaultNPC"></param>
    /// <param name="questType"></param>
    /// <param name="v"></param>
    /// <param name="comp"></param>
    public QuestStepRequirement(GameNpc defaultNPC, Type questType,int v, EComparator comp)
        : this(defaultNPC, (object)questType, (object)v, comp)
	{   			
	}

	/// <summary>
    /// Checks the added requirement whenever a trigger associated with this questpart fires.(returns true)
	/// </summary>
	/// <param name="e"></param>
	/// <param name="sender"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public override bool Check(CoreEvent e, object sender, EventArgs args)
	{
		bool result = true;
        GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
        
        AQuest playerQuest = player.IsDoingQuest(N);
        if (playerQuest != null)
        {
            result &= compare(playerQuest.Step, V, Comparator);
        }
        else
        {
            result = false;
        }

		return result;
	}
}