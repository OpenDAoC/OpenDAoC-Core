using System.Collections.Generic;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Effects
{
	public class SureShotEffect : StaticEffect, IGameEffect
	{
		/// <summary>
		/// The effect owner
		/// </summary>
		GamePlayer m_player;

		/// <summary>
		/// Start the effect on player
		/// </summary>
		/// <param name="player">The effect target</param>
		public void Start(GamePlayer player)
		{
			m_player = player;
			m_player.EffectList.Add(this);
			m_player.Out.SendMessage(LanguageMgr.GetTranslation(m_player.Client, "Effects.SureShotEffect.YouSwitchToSSMode"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Called when effect must be canceled
		/// </summary>
		public override void Cancel(bool playerCancel) 
		{
			m_player.EffectList.Remove(this);
		}

		/// <summary>
		/// Name of the effect
		/// </summary>
		public override string Name { get { return LanguageMgr.GetTranslation(m_player.Client, "Effects.SureShotEffect.Name"); } }

		/// <summary>
		/// Remaining Time of the effect in seconds
		/// </summary>
		public override int RemainingTime { get { return 0; } }

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon { get { return 485; } }

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo { get { return new string[0]; } }
	}
}
