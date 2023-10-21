using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.RealmAbilities
{
	public class NfRaDesperateBowmanAbility : Rr5RealmAbility
	{
		public NfRaDesperateBowmanAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			if (living.TargetObject == null)
				return;

			GameLiving target = (GameLiving)living.TargetObject;

			if (target == null) return;

			if (!GameServer.ServerRules.IsAllowedToAttack(living, target, false))
				return;

			if (!living.IsWithinRadius(target, 1000))
				return;

			if (living.ActiveWeaponSlot != EActiveWeaponSlot.Distance)
				return;

			SendCasterSpellEffectAndCastMessage(living, 7061, true);
			new NfRaDesperateBowmanDisarmEffect().Start(living);
			new NfRaDesperateBowmanStunEffect().Start(target);
			DamageTarget(target, living);
			DisableSkill(living);
		}

		private void DamageTarget(GameLiving target, GameLiving caster)
		{
			
			int damage = 300;

			GamePlayer player = caster as GamePlayer;
			if (player != null)
				player.Out.SendMessage("You hit " + target.Name + " for " + damage + " points of damage!", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);

			GamePlayer targetPlayer = target as GamePlayer;
			if (targetPlayer != null)
			{
				if (targetPlayer.IsStealthed)
					targetPlayer.Stealth(false);
			}

			AttackData ad = new AttackData();
			ad.AttackResult = EAttackResult.HitUnstyled;
			ad.Attacker = caster;
			ad.Target = target;
			ad.DamageType = EDamageType.Crush;
			ad.Damage = damage;
			target.OnAttackedByEnemy(ad);
			caster.DealDamage(ad);
		}
		
		
		public override int GetReUseDelay(int level)
		{
			return 420;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Stuns your target for 5 seconds and damage it for 300 , but disarms you for 15 seconds! You need a bow in your hand to use this ability!");
			list.Add("");
			list.Add("Range: 1000");
			list.Add("Target: Enemy");
			list.Add("Casting time: instant");
		}

	}
}