using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class EngageECSGameEffect : ECSGameAbilityEffect
    {
        public EngageECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Engage;
            EffectService.RequestStartEffect(this);
        }

        /// <summary>
		/// The player that is defended by the engage source
		/// </summary>
		GameLiving m_engageTarget;

        /// <summary>
        /// Gets the defended player
        /// </summary>
        public GameLiving EngageTarget
        {
            get { return m_engageTarget; }
            set { m_engageTarget = value; }
        }

        public override ushort Icon { get { return 421; } }
        public override string Name 
        { 
            get 
            {
                if (m_engageTarget != null)
                    return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.EngageEffect.EngageName", m_engageTarget.GetName(0, false));
                return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.EngageEffect.Name");
            } 
        }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            m_engageTarget = Owner.TargetObject as GameLiving;
            Owner.IsEngaging = true;

            if (OwnerPlayer != null)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.ConcOnBlockingX", m_engageTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            
			// only emulate attack mode so it works more like on live servers
			// entering real attack mode while engaging someone stops engage
			// other players will see attack mode after pos update packet is sent
			if (!Owner.attackComponent.AttackState)
			{
                //Owner.attackComponent.StartAttack(m_engageTarget);
                Owner.attackComponent.AttackState = true;
				if (Owner is GamePlayer)
					(Owner as GamePlayer).Out.SendAttackMode(true);
				//m_engageSource.Out.SendMessage("You enter combat mode to engage your target!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
				//m_engageSource.Out.SendMessage("You enter combat mode and target ["+engageTarget.GetName(0, false)+"]", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
			}
			 
        }
        public override void OnStopEffect()
        {
            Owner.IsEngaging = false;
        }

        public void Cancel(bool playerCancel)
        {
            Owner.attackComponent.AttackState = false;
            if (Owner is GamePlayer)
                (Owner as GamePlayer).Out.SendAttackMode(false);

            EffectService.RequestImmediateCancelEffect(this, playerCancel);
            if (OwnerPlayer != null)
            {
                if (playerCancel)
                    OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.YouNoConcOnBlock"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                else
                    OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.EngageEffect.YouNoAttemptToEngageT"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
