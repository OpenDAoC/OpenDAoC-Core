using System.Collections.Generic;
using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
	public class NfRaAllureOfDeathAbility : Rr5RealmAbility
	{
		public NfRaAllureOfDeathAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;



			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				SendCasterSpellEffectAndCastMessage(player, 7076, true);
				NfRaAllureOfDeathEffect effect = new NfRaAllureOfDeathEffect();
				effect.Start(player);
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 420;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Allure of Death.");
			list.Add("");
			list.Add("Target: Self");
			list.Add("Duration: 60 seconds");
			list.Add("Casting time: instant");
		}

	}
}