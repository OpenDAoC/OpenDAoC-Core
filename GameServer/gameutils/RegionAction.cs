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
using System.Text;

namespace DOL.GS
{
	public abstract class RegionECSAction : ECSGameTimer
	{
		/// <summary>
		/// The source of the action
		/// </summary>
		protected readonly GameObject m_actionSource;

		/// <summary>
		/// Constructs a new region action
		/// </summary>
		/// <param name="actionSource">The action source</param>
		public RegionECSAction(GameObject actionSource) : base(actionSource)
		{
			if (actionSource == null)
				throw new ArgumentNullException("actionSource");
			m_actionSource = actionSource;

			TimerOwner = actionSource;
			Callback = new ECSTimerCallback(OnTick);
		}

		protected abstract int OnTick(ECSGameTimer timer);


		/// <summary>
		/// Returns short information about the timer
		/// </summary>
		/// <returns>Short info about the timer</returns>
		public override string ToString()
		{
			return new StringBuilder(base.ToString(), 128)
				.Append(" actionSource: (").Append(m_actionSource.ToString())
				.Append(')')
				.ToString();
		}
	}

	public abstract class AuxRegionECSAction : AuxECSGameTimer
	{
		/// <summary>
		/// The source of the action
		/// </summary>
		protected readonly GameObject m_actionSource;

		/// <summary>
		/// Constructs a new region action
		/// </summary>
		/// <param name="actionSource">The action source</param>
		public AuxRegionECSAction(GameObject actionSource) : base(actionSource)
		{
			if (actionSource == null)
				throw new ArgumentNullException("actionSource");
			m_actionSource = actionSource;

			TimerOwner = actionSource;
			Callback = new AuxECSTimerCallback(OnTick);
		}

		protected abstract int OnTick(AuxECSGameTimer timer);


		/// <summary>
		/// Returns short information about the timer
		/// </summary>
		/// <returns>Short info about the timer</returns>
		public override string ToString()
		{
			return new StringBuilder(base.ToString(), 128)
				.Append(" actionSource: (").Append(m_actionSource.ToString())
				.Append(')')
				.ToString();
		}
	}
}
