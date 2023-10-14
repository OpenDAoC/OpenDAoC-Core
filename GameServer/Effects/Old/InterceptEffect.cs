using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.Language;

namespace DOL.GS.Effects
{
	public class InterceptEffect : StaticEffect, IGameEffect
	{
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
		private GroupUtil m_group;

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
				GameSummonedPet pet = InterceptSource as GameSummonedPet;
				if (pet == null) { return 0; }
				if (pet.Brain is BrittleGuardBrain)
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

		/// <summary>
		/// Start the intercepting on player
		/// </summary>
		/// <param name="interceptor">The interceptor</param>
		/// <param name="intercepted">The intercepted</param>
		public void Start(GameLiving interceptor, GameLiving intercepted)
		{
			if (interceptor is GamePlayer && intercepted is GamePlayer)
			{
				m_group = ((GamePlayer)interceptor).Group;
				if (m_group == null) return;
				GameEventMgr.AddHandler(m_group, GroupEvent.MemberDisbanded, new CoreEventHandler(GroupDisbandCallback));
			}
			m_interceptSource = interceptor;
			m_owner = m_interceptSource;
			m_interceptTarget = intercepted;

			if (!interceptor.IsWithinRadius(intercepted, InterceptAbilityHandler.INTERCEPT_DISTANCE))
			{
				if (interceptor is GamePlayer)
					((GamePlayer)interceptor).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)interceptor).Client, "Effects.InterceptEffect.YouAttemtInterceptYBut", intercepted.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				if (intercepted is GamePlayer && interceptor is GamePlayer)
					((GamePlayer)intercepted).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)intercepted).Client, "Effects.InterceptEffect.XAttemtInterceptYouBut", interceptor.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			else
			{
				if (interceptor is GamePlayer)
					((GamePlayer)interceptor).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)interceptor).Client, "Effects.InterceptEffect.YouAttemtInterceptY", intercepted.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				if (intercepted is GamePlayer && interceptor is GamePlayer)
					((GamePlayer)intercepted).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)intercepted).Client, "Effects.InterceptEffect.XAttemptInterceptYou", interceptor.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			interceptor.EffectList.Add(this);
			intercepted.EffectList.Add(this);
		}

		/// <summary>
		/// Called when effect must be canceled
		/// </summary>
		public override void Cancel(bool playerCancel)
		{
			if (InterceptSource is GamePlayer && InterceptTarget is GamePlayer)
			{
				GameEventMgr.RemoveHandler(m_group, GroupEvent.MemberDisbanded, new CoreEventHandler(GroupDisbandCallback));
				m_group = null;
			}
			InterceptSource.EffectList.Remove(this);
			InterceptTarget.EffectList.Remove(this);
			if (playerCancel)
			{
				if (InterceptSource is GamePlayer)
					((GamePlayer)InterceptSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptSource).Client, "Effects.InterceptEffect.YouNoAttemtInterceptY", InterceptTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				if (InterceptTarget is GamePlayer && InterceptSource is GamePlayer)
					((GamePlayer)InterceptTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)InterceptTarget).Client, "Effects.InterceptEffect.XNoAttemptInterceptYou", InterceptSource.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}

		/// <summary>
		/// Cancels effect if interceptor or intercepted leaves the group
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender">The group</param>
		/// <param name="args"></param>
		protected void GroupDisbandCallback(CoreEvent e, object sender, EventArgs args)
		{
			MemberDisbandedEventArgs eArgs = args as MemberDisbandedEventArgs;
			if (eArgs == null) return;
			if (eArgs.Member == InterceptSource || eArgs.Member == InterceptTarget)
			{
				Cancel(false);
			}
		}

		/// <summary>
		/// Name of the effect
		/// </summary>
		public override string Name
		{
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

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon
		{
			get
			{
				//let's not display this icon on NPC's because i use this for spiritmasters
				if (m_owner is GameNPC)
					return 7249;
				return 410;
			}
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var delveInfoList = new List<string>(4);
				delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.InterceptEffect.InfoEffect"));
				delveInfoList.Add(" ");
				delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.InterceptEffect.XInterceptingY", InterceptSource.GetName(0, true), InterceptTarget.GetName(0, false)));

				return delveInfoList;
			}
		}
	}
}
