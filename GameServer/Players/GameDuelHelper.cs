using System;
using System.Linq;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS
{
	public class GameDuelHelper
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
		public GameDuelHelper(GamePlayer starter, GamePlayer target)
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
			GameEventMgr.AddHandler(Starter, GamePlayerEvent.Quit, new CoreEventHandler(DuelOnPlayerQuit));
			GameEventMgr.AddHandler(Starter, GamePlayerEvent.Linkdeath, new CoreEventHandler(DuelOnPlayerQuit));
			GameEventMgr.AddHandler(Starter, GamePlayerEvent.RegionChanged, new CoreEventHandler(DuelOnPlayerQuit));
			GameEventMgr.AddHandler(Starter, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(DuelOnAttack));
			GameEventMgr.AddHandler(Starter, GameLivingEvent.AttackFinished, new CoreEventHandler(DuelOnAttack));
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
			if (Caster is GameSummonedPet casterPet && (casterPet.Owner == Target || casterPet.Owner == Starter))
			{
				StopNegativeEffects(Target, casterPet);
				StopNegativeEffects(Starter, casterPet);
			}
			StopImmunityEffects(Target);
			StopImmunityEffects(Starter);

			Started = false;
			Target?.DuelStop();
			Target = null;

			GameEventMgr.RemoveHandler(Starter, GamePlayerEvent.Quit, new CoreEventHandler(DuelOnPlayerQuit));
			GameEventMgr.RemoveHandler(Starter, GamePlayerEvent.Linkdeath, new CoreEventHandler(DuelOnPlayerQuit));
			GameEventMgr.RemoveHandler(Starter, GamePlayerEvent.RegionChanged, new CoreEventHandler(DuelOnPlayerQuit));
			GameEventMgr.RemoveHandler(Starter, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(DuelOnAttack));
			GameEventMgr.RemoveHandler(Starter, GameLivingEvent.AttackFinished, new CoreEventHandler(DuelOnAttack));
			
			lock (Starter.XPGainers.SyncRoot)
			{
				Starter.XPGainers.Clear();
			}
			
			Starter.Out.SendMessage(LanguageMgr.GetTranslation(Starter.Client, "GamePlayer.DuelStop.DuelEnds"), EChatType.CT_Emote, EChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Stops the duel if player attack or is attacked by anything other that duel target
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected virtual void DuelOnAttack(CoreEvent e, object sender, EventArgs arguments)
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
			GameNpc npc = target as GameNpc;
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
		protected virtual void DuelOnPlayerQuit(CoreEvent e, object sender, EventArgs arguments)
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
			foreach (var spellEffect in spelle.Where(spellEffect => spellEffect != null && spellEffect is EcsImmunityEffect && spellEffect.Caster != target))
			{
				EffectService.RequestImmediateCancelEffect(EffectListService.GetImmunityEffectOnTarget(target, spellEffect.EffectType));
			}
			foreach (var effect in effects.Where(effect => effect != null && effect is EcsImmunityEffect && effect.Owner != Caster))
			{
				EffectService.RequestImmediateCancelEffect(EffectListService.GetImmunityEffectOnTarget(target, effect.EffectType));
			}
		}

	}
}