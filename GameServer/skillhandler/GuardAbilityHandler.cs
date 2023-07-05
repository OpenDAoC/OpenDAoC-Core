/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System.Linq;
using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Handler for Guard ability clicks
    /// </summary>
    [SkillHandler(Abilities.Guard)]
    public class GuardAbilityHandler : IAbilityActionHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const int GUARD_DISTANCE = 256;

        public void Execute(Ability ab, GamePlayer player)
        {
            if (player == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not retrieve player in GuardAbilityHandler.");

                return;
            }

            if (player.TargetObject == null)
            {
                foreach (GuardECSGameEffect guard in player.effectListComponent.GetAllEffects().Where(e => e.EffectType == eEffect.Guard))
                {
                    if (guard.GuardSource == player)
                        guard.Cancel();
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CancelTargetNull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            GamePlayer guardTarget = player.TargetObject as GamePlayer;

            if (guardTarget == player)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.GuardTargetIsGuardSource"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            Group group = player.Group;

            if (guardTarget == null || group == null || !group.IsInTheGroup(guardTarget))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.NotInGroup"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // Cancel our effect if it exists and check if someone is already guarding the target.
            CheckExistingEffectsOnTarget(player, guardTarget, true, out bool foundOurEffect, out GuardECSGameEffect existingEffectFromAnotherSource);

            if (foundOurEffect)
                return;

            if (existingEffectFromAnotherSource != null)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.Guard.CannotUse.GuardTargetAlreadyGuarded", existingEffectFromAnotherSource.GuardSource.GetName(0, true), existingEffectFromAnotherSource.GuardTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // Cancel other guard effects by this player before adding a new one.
            CancelOurEffectThenAddOnTarget(player, guardTarget);
        }

        public static void CheckExistingEffectsOnTarget(GameLiving guardSource, GameLiving guardTarget, bool cancelOurs, out bool foundOurEffect, out GuardECSGameEffect effectFromAnotherSource)
        {
            foundOurEffect = false;
            effectFromAnotherSource = null;

            foreach (GuardECSGameEffect guard in guardTarget.effectListComponent.GetAllEffects().Where(e => e.EffectType == eEffect.Guard))
            {
                if (guard.GuardSource == guardSource)
                {
                    foundOurEffect = true;

                    if (cancelOurs)
                        guard.Cancel();
                }

                if (guard.GuardTarget == guardTarget)
                    effectFromAnotherSource = guard;
            }
        }

        public static void CancelOurEffectThenAddOnTarget(GameLiving guardSource, GameLiving guardTarget)
        {
            foreach (GuardECSGameEffect guard in guardSource.effectListComponent.GetAllEffects().Where(e => e.EffectType == eEffect.Guard))
            {
                if (guard.GuardSource == guardSource)
                    guard.Cancel();
            }

            new GuardECSGameEffect(new ECSGameEffectInitParams(guardSource, 0, 1, null), guardSource, guardTarget);
        }
    }
}
