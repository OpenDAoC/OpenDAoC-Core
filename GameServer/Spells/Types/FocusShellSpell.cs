using System;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("FocusShell")]
public class FocusShellSpell : SpellHandler
{
	public override void CreateECSEffect(EcsGameEffectInitParams initParams)
	{
		new FocusEcsSpellEffect(initParams);
	}
	
	private GamePlayer FSTarget = null;
	private FSTimer timer = null;

	public FocusShellSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

	public override bool CheckBeginCast(GameLiving selectedTarget)
	{
		//Only works on members of the same realm
		if (GameServer.ServerRules.IsAllowedToAttack(selectedTarget, Caster, true) == false)
		{
			//This spell doesn't work on pets or monsters
			if (selectedTarget is GameNpc)
			{
				MessageToCaster("This spell may not be cast on pets!", EChatType.CT_SpellResisted);
				return false;
			}

			if (selectedTarget is GamePlayer)
			{
				Caster.ActivePulseSpells.TryGetValue(m_spell.SpellType, out Spell currentSpell);

				if (currentSpell != null && currentSpell == Spell)
				{
					Caster.CancelFocusSpell();
				}

				FSTarget = selectedTarget as GamePlayer;
			}
			else
			{
				return false;
			}
		}
		else
		{
			MessageToCaster("This spell only works on members of your realm!", EChatType.CT_SpellResisted);
			return false;
		}

		return base.CheckBeginCast(selectedTarget);
	}

	public override void  OnEffectStart(GameSpellEffect effect)
	{
		//Add the handler for when the target is attacked and we should reduce the damage
		GameEventMgr.AddHandler(FSTarget, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));

		//Add handlers for the target attacking, casting, etc
		//Don't need to add the handlers twice
		if (FSTarget != Caster)
		{
			GameEventMgr.AddHandler(FSTarget, GameLivingEvent.AttackFinished, new CoreEventHandler(CancelSpell));
			GameEventMgr.AddHandler(FSTarget, GameLivingEvent.CastStarting, new CoreEventHandler(CancelSpell));
		}
		

		timer = new FSTimer(Caster, this);
		timer.Start(1000);

 		base.OnEffectStart(effect);
	}

	public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
	{
		//Remove our handler
		GameEventMgr.RemoveHandler(FSTarget, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));

		if (FSTarget != Caster)
		{
			GameEventMgr.RemoveHandler(FSTarget, GameLivingEvent.AttackFinished, new CoreEventHandler(CancelSpell));
			GameEventMgr.RemoveHandler(FSTarget, GameLivingEvent.CastStarting, new CoreEventHandler(CancelSpell));
		}

		timer.Stop();
		
		return base.OnEffectExpires(effect, noMessages);
	}

	private void CancelSpell(CoreEvent e, object sender, EventArgs args)
	{
		//Send the cancel signal, we need to use the faster as the sender!
		FocusSpellAction(/*null, Caster, null*/);
	}
		
	private void OnAttacked(CoreEvent e, object sender, EventArgs args)
	{
		AttackedByEnemyEventArgs attackArgs = args as AttackedByEnemyEventArgs;
		if (attackArgs == null || attackArgs.AttackData == null)
			return;

		//If the attacker was not a mob, then lets do the reduction!
		if (attackArgs.AttackData.Attacker.Realm != ERealm.None)
		{
			//Damage absorbed from normal attack
			int damageAbsorbed = (int)(attackArgs.AttackData.Damage * (Spell.Value * .01));
			attackArgs.AttackData.Damage -= damageAbsorbed;
			//Damage absorbed from critical hit
			damageAbsorbed = (int)(attackArgs.AttackData.CriticalDamage * (Spell.Value * .01));
			attackArgs.AttackData.CriticalDamage -= damageAbsorbed;
		}
	}

	public override void FinishSpellCast(GameLiving target)
	{
		m_caster.Mana -= PowerCost(target);
		base.FinishSpellCast(target);
	}

	protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
	{
		return new FocusShellEffect(this, Spell.Duration, Spell.Frequency, effectiveness);
	}

	/// <summary>
	/// Since the focus spell isn't a pulsing spell we need our own mini-timer
	/// </summary>
	private class FSTimer : EcsGameTimerWrapperBase
	{
		//The handler for this timer
		FocusShellSpell m_handler;

		public FSTimer(GameObject actionSource, FocusShellSpell handler) : base(actionSource)
		{
			m_handler = handler;
			Interval = handler.Spell.Frequency;
		}

		protected override int OnTick(EcsGameTimer ecsGameTimer)
		{
			//If the target is still in range
			if (m_handler.Caster.Mana >= m_handler.Spell.PulsePower && m_handler.Caster.IsWithinRadius(m_handler.FSTarget, m_handler.Spell.Range))
			{
				m_handler.Caster.Mana -= m_handler.Spell.PulsePower;
				m_handler.Caster.LastAttackTickPvP = m_handler.Caster.CurrentRegion.Time;
			}
			//Target went out of range, stop the spell
			else
			{
				//Cancel spell
				m_handler.CancelSpell(null, m_handler.Caster, null);
				Stop();
				return 0;
			}

			return Interval;
		}
	}
}