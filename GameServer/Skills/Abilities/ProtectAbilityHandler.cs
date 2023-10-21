using System.Linq;
using System.Reflection;
using Core.GS.ECS;
using Core.GS.PacketHandler;
using Core.Language;
using log4net;

namespace Core.GS.SkillHandler
{
	[SkillHandler(Abilities.Protect)]
	public class ProtectAbilityHandler : IAbilityActionHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The Protect Distance
		/// </summary>
		public const int PROTECT_DISTANCE = 1000;

		public void Execute(Ability ab, GamePlayer player)
		{
			if (player == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not retrieve player in ProtectAbilityHandler.");
				return;
			}

			GameObject targetObject = player.TargetObject;
			if (targetObject == null)
			{
				foreach (ProtectEcsAbilityEffect protect in player.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == EEffect.Protect))
				{
					if (protect.ProtectSource == player)
						protect.Cancel(false);
				}
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CancelTargetNull"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
			}

			// You cannot protect attacks on yourself            
			GamePlayer protectTarget = player.TargetObject as GamePlayer;
			if (protectTarget == player)
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CannotUse.CantProtectYourself"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			// Only attacks on other players may be protected. 
			// protect may only be used on other players in group
			GroupUtil group = player.Group;
			if (protectTarget == null || group == null || !group.IsInTheGroup(protectTarget))
			{
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CannotUse.NotInGroup"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			// check if someone is protecting the target
			foreach (ProtectEcsAbilityEffect protect in protectTarget.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == EEffect.Protect))
			{
				if (protect.ProtectTarget != protectTarget)
					continue;
				if (protect.ProtectSource == player)
				{
					protect.Cancel(false);
					return;
				}
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Protect.CannotUse.ProtectTargetAlreadyProtectEffect", protect.ProtectSource.GetName(0, true), protect.ProtectTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			// cancel all guard effects by this player before adding a new one
			foreach (ProtectEcsAbilityEffect protect in player.effectListComponent.GetAbilityEffects().Where(e => e.EffectType == EEffect.Protect))
			{
				if (protect.ProtectSource == player)
					protect.Cancel(false);
			}

			//new ProtectEffect().Start(player, protectTarget);
			new ProtectEcsAbilityEffect(new EcsGameEffectInitParams(player, 0, 1), player, protectTarget);
		}
	}
}