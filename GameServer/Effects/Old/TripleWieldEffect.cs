using System;
using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.World;

namespace Core.GS.Effects.Old;

public class TripleWieldEffect : TimedEffect
{
	public TripleWieldEffect(int duration) : base(duration)
	{
	}

	public override void Start(GameLiving target)
	{
		base.Start(target);
		GamePlayer player = target as GamePlayer;
        foreach(GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
		    p.Out.SendSpellEffectAnimation(player, player, 7102, 0, false, 1);
		    p.Out.SendSpellCastAnimation(player, Icon, 0);
        }
		GameEventMgr.AddHandler(player, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));			
	}

	public override void Stop()
	{
		base.Stop();
		GamePlayer player = Owner as GamePlayer;
		GameEventMgr.RemoveHandler(player, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));
	}

	/// <summary>
	/// Handler fired on every melee attack by effect target
	/// </summary>
	/// <param name="e"></param>
	/// <param name="sender"></param>
	/// <param name="arguments"></param>
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

		int modifier = 100;
		//double dpsCap = (1.2 + 0.3 * attacker.Level) * 0.7;
		//double dps = Math.Min(atkArgs.AttackData.Weapon.DPS_AF/10.0, dpsCap);
		double baseDamage = atkArgs.AttackData.Weapon.DPS_AF/10.0*
		                    atkArgs.AttackData.WeaponSpeed;

		modifier += (int)(25 * atkArgs.AttackData.Target.GetConLevel(atkArgs.AttackData.Attacker));
		modifier = Math.Min(300, modifier);
		modifier = Math.Max(75, modifier);
		
		double damage = baseDamage * modifier * 0.001; // attack speed is 10 times higher (2.5spd=25)			
		double damageResisted = damage * target.GetResist(EDamageType.Body) * -0.01;
		
		AttackData ad = new AttackData();
		ad.Attacker = attacker;
		ad.Target = target;
		ad.Damage = (int)(damage + damageResisted);
		ad.Modifier = (int)damageResisted;
		ad.DamageType = EDamageType.Body;
		ad.AttackType = EAttackType.MeleeOneHand;
		ad.AttackResult = EAttackResult.HitUnstyled;
        ad.WeaponSpeed = atkArgs.AttackData.WeaponSpeed; 

		GamePlayer owner = attacker as GamePlayer;
		if (owner != null) {
			owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client, "Effects.TripleWieldEffect.MBHitsExtraDamage", target.GetName(0, false), ad.Damage), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
			GamePlayer playerTarget = target as GamePlayer;
			if (playerTarget != null) {
                playerTarget.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.TripleWieldEffect.XMBExtraDamageToYou", attacker.GetName(0, false), ad.Damage), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
			}
		}
		
		target.OnAttackedByEnemy(ad);
		attacker.DealDamage(ad);

		foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x0A, target.HealthPercent);
		}
	}

	public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.TripleWieldEffect.Name"); } }

	public override ushort Icon { get { return 475; } }

	public override IList<string> DelveInfo
	{
		get
		{
			var list = new List<string>(4); ;
			list.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.TripleWieldEffect.InfoEffect"));
			list.AddRange(base.DelveInfo);

			return list;
		}
	}
}