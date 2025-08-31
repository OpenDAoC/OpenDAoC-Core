using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Trueshot RA, grants 50% more range on next archery attack
	/// </summary>
	public class TrueshotAbility : TimedRealmAbility
	{
		public TrueshotAbility(DbAbility dba, int level) : base(dba, level) { }

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
				SureShotECSGameEffect sureShot = EffectListService.GetAbilityEffectOnTarget(player, eEffect.SureShot) as SureShotECSGameEffect;
				sureShot?.Stop();

				RapidFireECSGameEffect rapidFire = EffectListService.GetAbilityEffectOnTarget(player, eEffect.RapidFire) as RapidFireECSGameEffect;
				rapidFire?.Stop(false);

				ECSGameEffectFactory.Create(new(player, 0, 1), this, static (in ECSGameEffectInitParams i, TrueshotAbility trueshot) => new TrueShotECSGameEffect(trueshot, i));
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			switch (level)
			{
				case 1: return 600;
				case 2: return 180;
				case 3: return 30;
			}
			return 600;
		}
	}
}