using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
	public class NfRaShieldTripAbility : Rr5RealmAbility
	{
		public NfRaShieldTripAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			DbInventoryItem shield = living.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
			if (shield == null)
				return;
			if (shield.Object_Type != (int)EObjectType.Shield)
				return;
			if (living.TargetObject == null)
				return;
			if (living.ActiveWeaponSlot == EActiveWeaponSlot.Distance)
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
			new NfRaShieldTripRootEffect().Start(target);

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				SendCasterSpellEffectAndCastMessage(player, 7046, true);
				NfRaShieldTripDisarmEffect effect = new NfRaShieldTripDisarmEffect();
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