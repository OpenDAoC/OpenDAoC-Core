using System.Linq;
using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.Language;
using log4net;

namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Intercept)]
	public class InterceptAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The intercept distance
		/// </summary>
		public const int INTERCEPT_DISTANCE = 128;

		/// <summary>
		/// Intercept reuse timer in milliseconds
		/// </summary>
		public const int REUSE_TIMER = 60 * 1000;

		/// <summary>
		/// Executes the ability
		/// </summary>
		/// <param name="ab">The ability used</param>
		/// <param name="player">The player that used the ability</param>
		public void Execute(Ability ab, GamePlayer player)
		{
			if (player == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not retrieve player in InterceptAbilityHandler.");
				return;
			}

			GameObject targetObject = player.TargetObject;
			if (targetObject == null)
			{
				//foreach (InterceptEffect intercept in player.EffectList.GetAllOfType<InterceptEffect>())
				foreach (InterceptEcsAbilityEffect intercept in player.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == EEffect.Intercept))
				{
					if (intercept.InterceptSource != player)
						continue;
					intercept.Cancel(false);
				}
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Intercept.CancelTargetNull"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			// Only attacks on other players may be intercepted. 
			// You cannot intercept attacks on yourself            
			GroupUtil group = player.Group;
			GamePlayer interceptTarget = targetObject as GamePlayer;
			if (interceptTarget == null || group == null || !group.IsInTheGroup(interceptTarget) || interceptTarget == player)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Intercept.CannotUse.NotInGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			// check if someone is already intercepting for that target
			//foreach (InterceptEffect intercept in interceptTarget.EffectList.GetAllOfType<InterceptEffect>())
			foreach (InterceptEcsAbilityEffect intercept in interceptTarget.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == EEffect.Intercept))
			{
				if (intercept.InterceptTarget != interceptTarget)
					continue;
				if (intercept.InterceptSource != player && !(intercept.InterceptSource is GameNpc))
				{
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Intercept.CannotUse.InterceptTargetAlreadyInterceptedEffect", intercept.InterceptSource.GetName(0, true), intercept.InterceptTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return;
				}
			}

			// cancel all intercepts by this player
			//foreach (InterceptEffect intercept in player.EffectList.GetAllOfType<InterceptEffect>())
			foreach (InterceptEcsAbilityEffect intercept in player.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == EEffect.Intercept))
			{
				if (intercept.InterceptSource != player)
					continue;
				intercept.Cancel(false);
			}

			player.DisableSkill(ab, REUSE_TIMER);

			//new InterceptEffect().Start(player, interceptTarget);
			new InterceptEcsAbilityEffect(new EcsGameEffectInitParams(player, 0, 1), player, (GameLiving)player.TargetObject);
		}
	}
}