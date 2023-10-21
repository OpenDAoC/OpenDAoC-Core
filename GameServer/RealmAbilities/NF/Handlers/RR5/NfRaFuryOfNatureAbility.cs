using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
	public class NfRaFuryOfNatureAbility : Rr5RealmAbility
	{
		public NfRaFuryOfNatureAbility(DbAbility dba, int level) : base(dba, level) { }

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
				SendCasterSpellEffectAndCastMessage(player, 5103, true);
				NfRaFuryOfNatureEffect effect = new NfRaFuryOfNatureEffect();
				effect.Start(player);
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 600;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Doubles Style Damage for 30 seconds and heals the group, excluding the caster, like spreadheal with all the damage dealt");
			list.Add("");
			list.Add("Target: Self");
			list.Add("Duration: 30 sec");
			list.Add("Casting time: instant");
		}

	}
}