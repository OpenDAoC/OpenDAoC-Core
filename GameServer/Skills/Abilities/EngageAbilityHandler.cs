using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.Languages;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS.SkillHandler
{
    [SkillHandler(Abilities.Engage)]
    public class EngageAbilityHandler : IAbilityActionHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// wait 5 sec to engage after attack
        /// </summary>
        public const int ENGAGE_ATTACK_DELAY_TICK = 5000;

        /// <summary>
        /// Endurance lost on every attack
        /// </summary>
        public const int ENGAGE_ENDURANCE_COST = 5;

        /// <summary>
        /// Execute engage ability
        /// </summary>
        /// <param name="ab">The used ability</param>
        /// <param name="player">The player that used the ability</param>
        public void Execute(Ability ab, GamePlayer player)
        {
            if (player == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not retrieve player in EngageAbilityHandler.");

                return;
            }

            //Cancel old engage effects on player
            if (player.IsEngaging)
            {
                if (EffectListService.GetEffectOnTarget(player, EEffect.Engage) is EngageEcsAbilityEffect engage)
                {
                    engage.Cancel(true, true);
                    return;
                }
            }

            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Engage.CannotUseDead"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                return;
            }

            if (player.IsSitting)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Engage.CannotUseStanding"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                return;
            }

            if (player.ActiveWeaponSlot == EActiveWeaponSlot.Distance)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Engage.CannotUseNoCaCWeapons"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                return;
            }

            if (player.TargetObject is not GameLiving target)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Engage.NoTarget"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            // You cannot engage a mob that was attacked within the last 5 seconds...
            if (target.LastAttackedByEnemyTick > GameLoopMgr.GameLoopTime - ENGAGE_ATTACK_DELAY_TICK)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Engage.TargetAttackedRecently", target.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            if (!GameServer.ServerRules.IsAllowedToAttack(player, target, true))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Engage.NotAllowedToEngageTarget", target.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

            new EngageEcsAbilityEffect(new EcsGameEffectInitParams(player, 0, 1, null));
        }
    }
}
