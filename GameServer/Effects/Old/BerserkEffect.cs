using System.Collections.Generic;
using Core.GS.PacketHandler;
using Core.GS.SkillHandler;
using Core.Language;

namespace Core.GS.Effects.Old;

public class BerserkEffect : TimedEffect, IGameEffect
{
	protected ushort m_startModel = 0;
	/// <summary>
	/// Creates a new berserk effect
	/// </summary>
	public BerserkEffect()
		: base(BerserkAbilityHandler.DURATION)
	{
	}

	/// <summary>
	/// Start the berserk on a living
	/// </summary>
	public override void Start(GameLiving living)
	{
		base.Start(living);
		m_startModel = living.Model;

		if(living is GamePlayer)
		{
			(living as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((living as GamePlayer).Client, "Effects.BerserkEffect.GoBerserkerFrenzy"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			living.Emote(EEmote.MidgardFrenzy);
			//TODO differentiate model between Dwarves and other races
			if ((living as GamePlayer).Race == (int)ERace.Dwarf)
			{
				living.Model = 12;
			}
			else
			{
				living.Model = 3;
			}
		}
	}

	public override void Stop()
	{
		base.Stop();
		m_owner.Model = m_startModel;

		// there is no animation on end of the effect
		if(m_owner is GamePlayer)
			(m_owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((m_owner as GamePlayer).Client, "Effects.BerserkEffect.BerserkerFrenzyEnds"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	/// <summary>
	/// Name of the effect
	/// </summary>
	public override string Name
	{
		get
		{
			return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BerserkEffect.Name");
		}
	}

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo
	{
		get
		{
			var delveInfoList = new List<string>();
			delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.BerserkEffect.InfoEffect"));

			int seconds = RemainingTime / 1000;
			if(seconds > 0)
			{
				delveInfoList.Add(" "); //empty line
				if(seconds > 60)
					delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.DelveInfo.MinutesRemaining", (seconds / 60), ((seconds % 60).ToString("00"))));
				else
					delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.DelveInfo.SecondsRemaining", seconds));
			}

			return delveInfoList;
		}
	}
}