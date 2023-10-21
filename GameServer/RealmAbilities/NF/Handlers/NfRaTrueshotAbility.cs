using Core.Database;

namespace Core.GS.RealmAbilities
{
	public class NfRaTrueshotAbility : TimedRealmAbility
	{
		public NfRaTrueshotAbility(DbAbility dba, int level) : base(dba, level) { }

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
				SureShotEcsAbilityEffect sureShot = (SureShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.SureShot);
				if (sureShot != null)
					EffectService.RequestImmediateCancelEffect(sureShot);

				RapidFireEcsAbilityEffect rapidFire = (RapidFireEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.RapidFire);
				if (rapidFire != null)
					EffectService.RequestImmediateCancelEffect(rapidFire, false);

				new TrueShotEcsAbilityEffect(new EcsGameEffectInitParams(player, 0, 1));
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