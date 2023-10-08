using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.SkillHandler
{
	[SkillHandler(Abilities.Berserk)]
	public class BerserkAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The reuse time in milliseconds for berserk ability
		/// </summary>
		protected const int REUSE_TIMER = 60000 * 7; // clait: 10 minutes [og: 7]

		/// <summary>
		/// The effect duration in milliseconds
		/// </summary>
		public const int DURATION = 20000;

		/// <summary>
		/// Execute the ability
		/// </summary>
		/// <param name="ab">The ability executed</param>
		/// <param name="player">The player that used the ability</param>
		public void Execute(Ability ab, GamePlayer player)
		{
			if (player == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not retrieve player in BerserkAbilityHandler.");
				return;
			}

            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
            if (player.IsMezzed)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseMezzed"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
            if (player.IsStunned)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStunned"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
			if (player.IsSitting)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStanding"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			//Cancel old berserk effects on player
			//BerserkEffect berserk = player.EffectList.GetOfType<BerserkEffect>();
			//if (berserk!=null)
			//{
			//	berserk.Cancel(false);
			//	return;
			//}
			EcsGameEffect berserk = EffectListService.GetEffectOnTarget(player, EEffect.Berserk);
			if (berserk != null)
				EffectService.RequestImmediateCancelEffect(berserk);

			player.DisableSkill(ab, REUSE_TIMER);

			//new BerserkEffect().Start(player);
			new BerserkEcsAbilityEffect(new EcsGameEffectInitParams(player, DURATION, 1, null));
		}                       
    }
}
