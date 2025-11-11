using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
	/// <summary>
	/// Handler for Sprint Ability clicks
	/// </summary>
	[SkillHandlerAttribute(Abilities.Berserk)]
	public class BerserkAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

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
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (player.IsMezzed)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseMezzed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (player.IsStunned)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStunned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
			if (player.IsSitting)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStanding"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			ECSGameEffect berserk = EffectListService.GetEffectOnTarget(player, eEffect.Berserk);
			berserk?.End();
			player.DisableSkill(ab, REUSE_TIMER);
			ECSGameEffectFactory.Create(new(player, DURATION, 1, null), static (in ECSGameEffectInitParams i) => new BerserkECSGameEffect(i));
        }
    }
}
