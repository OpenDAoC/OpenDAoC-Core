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
using System.Reflection;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.SkillHandler
{
	/// <summary>
	/// Handler for Sprint Ability clicks
	/// </summary>
	[SkillHandler(Abilities.DirtyTricks)]
	public class DirtyTricksAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		/// <summary>
		/// The ability reuse time in seconds
		/// </summary>
		protected const int REUSE_TIMER = 60000 * 7;

		/// <summary>
		/// The ability effect duration in seconds
		/// </summary>
		public const int DURATION = 30;

		/// <summary>
		/// Execute dirtytricks ability
		/// </summary>
		/// <param name="ab">The used ability</param>
		/// <param name="player">The player that used the ability</param>
		public void Execute(Ability ab, GamePlayer player)
		{
			if (player == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not retrieve player in DirtyTricksAbilityHandler.");
				return;
			}

			if (!player.IsAlive)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                return;
			}

			if (player.IsMezzed)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseMezzed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (player.IsStunned)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStunned"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			if (player.IsSitting)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStanding"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			player.DisableSkill(ab, REUSE_TIMER);
			new DirtyTricksEcsAbilityEffect(new EcsGameEffectInitParams(player, DURATION * 1000, 1));
		}
	}
}

namespace DOL.GS.Effects
{
	/// <summary>
	/// TripleWield
	/// </summary>
	public class DirtyTricksEffect : TimedEffect
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public DirtyTricksEffect(int duration)
			: base(duration)
		{
		}
		public override void Start(GameLiving target)
		{
			base.Start(target);
			GamePlayer player = target as GamePlayer;
			//   foreach(GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			//    {
			//	    p.Out.SendSpellEffectAnimation(player, player, Icon, 0, false, 1);
			//	    p.Out.SendSpellCastAnimation(player, Icon, 0);
			//   }
			GameEventMgr.AddHandler(player, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));
		}
		public override void Stop()
		{
			base.Stop();
			GamePlayer player = Owner as GamePlayer;
			GameEventMgr.RemoveHandler(player, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));
		}
		protected void EventHandler(CoreEvent e, object sender, EventArgs arguments)
		{
			AttackFinishedEventArgs atkArgs = arguments as AttackFinishedEventArgs;
			if (atkArgs == null) return;
			if (atkArgs.AttackData.AttackResult != EAttackResult.HitUnstyled
				&& atkArgs.AttackData.AttackResult != EAttackResult.HitStyle) return;
			if (atkArgs.AttackData.Target == null) return;
			GameLiving target = atkArgs.AttackData.Target;
			if (target == null) return;
			if (target.ObjectState != GameObject.eObjectState.Active) return;
			if (target.IsAlive == false) return;
			GameLiving attacker = sender as GameLiving;
			if (attacker == null) return;
			if (attacker.ObjectState != GameObject.eObjectState.Active) return;
			if (attacker.IsAlive == false) return;
			if (atkArgs.AttackData.IsOffHand) return; // only react to main hand
			if (atkArgs.AttackData.Weapon == null) return; // no weapon attack

			DTdetrimentalEffect dt = target.EffectList.GetOfType<DTdetrimentalEffect>();
			if (dt == null)
			{
				new DTdetrimentalEffect().Start(target);
				// Log.Debug("Effect Started from dirty tricks handler on " + target.Name);
			}
		}

        public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.DirtyTricks.Name");} }

		public override ushort Icon { get { return 478; } }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.DirtyTricks.Description"));
				list.AddRange(base.DelveInfo);
				return list;
			}
		}
	}
}

namespace DOL.GS.Effects
{
	/// <summary>
	/// The helper class for the berserk ability
	/// </summary>
	public class DTdetrimentalEffect : StaticEffect, IGameEffect
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The ability description
		/// </summary>
        protected const String delveString = "Causes target's fumble rate to increase.";

		/// <summary>
		/// The owner of the effect
		/// </summary>
		GameLiving m_player;

		/// <summary>
		/// The timer that will cancel the effect
		/// </summary>
		protected EcsGameTimer m_expireTimer;

		/// <summary>
		/// Creates a new berserk effect
		/// </summary>
		public DTdetrimentalEffect()
		{
		}

		/// <summary>
		/// Start the berserk on a player
		/// </summary>
		public override void Start(GameLiving living)
		{
			m_player = living;
			//    Log.Debug("Effect Started from DT detrimental effect on " + m_player.Name);
			StartTimers(); // start the timers before adding to the list!
			m_player.EffectList.Add(this);
			m_player.DebuffCategory[(int)EProperty.FumbleChance] += 50;
			//  foreach (GamePlayer visiblePlayer in m_player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			//  {
			//  }
			GamePlayer player = living as GamePlayer;
			if (player != null)
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.DirtyTricks.EffectStart"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Called when effect must be canceled
		/// </summary>
		public override void Cancel(bool playerCancel)
		{
			if (playerCancel) // not cancelable by teh player
				return;
			//  Log.Debug("Effect Canceled from DT Detrimental effect on "+ m_player.Name);
			StopTimers();
			m_player.EffectList.Remove(this);
			m_player.DebuffCategory[(int)EProperty.FumbleChance] -= 50;
			GamePlayer player = m_player as GamePlayer;
			if (player != null)
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.DirtyTricks.EffectCancel"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Starts the timers for this effect
		/// </summary>
		protected virtual void StartTimers()
		{
			StopTimers();
			m_expireTimer = new EcsGameTimer(m_player, new EcsGameTimer.EcsTimerCallback(ExpiredCallback), 10000);
		}

		/// <summary>
		/// Stops the timers for this effect
		/// </summary>
		protected virtual void StopTimers()
		{
			if (m_expireTimer != null)
			{
				//DOLConsole.WriteLine("effect stop expire on "+Owner.Name+" "+this.InternalID);
				m_expireTimer.Stop();
				m_expireTimer = null;
			}
		}

		/// <summary>
		/// The callback method when the effect expires
		/// </summary>
		/// <param name="callingTimer">the regiontimer of the effect</param>
		/// <returns>the new intervall (0) </returns>
		protected virtual int ExpiredCallback(EcsGameTimer callingTimer)
		{
			Cancel(false);
			return 0;
		}

		/// <summary>
		/// Name of the effect
		/// </summary>
        public override string Name 
		{ 
			get 
			{ 
				if (Owner != null && Owner is GamePlayer && (Owner as GamePlayer).Client != null)
				{
					return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.DirtyTricks.Name"); 
				}

				return LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "Skill.Ability.DirtyTricks.Name"); 
			} 
		}

		/// <summary>
		/// Remaining Time of the effect in milliseconds
		/// </summary>
		public override int RemainingTime
		{
			get
			{
				EcsGameTimer timer = m_expireTimer;
				if (timer == null || !timer.IsAlive)
					return 0;
				return timer.TimeUntilElapsed;
			}
		}

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon { get { return 478; } }

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var delveInfoList = new List<string>(4);
				delveInfoList.Add(delveString);

				int seconds = RemainingTime / 1000;
				if (seconds > 0)
				{
					delveInfoList.Add(" "); //empty line
					if (seconds > 60)
						delveInfoList.Add("- " + seconds / 60 + ":" + (seconds % 60).ToString("00") + LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.MinRemaining"));
					else
						delveInfoList.Add("- " + seconds + LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Skill.Ability.SecRemaining"));
				}

				return delveInfoList;
			}
		}
	}
}
