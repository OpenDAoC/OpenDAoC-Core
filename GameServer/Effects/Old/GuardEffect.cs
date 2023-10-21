using System;
using System.Collections.Generic;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.SkillHandler;
using Core.Language;

namespace Core.GS.Effects
{
	public class GuardEffect : StaticEffect, IGameEffect
	{
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
		private GroupUtil m_playerGroup;

		/// <summary>
		/// Creates a new guard effect
		/// </summary>
		public GuardEffect()
		{
		}

		/// <summary>
		/// Start the guarding on player
		/// </summary>
		/// <param name="guardSource">The guarder</param>
		/// <param name="guardTarget">The player guarded by guarder</param>
		public void Start(GameLiving guardSource, GameLiving guardTarget)
		{
			if (guardSource == null || guardTarget == null)
				return;

			if (guardSource is GamePlayer && guardTarget is GamePlayer)
			{
				m_playerGroup = ((GamePlayer)guardSource).Group;
				if (m_playerGroup == null) return;
				if (m_playerGroup != guardTarget.Group)	return;
				GameEventMgr.AddHandler(m_playerGroup, GroupEvent.MemberDisbanded, new CoreEventHandler(GroupDisbandCallback));
			}

			m_guardSource = guardSource;
			m_guardTarget = guardTarget;
			m_owner = m_guardSource;

			if (!guardSource.IsWithinRadius(guardTarget, GuardAbilityHandler.GUARD_DISTANCE))
			{
				if(guardSource is GamePlayer)
					((GamePlayer)guardSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)guardSource).Client, "Effects.GuardEffect.YouAreNowGuardingYBut", guardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				if(guardSource is GamePlayer&&guardTarget is GamePlayer)
					((GamePlayer)guardTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)guardTarget).Client, "Effects.GuardEffect.XIsNowGuardingYouBut", guardSource.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			else
			{
				if(guardSource is GamePlayer)
					((GamePlayer)guardSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)guardSource).Client, "Effects.GuardEffect.YouAreNowGuardingY", guardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				if(guardSource is GamePlayer&&guardTarget is GamePlayer)
					((GamePlayer)guardTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)guardTarget).Client, "Effects.GuardEffect.XIsNowGuardingYou", guardSource.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			
			m_guardSource.EffectList.Add(this);
			m_guardTarget.EffectList.Add(this);
		}

		/// <summary>
		/// Cancels guard if one of players disbands
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender">The group</param>
		/// <param name="args"></param>
		protected void GroupDisbandCallback(CoreEvent e, object sender, EventArgs args)
		{
			MemberDisbandedEventArgs eArgs = args as MemberDisbandedEventArgs;
			if (eArgs == null) return;
			if (eArgs.Member == GuardTarget || eArgs.Member == GuardSource)
			{
				Cancel(false);
			}
		}

		/// <summary>
		/// Called when effect must be canceled
		/// </summary>
		public override void Cancel(bool playerCancel)
		{
			if(m_guardSource is GamePlayer && m_guardTarget is GamePlayer)
			{
				GameEventMgr.RemoveHandler(m_playerGroup, GroupEvent.MemberDisbanded, new CoreEventHandler(GroupDisbandCallback));
				m_playerGroup = null;
			}
			m_guardSource.EffectList.Remove(this);
			m_guardTarget.EffectList.Remove(this);

			if(m_guardSource is GamePlayer)
				((GamePlayer)m_guardSource).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)m_guardSource).Client, "Effects.GuardEffect.YourNoLongerGuardingY", m_guardTarget.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			if(m_guardSource is GamePlayer&&m_guardTarget is GamePlayer)
				((GamePlayer)m_guardTarget).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)m_guardTarget).Client, "Effects.GuardEffect.XNoLongerGuardingYoy", m_guardSource.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Name of the effect
		/// </summary>
		public override string Name
		{
			get
			{
				if(Owner is GamePlayer)
				{
					if (m_guardSource != null && m_guardTarget != null)
						return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.GuardEffect.GuardedByName", m_guardTarget.GetName(0, false), m_guardSource.GetName(0, false));
					return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.GuardEffect.Name");
				}
				return "";
			}
		}

		/// <summary>
		/// Remaining Time of the effect in milliseconds
		/// </summary>
		public override int RemainingTime
		{
			get { return 0; }
		}

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon
		{
			get
			{
				if (m_owner is GameNpc)
					return 1001;
				return 412;
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
				delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.GuardEffect.InfoEffect"));
				delveInfoList.Add(" ");
				delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.GuardEffect.XIsGuardingY", GuardSource.GetName(0, true), GuardTarget.GetName(0, false)));
				return delveInfoList;
			}
		}
	}
}
