using System.Reflection;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS;
using log4net;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Handler for Guard ability clicks
    /// </summary>
    [SkillHandler(Abilities.Bodyguard)]
    public class BodyguardAbilityHandler : IAbilityActionHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The guard distance
        /// </summary>
        public const int BODYGUARD_DISTANCE = 300;

        public void Execute(AbilityUtil ab, GamePlayer player)
        {
            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Bodyguard.Dead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }
            if (player == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not retrieve player in BodyguardAbilityHandler.");
                return;
            }

            GameObject targetObject = player.TargetObject;
            if (targetObject == null)
            {
				foreach (BodyguardEffect bg in player.EffectList.GetAllOfType<BodyguardEffect>())
                {
                    if (bg.GuardSource == player)
                        bg.Cancel(false);
                }
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Bodyguard.Dead"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            // You cannot guard attacks on yourself            
            GamePlayer guardTarget = player.TargetObject as GamePlayer;
            if (guardTarget == player)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Bodyguard.GuardTargetIsGuardSource"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            // Only attacks on other players may be guarded. 
            // guard may only be used on other players in group
            Group group = player.Group;
            if (guardTarget == null || group == null || !group.IsInTheGroup(guardTarget))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Bodyguard.GuardTargetIsGuardSource"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            // check if someone is guarding the target
			foreach (BodyguardEffect bg in guardTarget.EffectList.GetAllOfType<BodyguardEffect>())
            {
                if (bg.GuardTarget != guardTarget) continue;
                if (bg.GuardSource == player)
                {
                    bg.Cancel(false);
                    return;
                }
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Bodyguard.GuardTargetAlreadyBodyGuarded", bg.GuardSource.GetName(0, true), bg.GuardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

			foreach (BodyguardEffect bg in player.EffectList.GetAllOfType<BodyguardEffect>())
            {
                    if (bg != null && player == bg.GuardTarget)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUse.Bodyguard.GuardSourceBodyGuarded"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        return;
                    }
            }

            // cancel all guard effects by this player before adding a new one
			foreach (BodyguardEffect bg in player.EffectList.GetAllOfType<BodyguardEffect>())
            {
                if (bg.GuardSource == player)
                    bg.Cancel(false);
            }

            new BodyguardEffect().Start(player, guardTarget);
        }
    }
}
