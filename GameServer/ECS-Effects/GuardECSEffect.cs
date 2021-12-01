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
    public class GuardECSGameEffect : ECSGameAbilityEffect
    {
        public GuardECSGameEffect(ECSGameEffectInitParams initParams, GameLiving guardSource, GameLiving guardTarget)
            : base(initParams) 
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
		public GameLiving GuardSource
		{
			get { return m_guardSource; }
		}

		/// <summary>
		/// Holds guarded player
		/// </summary>
		private GameLiving m_guardTarget;

		/// <summary>
		/// Gets guarded player
		/// </summary>
		public GameLiving GuardTarget
		{
			get { return m_guardTarget; }
		}

		/// <summary>
		/// Holds player group
		/// </summary>
		private Group m_playerGroup;

		public override ushort Icon 
		{ 
			get 
			{
				if (Owner is GameNPC)
					return 1001;
				return 412;
			} 
		}
        public override string Name
		{
			get
			{
				if (Owner is GamePlayer)
				{
					if (m_guardSource != null && m_guardTarget != null)
						return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.GuardEffect.GuardedByName", m_guardTarget.GetName(0, false), m_guardSource.GetName(0, false));
					return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.GuardEffect.Name");
				}
				return "";
			}
		}
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
			if (m_guardSource == null || m_guardTarget == null)
				return;

			if (GuardSource is GamePlayer && GuardTarget is GamePlayer)
			{
				m_playerGroup = ((GamePlayer)GuardSource).Group;
				if (m_playerGroup == null) return;
				if (m_playerGroup != GuardTarget.Group) return;
				GameEventMgr.AddHandler(m_playerGroup, GroupEvent.MemberDisbanded, new DOLEventHandler(GroupDisbandCallback));
			}

			if (Owner == GuardSource)
			{
				if (!GuardSource.IsWithinRadius(GuardTarget, GuardAbilityHandler.GUARD_DISTANCE))
				{
					if (GuardSource is GamePlayer)
						((GamePlayer)GuardSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)GuardSource).Client, "Effects.GuardEffect.YouAreNowGuardingYBut", GuardTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					if (GuardSource is GamePlayer && GuardTarget is GamePlayer)
						((GamePlayer)GuardTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)GuardTarget).Client, "Effects.GuardEffect.XIsNowGuardingYouBut", GuardSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
				{
					if (GuardSource is GamePlayer)
						((GamePlayer)GuardSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)GuardSource).Client, "Effects.GuardEffect.YouAreNowGuardingY", GuardTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					if (GuardSource is GamePlayer && GuardTarget is GamePlayer)
						((GamePlayer)GuardTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)GuardTarget).Client, "Effects.GuardEffect.XIsNowGuardingYou", GuardSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}


				new GuardECSGameEffect(new ECSGameEffectInitParams(GuardTarget, 0, 1, null), GuardSource, GuardTarget);
			}
		}
        public override void OnStopEffect()
        {

        }

		public void Cancel(bool playerCancel)
        {
			if (m_guardSource is GamePlayer && m_guardTarget is GamePlayer)
			{
				GameEventMgr.RemoveHandler(m_playerGroup, GroupEvent.MemberDisbanded, new DOLEventHandler(GroupDisbandCallback));
				m_playerGroup = null;
			}

			var guardSourceEffect = EffectListService.GetEffectOnTarget(m_guardSource, eEffect.Guard);
			if (guardSourceEffect != null)
				EffectService.RequestImmediateCancelEffect(guardSourceEffect);
			var guardTargetEffect = EffectListService.GetEffectOnTarget(m_guardTarget, eEffect.Guard);
			if (guardTargetEffect != null)
				EffectService.RequestImmediateCancelEffect(guardTargetEffect);

			if (m_guardSource is GamePlayer)
				((GamePlayer)m_guardSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)m_guardSource).Client, "Effects.GuardEffect.YourNoLongerGuardingY", m_guardTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			if (m_guardSource is GamePlayer && m_guardTarget is GamePlayer)
				((GamePlayer)m_guardTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)m_guardTarget).Client, "Effects.GuardEffect.XNoLongerGuardingYoy", m_guardSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
			if (eArgs.Member == GuardTarget || eArgs.Member == GuardSource)
			{
				Cancel(false);
			}
		}
	}
}
