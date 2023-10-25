using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Languages;
using log4net;

namespace Core.GS.Skills;

[SkillHandler(AbilityConstants.DirtyTricks)]
public class DirtyTricksAbilityHandler : IAbilityActionHandler
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
	/// <summary>
	/// The ability reuse time in seconds
	/// </summary>
	protected const int REUSE_TIMER = 60000 * 7;

	/// <summary>
	/// The ability effect duration in seconds
	/// </summary>
	public const int DURATION = 30;

	/// <summary>
	/// Execute dirtytricks ability
	/// </summary>
	/// <param name="ab">The used ability</param>
	/// <param name="player">The player that used the ability</param>
	public void Execute(Ability ab, GamePlayer player)
	{
		if (player == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("Could not retrieve player in DirtyTricksAbilityHandler.");
			return;
		}

		if (!player.IsAlive)
		{
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
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

		player.DisableSkill(ab, REUSE_TIMER);
		new DirtyTricksEcsAbilityEffect(new EcsGameEffectInitParams(player, DURATION * 1000, 1));
	}
}