/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Reflection;
using Core.Events;
using Core.GS.Behaviors;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;
using log4net;

namespace Core.GS.Quests.Requirements
{

	/// <summary>
	/// Requirements describe what must be true to allow a QuestAction to fire.
	/// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
	/// additional parameters. To fire a QuestAction ALL requirements must be fulfilled.         
	/// </summary>
    [Requirement(RequirementType=ERequirementType.QuestGivable,DefaultValueV=EDefaultValueConstants.NPC)]
	public class QuestGivableRequirement : ARequirement<Type,GameNpc>
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
        /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
		/// </summary>
		/// <param name="defaultNPC"></param>
		/// <param name="n"></param>
		/// <param name="v"></param>
		/// <param name="comp"></param>
        public QuestGivableRequirement(GameNpc defaultNPC, Object n, Object v, EComparator comp)
            : base(defaultNPC, ERequirementType.QuestGivable, n, v, comp)
		{
		}

        /// <summary>
        /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
        /// </summary>
        /// <param name="defaultNPC"></param>
        /// <param name="questType"></param>
        /// <param name="v"></param>
        /// <param name="comp"></param>
        public QuestGivableRequirement(GameNpc defaultNPC, Type questType, GameNpc v, EComparator comp)
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

            if (Comparator == EComparator.Not)
                result = QuestMgr.CanGiveQuest(N, player, V) <= 0;
            else
                result = QuestMgr.CanGiveQuest(N, player, V) > 0;

			return result;
		}

		
    }
}
