using System;
using System.Reflection;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using log4net;

namespace DOL.GS.Behaviour.Requirements
{
	/// <summary>
	/// Requirements describe what must be true to allow a QuestAction to fire.
	/// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
	/// additional parameters. To fire a QuestAction ALL requirements must be fulfilled.         
	/// </summary>
    [Requirement(RequirementType=ERequirementType.RealmPoints)]
	public class RealmPointsRequirement : ARequirement<long,Unused>
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
        /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
		/// </summary>
		/// <param name="defaultNPC"></param>
		/// <param name="n"></param>
		/// <param name="v"></param>
		/// <param name="comp"></param>
        public RealmPointsRequirement(GameNpc defaultNPC,  Object n, Object v, EComparator comp)
            : base(defaultNPC, ERequirementType.RealmPoints, n, v, comp)
		{   			
		}

        /// <summary>
		/// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
		/// </summary>
		/// <param name="defaultNPC">Parent defaultNPC of this Requirement</param>		
		/// <param name="n">First Requirement Variable, meaning depends on RequirementType</param>		
		/// <param name="comp">Comparator used if some values are veeing compared</param>
        public RealmPointsRequirement(GameNpc defaultNPC,  long n, EComparator comp)
            : this(defaultNPC,  (object)n, (object)null, comp)
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

            result = compare(player.RealmPoints, N, Comparator);

			return result;
		}

		
    }
}
