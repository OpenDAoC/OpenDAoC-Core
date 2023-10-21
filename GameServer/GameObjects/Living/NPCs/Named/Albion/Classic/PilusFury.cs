using System;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS;

public class PilusFury : GameNpc
{
	public PilusFury() : base() { }
	#region Stats
	public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
	public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Pilus' Fury";
		Model = 665;
		Level = (byte)Util.Random(65, 70);
		Size = 50;
		Flags = (ENpcFlags)44;//notarget noname flying
		MaxSpeedBase = 0;
		RespawnInterval = -1;

		PilusFuryBrain sbrain = new PilusFuryBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	public override void StartAttack(GameObject target)
	{
	}
	public override bool IsVisibleToPlayers => true;
}