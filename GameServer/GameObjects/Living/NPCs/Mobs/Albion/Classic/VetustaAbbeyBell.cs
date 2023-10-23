using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS;

public class VetustaAbbeyBell : GameNpc
{
	public VetustaAbbeyBell() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Vetusta Abbey Bell";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)60;

		VetustaAbbeyBellBrain sbrain = new VetustaAbbeyBellBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}