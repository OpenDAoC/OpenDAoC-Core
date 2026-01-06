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
using System.Collections.Generic;
using DOL.Events;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[CharacterClass((int)eCharacterClass.Bainshee, "Bainshee", "Magician")]
	public class ClassBainshee : ClassMagician
	{
		public ClassBainshee() : base()
		{
			m_profession = "PlayerClass.Profession.PathofAffinity";
			m_specializationMultiplier = 10;
			m_primaryStat = eStat.INT;
			m_secondaryStat = eStat.DEX;
			m_tertiaryStat = eStat.CON;
			m_manaStat = eStat.INT;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}
		
		#region Wraith Form
		protected const int WRAITH_FORM_RESET_DELAY = 30000;
		
		/// <summary>
		/// Timer Action for Reseting Wraith Form
		/// </summary>
		protected ECSGameTimer m_wraithTimerAction;
		
		/// <summary>
		/// Event Trigger When Player Zoning Out to Force Reset Form
		/// </summary>
		protected DOLEventHandler m_wraithTriggerEvent;
		
		/// <summary>
		/// Bainshee Transform While Casting.
		/// </summary>
		/// <param name="player"></param>
		public override void Init(GamePlayer player)
		{
			base.Init(player);

			m_wraithTimerAction = new ECSGameTimer(player, new ECSGameTimer.ECSTimerCallback(_ =>
			{
				if (player.CharacterClass is ClassBainshee bainshee)
					bainshee.TurnOutOfWraith();

				return 0;
			}));

			m_wraithTriggerEvent = new DOLEventHandler(TriggerUnWraithForm);
			GameEventMgr.AddHandler(Player, GameLivingEvent.CastFinished, new DOLEventHandler(TriggerWraithForm));
		}

		/// <summary>
		/// Check if this Spell Cast Trigger Wraith Form
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected virtual void TriggerWraithForm(DOLEvent e, object sender, EventArgs arguments)
		{
			var player = sender as GamePlayer;
			
			if (player != Player)
				return;
			
			var args = arguments as CastingEventArgs;
			
			if (args == null || args.SpellHandler == null)
				return;

			if (!args.SpellHandler.HasPositiveEffect)
				TurnInWraith();
		}
		
		/// <summary>
		/// Check if we should remove Wraith Form
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected virtual void TriggerUnWraithForm(DOLEvent e, object sender, EventArgs arguments)
		{
			GamePlayer player = sender as GamePlayer;
			
			if (player != Player)
				return;
			
			TurnOutOfWraith(true);
		}
		
		/// <summary>
		/// Turn in Wraith Change Model and Start Timer for Reverting.
		/// If Already in Wraith Form Restart Timer Only.
		/// </summary>
		public virtual void TurnInWraith()
		{
			if (Player == null)
				return;
			
			if (!m_wraithTimerAction.IsAlive)
			{
				Player.Model = Player.Race switch
				{
					11 => 1885,//Elf
					12 => 1884,//Lurikeen
					_ => 1883,//Celt
				};

				GameEventMgr.AddHandler(Player, GameObjectEvent.RemoveFromWorld, m_wraithTriggerEvent);
			}
			
			m_wraithTimerAction.Start(WRAITH_FORM_RESET_DELAY);
		}

		/// <summary>
		/// Turn out of Wraith.
		/// Stop Timer and Remove Event Handlers.
		/// </summary>
		public void TurnOutOfWraith()
		{
			TurnOutOfWraith(false);
		}
		
		/// <summary>
		/// Turn out of Wraith.
		/// Stop Timer and Remove Event Handlers.
		/// </summary>
		public virtual void TurnOutOfWraith(bool forced)
		{
			if (Player == null)
				return;

			// Keep Wraith Form if Pulsing Offensive Spell Running
			//if (!forced && Player.ConcentrationEffects.OfType<PulsingSpellEffect>().Any(pfx => pfx.SpellHandler != null && !pfx.SpellHandler.HasPositiveEffect))
			//{
			//	TurnInWraith();
			//	return;
			//}
			
			m_wraithTimerAction.Stop();
			GameEventMgr.RemoveHandler(Player, GameObjectEvent.RemoveFromWorld, m_wraithTriggerEvent);
			Player.Model = (ushort)Player.Client.Account.Characters[Player.Client.ActiveCharIndex].CreationModel;
		}

		public override List<PlayerRace> EligibleRaces => new()
		{
			PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen,
		};
	}
	#endregion
}
