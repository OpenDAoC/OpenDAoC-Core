using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
	public class NfRaBladeBarrierAbility : Rr5RealmAbility
	{
		public NfRaBladeBarrierAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				SendCasterSpellEffectAndCastMessage(player, 7055, true);
				NfRaBladeBarrierEffect effect = new NfRaBladeBarrierEffect();
				effect.Start(player);
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 300;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Gives you a 90% 360� Parry buff which is broken if the Effect Owner attacks");
			list.Add("");
			list.Add("Target: Self");
			list.Add("Duration: 30 sec");
			list.Add("Casting time: instant");
		}

	}
}