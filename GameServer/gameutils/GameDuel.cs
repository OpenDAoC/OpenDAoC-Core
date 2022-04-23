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
using System.Linq;
using DOL.Events;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.Language;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using Newtonsoft.Json.Serialization;

namespace DOL.GS
{
	/// <summary>
	/// GameDuel is an Helper Class for Player Duels
	/// </summary>
	public class GameDuel
	{
		/// <summary>
		/// Duel Initiator
		/// </summary>
		public GamePlayer Starter { get; protected set; }
		
		/// <summary>
		/// Duel Target
		/// </summary>
		public GamePlayer Target { get; protected set; }
		
		/// <summary>
		/// Is Duel Started ?
		/// </summary>
		public bool Started { get { return m_started; } protected set { m_started = value; } }
		protected volatile bool m_started;
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="starter"></param>
		/// <param name="target"></param>
		public GameDuel(GamePlayer starter, GamePlayer target)
		{
			Starter = starter;
			Target = target;
			Started = false;
		}

		/// <summary>
		/// Start Duel if is not running.
		/// </summary>
		public virtual void Start()
		{
			if (Started)
				return;
			
			Started = true;
			
			Target.DuelStart(Starter);
			GameEventMgr.AddHandler(Starter, GamePlayerEvent.Quit, new DOLEventHandler(DuelOnPlayerQuit));
			GameEventMgr.AddHandler(Starter, GamePlayerEvent.Linkdeath, new DOLEventHandler(DuelOnPlayerQuit));
			GameEventMgr.AddHandler(Starter, GamePlayerEvent.RegionChanged, new DOLEventHandler(DuelOnPlayerQuit));
			GameEventMgr.AddHandler(Starter, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(DuelOnAttack));
			GameEventMgr.AddHandler(Starter, GameLivingEvent.AttackFinished, new DOLEventHandler(DuelOnAttack));
		}
		
		/// <summary>
		/// Stops the duel if it is running
		/// </summary>
		public virtual void Stop()
		{
			if (!Started)
				return;

			StopNegativeEffects(Starter, Target);
			StopNegativeEffects(Target, Starter);
			if (Caster is GamePet casterPet && (casterPet.Owner == Target || casterPet.Owner == Starter))
			{
				StopNegativeEffects(Target, casterPet);
				StopNegativeEffects(Starter, casterPet);
			}
			StopImmunityEffects(Target);
			StopImmunityEffects(Starter);

			Started = false;
			Target.DuelStop();
			Target = null;

			GameEventMgr.RemoveHandler(Starter, GamePlayerEvent.Quit, new DOLEventHandler(DuelOnPlayerQuit));
			GameEventMgr.RemoveHandler(Starter, GamePlayerEvent.Linkdeath, new DOLEventHandler(DuelOnPlayerQuit));
			GameEventMgr.RemoveHandler(Starter, GamePlayerEvent.RegionChanged, new DOLEventHandler(DuelOnPlayerQuit));
			GameEventMgr.RemoveHandler(Starter, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(DuelOnAttack));
			GameEventMgr.RemoveHandler(Starter, GameLivingEvent.AttackFinished, new DOLEventHandler(DuelOnAttack));
			
			lock (Starter.XPGainers.SyncRoot)
			{
				Starter.XPGainers.Clear();
			}
			
			Starter.Out.SendMessage(LanguageMgr.GetTranslation(Starter.Client, "GamePlayer.DuelStop.DuelEnds"), eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Stops the duel if player attack or is attacked by anything other that duel target
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected virtual void DuelOnAttack(DOLEvent e, object sender, EventArgs arguments)
		{
			AttackData ad = null;
			GameLiving target = null;
			var afea = arguments as AttackFinishedEventArgs;
			var abeea = arguments as AttackedByEnemyEventArgs;
			
			if (afea != null)
			{
				ad = afea.AttackData;
				target = ad.Target;
			}
			else if (abeea != null)
			{
				ad = abeea.AttackData;
				target = ad.Attacker;
			}

			if (ad == null)
				return;

			// check pets owner for my and enemy attacks
			GameNPC npc = target as GameNPC;
			if (npc != null)
			{
				IControlledBrain brain = npc.Brain as IControlledBrain;
				if (brain != null)
					target = brain.GetPlayerOwner();
			}
			
			// Duel should end if players join group and trys to attack
			if (ad.Attacker.Group != null && ad.Attacker.Group.IsInTheGroup(ad.Target))
				Stop();
			
			if (ad.IsHit && (target != Target && target != Starter ))
				Stop();
		}

		/// <summary>
		/// Stops the duel on quit/link death
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected virtual void DuelOnPlayerQuit(DOLEvent e, object sender, EventArgs arguments)
		{
			Stop();
		}
		
		public GameLiving Caster;
		
		/// <summary>
		/// Stops a negative effect 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="Caster"></param>
		protected virtual void StopNegativeEffects(GamePlayer target, GameLiving Caster)
		{
			if (target == null)
				return;
			
			var effects = target.effectListComponent.GetAllEffects();
			var spelle = target.effectListComponent.GetSpellEffects();
			foreach (var spellEffect in spelle.Where(spellEffect => spellEffect != null && !spellEffect.HasPositiveEffect && spellEffect.Caster == Caster && spellEffect.Owner == target))
			{
				EffectService.RequestImmediateCancelEffect(EffectListService.GetSpellEffectOnTarget(target, spellEffect.EffectType));
			}
			foreach (var effect in effects.Where(effect => effect != null && !effect.HasPositiveEffect && effect.Owner == target))
			{
				EffectService.RequestImmediateCancelEffect(EffectListService.GetEffectOnTarget(target, effect.EffectType));
			}
		}
		/// <summary>
		/// Stops any immunity timers 
		/// </summary>
		/// <param name="target"></param>
		protected virtual void StopImmunityEffects(GamePlayer target)
		{
			if (target == null)
				return;
			
			var effects = target.effectListComponent.GetAllEffects();
			var spelle = target.effectListComponent.GetSpellEffects();
			foreach (var spellEffect in spelle.Where(spellEffect => spellEffect != null && spellEffect is ECSImmunityEffect && spellEffect.Caster != target))
			{
				EffectService.RequestImmediateCancelEffect(EffectListService.GetImmunityEffectOnTarget(target, spellEffect.EffectType));
			}
			foreach (var effect in effects.Where(effect => effect != null && effect is ECSImmunityEffect && effect.Owner != Caster))
			{
				EffectService.RequestImmediateCancelEffect(EffectListService.GetImmunityEffectOnTarget(target, effect.EffectType));
			}
		}

	}
}
