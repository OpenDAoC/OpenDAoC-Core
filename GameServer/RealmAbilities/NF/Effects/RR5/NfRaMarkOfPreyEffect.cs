using System;
using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.RealmAbilities;

public class NfRaMarkOfPreyEffect : TimedEffect
{
	private GamePlayer EffectOwner;
	private GamePlayer EffectCaster;
	private GroupUtil m_playerGroup;

	public NfRaMarkOfPreyEffect()
		: base(RealmAbilities.NfRaMarkOfPreyAbility.DURATION)
	{ }

	/// <summary>
	/// Start guarding the player
	/// </summary>
	/// <param name="Caster"></param>
	/// <param name="CasterTarget"></param>
	public void Start(GamePlayer Caster, GamePlayer CasterTarget)
	{
		if (Caster == null || CasterTarget == null)
			return;

		m_playerGroup = Caster.Group;
		if (m_playerGroup != CasterTarget.Group)
			return;

		EffectCaster = Caster;
		EffectOwner = CasterTarget;
		foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			p.Out.SendSpellEffectAnimation(EffectCaster, EffectOwner, 7090, 0, false, 1);
		}
		GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
		if (m_playerGroup != null)
			GameEventMgr.AddHandler(m_playerGroup, GroupEvent.MemberDisbanded, new CoreEventHandler(GroupDisbandCallback));
		GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.AttackFinished, new CoreEventHandler(AttackFinished));
		EffectOwner.Out.SendMessage("Your weapon begins channeling the strength of the vampiir!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
		base.Start(CasterTarget);
	}

	public override void Stop()
	{
		if (EffectOwner != null)
		{
			if (m_playerGroup != null)
				GameEventMgr.RemoveHandler(m_playerGroup, GroupEvent.MemberDisbanded, new CoreEventHandler(GroupDisbandCallback));
			GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.AttackFinished, new CoreEventHandler(AttackFinished));
			GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
			m_playerGroup = null;
		}
		EffectOwner.Out.SendMessage("Your weapon returns to normal.", EChatType.CT_SpellExpires, EChatLoc.CL_SystemWindow);
		base.Stop();
	}

	/// <summary>
	/// Called when a player is inflicted in an combat action
	/// </summary>
	/// <param name="e">The event which was raised</param>
	/// <param name="sender">Sender of the event</param>
	/// <param name="args">EventArgs associated with the event</param>
	private void AttackFinished(CoreEvent e, object sender, EventArgs args)
	{
		AttackFinishedEventArgs atkArgs = args as AttackFinishedEventArgs;
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
		double dpsCap;
		dpsCap = (1.2 + 0.3 * attacker.Level) * 0.7;

		double dps = Math.Min(RealmAbilities.NfRaMarkOfPreyAbility.VALUE, dpsCap);
		double damage = dps * atkArgs.AttackData.WeaponSpeed * 0.1;
		double damageResisted = damage * target.GetResist(EDamageType.Heat) * -0.01;

		AttackData ad = new AttackData();
		ad.Attacker = attacker;
		ad.Target = target;
		ad.Damage = (int)(damage + damageResisted);
		ad.Modifier = (int)damageResisted;
		ad.DamageType = EDamageType.Heat;
		ad.AttackType = EAttackType.Spell;
		ad.AttackResult = EAttackResult.HitUnstyled;
		target.OnAttackedByEnemy(ad);
		EffectCaster.ChangeMana(EffectOwner, EPowerChangeType.Spell, (int)ad.Damage);
		if (attacker is GamePlayer)
			(attacker as GamePlayer).Out.SendMessage(string.Format("You hit {0} for {1} extra damage!", target.Name, ad.Damage), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
		attacker.DealDamage(ad);
	}

	/// <summary>
	/// Cancels effect if one of players disbands
	/// </summary>
	/// <param name="e"></param>
	/// <param name="sender">The group</param>
	/// <param name="args"></param>
	protected void GroupDisbandCallback(CoreEvent e, object sender, EventArgs args)
	{
		MemberDisbandedEventArgs eArgs = args as MemberDisbandedEventArgs;
		if (eArgs == null) return;
		if (eArgs.Member == EffectOwner)
		{
			Cancel(false);
		}
	}
	/// <summary>
	/// Called when a player leaves the game
	/// </summary>
	/// <param name="e">The event which was raised</param>
	/// <param name="sender">Sender of the event</param>
	/// <param name="args">EventArgs associated with the event</param>
	protected void PlayerLeftWorld(CoreEvent e, object sender, EventArgs args)
	{
		Cancel(false);
	}

	public override string Name { get { return "Mark of Prey"; } }
	public override ushort Icon { get { return 3089; } }

	// Delve Info
	public override IList<string> DelveInfo
	{
		get
		{
			var list = new List<string>();
			list.Add("Grants a 30 second damage add that stacks with all other forms of damage add. All damage done via the damage add will be returned to the Vampiir as power.");
			return list;
		}
	}
}