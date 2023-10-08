using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
	public class NfRaSelflessDevotionAbility : Rr5RealmAbility
	{
		public NfRaSelflessDevotionAbility(DbAbility dba, int level) : base(dba, level) { }

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				SendCasterSpellEffectAndCastMessage(player, 7039, true);
				NfRaSelflessDevotionEffect effect = new NfRaSelflessDevotionEffect();
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
			list.Add("Decrease Paladin stats by 25%, and pulse a 300 points group heal with a 750 units range every 3 seconds for 15 seconds total.");
			list.Add("");
			list.Add("Duration: 15 sec");
			list.Add("Casting time: instant");
		}
	}
}