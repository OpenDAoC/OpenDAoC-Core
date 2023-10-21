using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class NfRaTrueshotEffect : StaticEffect, IGameEffect
{
	public NfRaTrueshotEffect()
		: base()
	{
	}

	public override void Start(GameLiving target)
	{
		base.Start(target);
		GamePlayer player = target as GamePlayer;
		if (player != null)
		{
			player.Out.SendMessage("You prepare a Trueshot!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
	}

	public override string Name { get { return "Trueshot"; } }

	public override ushort Icon { get { return 3004; } }

	public override IList<string> DelveInfo
	{
		get
		{
			var list = new List<string>();
			list.Add("Grants 50% bonus to the next arrow fired. The arrow will penetrate and pop bladeturn.");
			return list;
		}
	}
}