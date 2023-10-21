using System;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS;

public class UnnaturalStorm : GameNpc
{
	public UnnaturalStorm() : base() { }
    #region Stats
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
	#endregion
	public override bool AddToWorld()
	{
		Name = "Unnatural Storm";
		Model = 665;
		Level = (byte)Util.Random(65, 70);
		Size = 100;
		MeleeDamageType = EDamageType.Crush;
		Race = 2003;
		Flags = (ENpcFlags)44;//notarget noname flying
		MaxSpeedBase = 0;
		RespawnInterval = -1;
		SpawnAdditionalStorms();

		UnnaturalStormBrain sbrain = new UnnaturalStormBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}

	protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellCastAnimation(this, 14323, 1);
				player.Out.SendSpellEffectAnimation(this, this, 3508, 0, false, 0x01);
			}

			return 3000;
		}

		return 0;
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
	private void SpawnAdditionalStorms()
    {
		foreach (GamePlayer player in ClientService.GetPlayersOfZone(CurrentZone))
			player.Out.SendMessage("An intense supernatural storm explodes in the sky over the northeastern expanse of Lyonesse!", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);

		for (int i = 0; i < Util.Random(4, 5); i++)
		{
			UnnaturalStormAdds Add = new UnnaturalStormAdds();
			Add.X = X + Util.Random(-1000, 1000);
			Add.Y = Y + Util.Random(-1000, 800);
			Add.Z = Z + Util.Random(-400, 400);
			Add.CurrentRegion = CurrentRegion;
			Add.Heading = Heading;
			Add.AddToWorld();
		}
	}
}


#region Additional Storm effect mobs
public class UnnaturalStormAdds : GameNpc
{
	public UnnaturalStormAdds() : base() { }
	public override bool AddToWorld()
	{
		Name = "Unnatural Storm";
		Model = 665;
		Level = (byte)Util.Random(40, 42);
		Size = 100;
		MeleeDamageType = EDamageType.Crush;
		Race = 2003;
		Flags = (ENpcFlags)60;//notarget noname flying
		MaxSpeedBase = 0;
		RespawnInterval = -1;

		UnnaturalStormAddsBrain sbrain = new UnnaturalStormAddsBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}

	private protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellCastAnimation(this, 14323, 1);
				player.Out.SendSpellEffectAnimation(this, this, 3508, 0, false, 0x01);
			}

			return 3000;
		}

		return 0;
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
}
#endregion

#region Unnatural Storm Controller - controll when storm will appear
public class UnnaturalStormController : GameNpc
{
	public UnnaturalStormController() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Unnatural Storm Controller";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)60;

		UnnaturalStormControllerBrain sbrain = new UnnaturalStormControllerBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion