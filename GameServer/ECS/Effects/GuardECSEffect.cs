﻿using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;

namespace DOL.GS
{
    public class GuardECSGameEffect : ECSGameAbilityEffect
    {
        public GuardECSGameEffect(ECSGameEffectInitParams initParams, GameLiving guardSource, GameLiving guardTarget) : base(initParams)
        {
            m_guardSource = guardSource;
            m_guardTarget = guardTarget;
            EffectType = eEffect.Guard;
            EffectService.RequestStartEffect(this);
        }

        /// <summary>
        /// Holds guarder
        /// </summary>
        private GameLiving m_guardSource;

        /// <summary>
        /// Gets guarder
        /// </summary>
        public GameLiving GuardSource => m_guardSource;

        /// <summary>
        /// Holds guarded player
        /// </summary>
        private GameLiving m_guardTarget;

        /// <summary>
        /// Gets guarded player
        /// </summary>
        public GameLiving GuardTarget => m_guardTarget;

        /// <summary>
        /// Holds player group
        /// </summary>
        private Group m_playerGroup;

        public override ushort Icon => Owner is GameNPC ? (ushort) 1001 : (ushort) 412;

        public override string Name
        {
            get
            {
                if (Owner is GamePlayer playerOwner)
                {
                    if (m_guardSource != null && m_guardTarget != null)
                        return LanguageMgr.GetTranslation(playerOwner.Client, "Effects.GuardEffect.GuardedByName", m_guardTarget.GetName(0, false), m_guardSource.GetName(0, false));

                    return LanguageMgr.GetTranslation(playerOwner.Client, "Effects.GuardEffect.Name");
                }

                return "";
            }
        }

        public override bool HasPositiveEffect => true;

        public override void OnStartEffect()
        {
            if (m_guardSource == null || m_guardTarget == null)
                return;

            GamePlayer playerSource = GuardSource as GamePlayer;
            GamePlayer playerTarget = GuardTarget as GamePlayer;

            if (playerSource != null && playerTarget != null)
            {
                m_playerGroup = GuardSource.Group;

                if (m_playerGroup == null)
                    return;

                if (m_playerGroup != GuardTarget.Group)
                    return;
            }

            if (Owner == GuardSource)
            {
                if (!GuardSource.IsWithinRadius(GuardTarget, GuardAbilityHandler.GUARD_DISTANCE))
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.GuardEffect.YouAreNowGuardingYBut", GuardTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.GuardEffect.XIsNowGuardingYouBut", GuardSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.GuardEffect.YouAreNowGuardingY", GuardTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.GuardEffect.XIsNowGuardingYou", GuardSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }

                new GuardECSGameEffect(new ECSGameEffectInitParams(GuardTarget, 0, 1, null), GuardSource, GuardTarget);
            }
        }

        public override void OnStopEffect()
        {
            ECSGameEffect otherGuard = null;

            if (GuardSource == Owner)
            {
                foreach (GuardECSGameEffect guard in GuardTarget.effectListComponent.GetAllEffects().Where(x => x.EffectType == eEffect.Guard))
                {
                    if (guard.GuardSource == Owner)
                    {
                        otherGuard = guard;
                        break;
                    }
                }
            }
            else if (GuardTarget == Owner)
            {
                foreach (GuardECSGameEffect guard in GuardSource.effectListComponent.GetAllEffects().Where(x => x.EffectType == eEffect.Guard))
                {
                    if (guard.GuardTarget == Owner)
                    {
                        otherGuard = guard;
                        break;
                    }
                }
            }

            if (otherGuard != null)
            {
                EffectService.RequestImmediateCancelEffect(otherGuard);

                GamePlayer playerSource = GuardSource as GamePlayer;
                GamePlayer playerTarget = GuardTarget as GamePlayer;

                playerSource?.Out.SendMessage(LanguageMgr.GetTranslation(playerSource.Client, "Effects.GuardEffect.YourNoLongerGuardingY", m_guardTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                playerTarget?.Out.SendMessage(LanguageMgr.GetTranslation(playerTarget.Client, "Effects.GuardEffect.XNoLongerGuardingYoy", m_guardSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            base.OnStopEffect();
        }
    }
}
