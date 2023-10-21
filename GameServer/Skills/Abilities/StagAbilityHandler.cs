using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Languages;
using log4net;

namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Stag)]
	public class StagAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The ability reuse time in milliseconds
		/// </summary>
		protected const int REUSE_TIMER = 60000 * 15; // clait: 15 minutes [og: 20]

		/// <summary>
		/// The ability effect duration in milliseconds
		/// </summary>
		public const int DURATION = 30 * 1000; // 30 seconds

		public void Execute(Ability ab, GamePlayer player)
		{
			if (player == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not retrieve player in StagAbilityHandler.");
				return;
			}

            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
            //if (player.IsMezzed)
            //{
            //    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseMezzed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            //    return;
            //}
            //if (player.IsStunned)
            //{
            //    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStunned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            //    return;
            //}
            if (player.IsSitting)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStanding"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
			//Cancel old stag effects on player
			StagEcsAbilityEffect stag = (StagEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(player, EEffect.Stag);
			if (stag != null)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseAlreadyActive"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}
			player.DisableSkill(ab, REUSE_TIMER);

			new StagEcsAbilityEffect(new EcsGameEffectInitParams(player, DURATION, 1), ab.Level);
		}
	}
}
