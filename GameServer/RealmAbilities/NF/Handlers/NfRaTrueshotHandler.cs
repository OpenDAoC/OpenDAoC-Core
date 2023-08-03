using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Trueshot RA, grants 50% more range on next archery attack
	/// </summary>
	public class NfRaTrueshotHandler : TimedRealmAbility
	{
		public NfRaTrueshotHandler(DbAbilities dba, int level) : base(dba, level) { }

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
				SureShotEcsEffect sureShot = (SureShotEcsEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.SureShot);
				if (sureShot != null)
					EffectService.RequestImmediateCancelEffect(sureShot);

				RapidFireEcsEffect rapidFire = (RapidFireEcsEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.RapidFire);
				if (rapidFire != null)
					EffectService.RequestImmediateCancelEffect(rapidFire, false);

				new TrueShotEcsEffect(new ECSGameEffectInitParams(player, 0, 1));
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