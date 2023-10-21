using System.Collections.Generic;
using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
	public class NfRaShadowShroudAbility : Rr5RealmAbility
	{
		public const int DURATION = 30 * 1000;
		public const double ABSPERCENT = 10; // 10% damage absorb
		public const int MISSHITBONUS = 10; // 10% misshit bonus
		public const int EFFECT = 1565;

		public NfRaShadowShroudAbility(DbAbility dba, int level) : base(dba, level) { }

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
				NfRaShadowShroudEffect ShadowShroud = (NfRaShadowShroudEffect)player.EffectList.GetOfType<NfRaShadowShroudEffect>();
				if (ShadowShroud != null)
					ShadowShroud.Cancel(false);

				new NfRaShadowShroudEffect().Start(player);
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 300;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Reduce all incoming damage by 10% and increase the Nightshade�s chance to be missed by 10% for 30 seconds");
			list.Add("");
			list.Add("Target: Self");
			list.Add("Duration: 30 sec");
			list.Add("Casting time: Instant");
			list.Add("Re-use : 5 minutes");

		}

	}
}
