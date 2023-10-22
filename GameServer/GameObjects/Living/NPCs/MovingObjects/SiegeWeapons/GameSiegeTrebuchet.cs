using Core.GS.Enums;
using Core.GS.Keeps;

namespace Core.GS;

public class GameSiegeTrebuchet : GameSiegeCatapult
{
	public GameSiegeTrebuchet()
		: base()
	{
		MeleeDamageType = EDamageType.Crush;
		Name = "trebuchet";
		AmmoType = 0x3A;
		EnableToMove = false;
		this.Model = 0xA2E;
		this.Effect = 0x89C;
		ActionDelay = new int[]
		{
			0,//none
			5000,//aiming
			15000,//arming
			0,//loading
			1000//fireing base delay
		};//en ms
		BaseDamage = 100;
		MinAttackRange = 2000;
		MaxAttackRange = 5000;
		AttackRadius = 150;
	}

	/// <summary>
	/// Calculates the damage based on the target type (door, siege, player)
	/// <summary>
	public override int CalcDamageToTarget(GameLiving target)
	{
		//Trebs are better against doors but lower damage against players/npcs/other objects
		if(target is GameKeepDoor || target is GameRelicDoor)
			return BaseDamage * 3;
		else
			return BaseDamage;
	}
}