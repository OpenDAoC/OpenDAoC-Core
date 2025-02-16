using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Mastery of Concentration RA
	/// </summary>
	public class ShieldTripAbility : RR5RealmAbility
	{
		public ShieldTripAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			DbInventoryItem shield = living.ActiveLeftWeapon;
			if (shield == null)
				return;
			if (shield.Object_Type != (int)eObjectType.Shield)
				return;
			if (living.TargetObject == null)
				return;
			if (living.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
				return;
			if (living.ActiveWeapon == null)
				return;
			if (living.ActiveWeapon.Hand == 1)
				return;
			GameLiving target = (GameLiving)living.TargetObject;
			if (target == null) return;
			if (!GameServer.ServerRules.IsAllowedToAttack(living, target, false))
				return;
			if (!living.IsWithinRadius( target, 1000 ))
				return;
			new ShieldTripRootEffect().Start(target);

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				SendCasterSpellEffectAndCastMessage(player, 7046, true);
				ShieldTripDisarmEffect effect = new ShieldTripDisarmEffect();
				effect.Start(player);
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 900;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Roots your target for 10 seconds but disarms you for 15 seconds!");
			list.Add("");
			list.Add("Range: 1000");
			list.Add("Target: Enemy");
			list.Add("Casting time: instant");
		}

	}
}