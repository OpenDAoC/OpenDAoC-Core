using System.Collections.Generic;
using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
	public class NfRaRetributionOfTheFaithfulAbility : Rr5RealmAbility
	{
		public NfRaRetributionOfTheFaithfulAbility(DbAbility dba, int level) : base(dba, level) { }

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
				SendCasterSpellEffectAndCastMessage(player, 7042, true);
				NfRaRetributionOfTheFaithfulEffect effect = new NfRaRetributionOfTheFaithfulEffect();
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
			list.Add("30 second buff that has a chance to proc a 3 second (duration undiminished by resists) stun on any melee attack on the cleric.");
			list.Add("");
			list.Add("Target: Self");
			list.Add("Duration: 30 sec");
			list.Add("Casting time: instant");
		}

	}
}