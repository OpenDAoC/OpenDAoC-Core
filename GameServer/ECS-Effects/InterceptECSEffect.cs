using DOL.AI.Brain;
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
    public class InterceptECSGameEffect : ECSGameAbilityEffect
    {
        public InterceptECSGameEffect(ECSGameEffectInitParams initParams, GameLiving interceptSource, GameLiving interceptTarget)
            : base(initParams)
        {
			m_interceptSource = interceptSource;
			m_interceptTarget = interceptTarget;
			EffectType = eEffect.Intercept;
			EffectService.RequestStartEffect(this);
		}

		/// <summary>
		/// Holds the interceptor
		/// </summary>
		private GameLiving m_interceptSource;

		/// <summary>
		/// Reference to gameplayer that is protecting this player with intercept
		/// </summary>
		private GameLiving m_interceptTarget;

		/// <summary>
		/// Holds the interceptor/intercepted group
		/// </summary>
		private Group m_group;

		/// <summary>
		/// Gets the interceptor
		/// </summary>
		public GameLiving InterceptSource
		{
			get { return m_interceptSource; }
		}

		/// <summary>
		/// Gets the intercepted
		/// </summary>
		public GameLiving InterceptTarget
		{
			get { return m_interceptTarget; }
		}

		/// <summary>
		/// chance to intercept
		/// </summary>
		public int InterceptChance
		{
			get
			{
				GamePet pet = InterceptSource as GamePet;
				if (pet == null) { return 50; }
				if (pet.Brain is BrittleBrain)
					return 100;
				else if (pet is BDSubPet)
					// Patch 1.123: The intercept chance on the Fossil Defender has been reduced by 20%.
					// Can't find documentation for previous intercept chance, so assuming 50%
					return 30;
				else if (pet != null)
					// Patch 1.125: Reduced the spirit warrior's intercept chance from 75% to 60% and intercept radius from 150 to 125
					return 60;
				else
					return 50;
			}
		}

		public override ushort Icon
		{
			get
			{
				//let's not display this icon on NPC's because i use this for spiritmasters
				if (Owner is GameNPC)
					return 7249;
				return 410;
			}
		}
        public override string Name {
			get
			{
				if (Owner is GamePlayer)
				{
					if (m_interceptSource != null && m_interceptTarget != null)
						return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.InterceptEffect.InterceptedByName", m_interceptTarget.GetName(0, false), m_interceptSource.GetName(0, false));
					return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.InterceptEffect.Name");
				}
				return "";
			}
		}
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
			if (InterceptSource is GamePlayer && InterceptTarget is GamePlayer)
			{
				m_group = ((GamePlayer)InterceptSource).Group;
				if (m_group == null) return;
				GameEventMgr.AddHandler(m_group, GroupEvent.MemberDisbanded, new DOLEventHandler(GroupDisbandCallback));
			}
			//m_interceptSource = interceptor;
			//m_owner = m_interceptSource;
			//m_interceptTarget = intercepted;

			if (Owner == InterceptSource)
			{
				if (!InterceptSource.IsWithinRadius(InterceptTarget, InterceptAbilityHandler.INTERCEPT_DISTANCE))
				{
					if (InterceptSource is GamePlayer)
						((GamePlayer)InterceptSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptSource).Client, "Effects.InterceptEffect.YouAttemtInterceptYBut", InterceptTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					if (InterceptTarget is GamePlayer && InterceptSource is GamePlayer)
						((GamePlayer)InterceptTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptTarget).Client, "Effects.InterceptEffect.XAttemtInterceptYouBut", InterceptSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				else
				{
					if (InterceptSource is GamePlayer)
						((GamePlayer)InterceptSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptSource).Client, "Effects.InterceptEffect.YouAttemtInterceptY", InterceptTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					if (InterceptTarget is GamePlayer && InterceptSource is GamePlayer)
						((GamePlayer)InterceptTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptTarget).Client, "Effects.InterceptEffect.XAttemptInterceptYou", InterceptSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
				new InterceptECSGameEffect(new ECSGameEffectInitParams(InterceptTarget, 0, 1), InterceptSource, InterceptTarget);
			}
		}
        public override void OnStopEffect()
        {

        }

		public void Cancel(bool playerCancel)
        {
			if (InterceptSource is GamePlayer && InterceptTarget is GamePlayer)
			{
				if (m_group != null)
					GameEventMgr.RemoveHandler(m_group, GroupEvent.MemberDisbanded, new DOLEventHandler(GroupDisbandCallback));
				m_group = null;
			}

			var interceptSourceEffect = EffectListService.GetEffectOnTarget(m_interceptSource, eEffect.Intercept);
			if (interceptSourceEffect != null)
				EffectService.RequestImmediateCancelEffect(interceptSourceEffect);
			var interceptTargetEffect = EffectListService.GetEffectOnTarget(m_interceptTarget, eEffect.Intercept);
			if (interceptTargetEffect != null)
				EffectService.RequestImmediateCancelEffect(interceptTargetEffect);

			if (playerCancel)
			{
				if (InterceptSource is GamePlayer)
					((GamePlayer)InterceptSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptSource).Client, "Effects.InterceptEffect.YouNoAttemtInterceptY", InterceptTarget.GetName(0, false)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				if (InterceptTarget is GamePlayer && InterceptSource is GamePlayer)
					((GamePlayer)InterceptTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptTarget).Client, "Effects.InterceptEffect.XNoAttemptInterceptYou", InterceptSource.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
		/// <summary>
		/// Cancels effect if interceptor or intercepted leaves the group
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender">The group</param>
		/// <param name="args"></param>
		protected void GroupDisbandCallback(DOLEvent e, object sender, EventArgs args)
		{
			MemberDisbandedEventArgs eArgs = args as MemberDisbandedEventArgs;
			if (eArgs == null) return;
			if (eArgs.Member == InterceptSource || eArgs.Member == InterceptTarget)
			{
				Cancel(false);
			}
		}
	}
}
