using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
	public class NfRaTestudoAbility : Rr5RealmAbility
	{
		public NfRaTestudoAbility(DbAbility dba, int level) : base(dba, level) { }

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
			if (living.ActiveWeapon.Hand == 1)
				return;

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				SendCasterSpellEffectAndCastMessage(player, 7068, true);
				NfRaTestudoEffect effect = new NfRaTestudoEffect();
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