using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Effects.Old;

public class RapidFireEffect : StaticEffect, IGameEffect
{
	/// <summary>
	/// Start the effect on player
	/// </summary>
	/// <param name="living">The effect target</param>
	public override void Start(GameLiving living)
	{
		base.Start(living);
		if (living is GamePlayer)
			(living as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((living as GamePlayer).Client, "Effects.RapidFireEffect.YouSwitchRFMode"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	/// <summary>
	/// Name of the effect
	/// </summary>
	public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.RapidFireEffect.Name"); } }

	/// <summary>
	/// Icon to show on players, can be id
	/// </summary>
	public override ushort Icon { get { return 484; } }
}