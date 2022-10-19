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
using DOL.Events;
using DOL.GS;
using FiniteStateMachine;

namespace DOL.AI
{
	/// <summary>
	/// This class is the base of all arteficial intelligence in game objects
	/// </summary>
	public abstract class ABrain
	{
		private readonly object m_LockObject = new object(); // dummy object for locking - Mannen. // use this object for locking, instead of locking on 'this'

		public FSM FSM { get; set; }
		public virtual GameNPC Body { get; set; }
		public virtual bool IsActive => Body != null && Body.IsAlive && Body.ObjectState == GameObject.eObjectState.Active && Body.IsVisibleToPlayers;
		public virtual int ThinkInterval { get; set; } = 2500;
		public virtual int CastInterval { get; set; } = 2500;
		public virtual long LastThinkTick { get; set; }

		/// <summary>
		/// Returns the string representation of the ABrain
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return new StringBuilder(32)
				.Append("body name='").Append(Body==null?"(null)":Body.Name)
				.Append("' (id=").Append(Body==null?"(null)":Body.ObjectID.ToString())
				.Append("), active=").Append(IsActive)
				.Append(", ThinkInterval=").Append(ThinkInterval)
				.ToString();
		}

		/// <summary>
		/// Starts the brain thinking
		/// </summary>
		/// <returns>true if started</returns>
		public virtual bool Start()
		{
			//Do not start brain if we are dead or inactive
			if (!Body.IsAlive || Body.ObjectState != GameObject.eObjectState.Active)
				return false;
			
			lock (m_LockObject)
			{
				if (IsActive)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Stops the brain thinking
		/// </summary>
		/// <returns>true if stopped</returns>
		public virtual bool Stop()
		{
			lock (m_LockObject)
			{
				if (!IsActive)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Receives all messages of the body
		/// </summary>
		/// <param name="e">The event received</param>
		/// <param name="sender">The event sender</param>
		/// <param name="args">The event arguments</param>
		public virtual void Notify(DOLEvent e, object sender, EventArgs args) { }

		/// <summary>
		/// This method is called whenever the brain does some thinking
		/// </summary>
		public abstract void Think();

		public abstract void KillFSM();       
    }
}
