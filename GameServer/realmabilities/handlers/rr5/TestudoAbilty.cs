using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Testudo Realm Ability
	/// </summary>
	public class TestudoAbility : RR5RealmAbility
	{
		public TestudoAbility(DbAbility dba, int level) : base(dba, level) { }

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
			if (living.ActiveWeapon.Hand == 1)
				return;

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				SendCasterSpellEffectAndCastMessage(player, 7068, true);
				TestudoEffect effect = new TestudoEffect();
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
			list.Add("Warrior with shield equipped covers up and takes 90% less damage for all attacks for 45 seconds. Can only move at reduced speed (speed buffs have no effect) and cannot attack. Using a style will break testudo form. This ability is only effective versus realm enemies.");
			list.Add("");
			list.Add("Target: Self");
			list.Add("Duration: 45 sec");
			list.Add("Casting time: instant");
		}

	}
}