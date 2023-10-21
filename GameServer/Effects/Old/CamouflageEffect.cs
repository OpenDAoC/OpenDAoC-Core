using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Effects.Old;

public class CamouflageEffect : StaticEffect, IGameEffect
{
	
	public override void Start(GameLiving target)
	{
		base.Start(target);
		if (target is GamePlayer)
			(target as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((target as GamePlayer).Client, "Effects.CamouflageEffect.YouAreCamouflaged"), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
	}

	public override void Stop()
	{
		base.Stop();
		if (m_owner is GamePlayer)
			(m_owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((m_owner as GamePlayer).Client, "Effects.CamouflageEffect.YourCFIsGone"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}
	
	public override string Name
	{
		get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.CamouflageEffect.Name"); }
	}
	
	public override ushort Icon
	{
		get { return 476; }
	}
	
	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo
	{
		get
		{
			var delveInfoList = new List<string>();
			delveInfoList.Add(LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.CamouflageEffect.InfoEffect"));

			return delveInfoList;
		}
	}
}