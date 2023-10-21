using System;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.ECS;

public class TripleWieldEcsAbilityEffect : EcsGameAbilityEffect
{
    public TripleWieldEcsAbilityEffect(EcsGameEffectInitParams initParams)
        : base(initParams)
    {
        EffectType = EEffect.TripleWield;
		EffectService.RequestStartEffect(this);
	}

    protected ushort m_startModel = 0;

    public override ushort Icon { get { return 475; } }
    public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.TripleWieldEffect.Name"); } }
    public override bool HasPositiveEffect { get { return true; } }

    public override void OnStartEffect()
    {
        GamePlayer player = Owner as GamePlayer;
        foreach (GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            p.Out.SendSpellEffectAnimation(player, player, 7102, 0, false, 1);
            p.Out.SendSpellCastAnimation(player, Icon, 0);
        }
    }
    public override void OnStopEffect()
    {

    }

	/// <summary>
	/// Handler fired on every melee attack by effect target
	/// </summary>
	/// <param name="e"></param>
	/// <param name="sender"></param>
	/// <param name="arguments"></param>
	public void EventHandler(AttackData attackData)
	{
		if (attackData == null) return;
		if (attackData.AttackResult != EAttackResult.HitUnstyled
			&& attackData.AttackResult != EAttackResult.HitStyle) return;
		if (attackData.Target == null) return;
		GameLiving target = attackData.Target;
		if (target == null) return;
		if (target.ObjectState != GameObject.eObjectState.Active) return;
		if (target.IsAlive == false) return;
		GameLiving attacker = Owner as GameLiving;
		if (attacker == null) return;
		if (attacker.ObjectState != GameObject.eObjectState.Active) return;
		if (attacker.IsAlive == false) return;
		//if (attackData.IsOffHand) return; // only react to main hand
		if (attackData.Weapon == null) return; // no weapon attack

		int modifier = 100;
		//double dpsCap = (1.2 + 0.3 * attacker.Level) * 0.7;
		//double dps = Math.Min(atkArgs.AttackData.Weapon.DPS_AF/10.0, dpsCap);
		double baseDamage = attackData.Weapon.DPS_AF / 10.0 *
							attackData.WeaponSpeed;

		modifier += (int)(25 * attackData.Target.GetConLevel(attackData.Attacker));
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
		ad.WeaponSpeed = attackData.WeaponSpeed;

		GamePlayer owner = attacker as GamePlayer;
		if (owner != null)
		{
			owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client, "Effects.TripleWieldEffect.MBHitsExtraDamage", target.GetName(0, false), ad.Damage), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
			GamePlayer playerTarget = target as GamePlayer;
			if (playerTarget != null)
			{
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
}