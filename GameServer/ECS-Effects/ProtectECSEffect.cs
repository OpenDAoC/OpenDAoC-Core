using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class ProtectECSGameEffect : ECSGameAbilityEffect
    {
        public ProtectECSGameEffect(ECSGameEffectInitParams initParams, GamePlayer protectSource, GamePlayer protectTarget)
            : base(initParams)
        {
			m_protectSource = protectSource;
			m_protectTarget = protectTarget;
			m_playerGroup = protectSource.Group;
            EffectType = eEffect.Protect;
            EffectService.RequestStartEffect(this);
		}

		/// <summary>
		/// The player protecting the target
		/// </summary>
		GamePlayer m_protectSource;

		/// <summary>
		/// Gets the player protecting the target
		/// </summary>
		public GamePlayer ProtectSource
		{
			get { return m_protectSource; }
			set { m_protectSource = value; }
		}

		/// <summary>
		/// Reference to gameplayer that is protecting this player
		/// </summary>
		GamePlayer m_protectTarget = null;

		/// <summary>
		/// Gets the protected player
		/// </summary>
		public GamePlayer ProtectTarget
		{
			get { return m_protectTarget; }
			set { m_protectTarget = value; }
		}

		private Group m_playerGroup;

		public override ushort Icon { get { return 411; } }
        public override string Name 
		{
			get
			{
				if (m_protectSource != null && m_protectTarget != null)
					return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.ProtectEffect.ProtectByName", m_protectTarget.GetName(0, false), m_protectSource.GetName(0, false));
				return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.ProtectEffect.Name");
			}
		}
        public override bool HasPositiveEffect { get { return true; } }
        public override long GetRemainingTimeForClient() { return 0; }
        public override void OnStartEffect()
        {
	        if (ProtectSource == null || ProtectTarget == null)			
				return;


			if (m_playerGroup != ProtectTarget.Group)
				return;

			if(m_playerGroup != null)
				GameEventMgr.AddHandler(m_playerGroup, GroupEvent.MemberDisbanded, new DOLEventHandler(GroupDisbandCallback));

			
			if (Owner == ProtectSource)
			{
				if (!ProtectSource.IsWithinRadius(ProtectTarget, ProtectAbilityHandler.PROTECT_DISTANCE))
				{
					ProtectSource.Out.SendMessage(LanguageMgr.GetTranslation(ProtectSource.Client, "Effects.ProtectEffect.YouProtectingYBut", ProtectTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					ProtectTarget.Out.SendMessage(LanguageMgr.GetTranslation(ProtectTarget.Client, "Effects.ProtectEffect.XProtectingYouBut", ProtectSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
				{
					ProtectSource.Out.SendMessage(LanguageMgr.GetTranslation(ProtectSource.Client, "Effects.ProtectEffect.YouProtectingY", ProtectTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					ProtectTarget.Out.SendMessage(LanguageMgr.GetTranslation(ProtectTarget.Client, "Effects.ProtectEffect.XProtectingYou", ProtectSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				new ProtectECSGameEffect(new ECSGameEffectInitParams(ProtectTarget, 0, 1), ProtectSource, ProtectTarget);
			}
		}
        public override void OnStopEffect()
        {

        }
		public void Cancel(bool playerCancel)
		{
			if(m_playerGroup != null)
				GameEventMgr.RemoveHandler(m_playerGroup, GroupEvent.MemberDisbanded, new DOLEventHandler(GroupDisbandCallback));

			var protectSourceEffect = EffectListService.GetEffectOnTarget(m_protectSource, eEffect.Protect);
			if (protectSourceEffect != null)
				EffectService.RequestImmediateCancelEffect(protectSourceEffect);
			var protectTargetEffect = EffectListService.GetEffectOnTarget(m_protectTarget, eEffect.Protect);
			if (protectTargetEffect != null)
				EffectService.RequestImmediateCancelEffect(protectTargetEffect);

			m_protectSource.Out.SendMessage(LanguageMgr.GetTranslation(m_protectSource.Client, "Effects.ProtectEffect.YouNoProtectY", m_protectTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			m_protectTarget.Out.SendMessage(LanguageMgr.GetTranslation(m_protectTarget.Client, "Effects.ProtectEffect.XNoProtectYou", m_protectSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			m_playerGroup = null;
		}

		/// <summary>
		/// Cancels guard if one of players disbands
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender">The group</param>
		/// <param name="args"></param>
		protected void GroupDisbandCallback(DOLEvent e, object sender, EventArgs args)
		{
			MemberDisbandedEventArgs eArgs = args as MemberDisbandedEventArgs;
			if (eArgs == null) return;
			if (eArgs.Member == ProtectTarget || eArgs.Member == ProtectSource)
			{
				Cancel(false);
			}
		}
	}
}
