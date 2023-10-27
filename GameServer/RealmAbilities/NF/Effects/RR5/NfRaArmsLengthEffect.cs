using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class NfRaArmsLengthEffect : TimedEffect
{
	/// <summary>
	/// Creates a new effect
	/// </summary>
	public NfRaArmsLengthEffect() : base(10000) { }

	/// <summary>
	/// Start the effect on player
	/// </summary>
	/// <param name="target">The effect target</param>
	public override void Start(GameLiving target)
	{
		base.Start(target);
		target.TempProperties.SetProperty("Charging", true);
		target.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, this, 2.5);
		if (target is GamePlayer)
		{
			((GamePlayer)target).Out.SendUpdateMaxSpeed();
		}
	}

	public override void Stop()
	{
		base.Stop();
		Owner.TempProperties.RemoveProperty("Charging");
		Owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, this);
		if (Owner is GamePlayer)
		{
			((GamePlayer)Owner).Out.SendUpdateMaxSpeed();
		}
	}

	/// <summary>
	/// Name of the effect
	/// </summary>
	public override string Name { get { return "Arms Length"; } }

	/// <summary>
	/// Icon to show on players, can be id
	/// </summary>
	public override ushort Icon { get { return 3057; } }

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo
	{
		get
		{
			var list = new List<string>();
			list.Add("Grants unbreakable extreme speed for 15 seconds.");
			list.AddRange(base.DelveInfo);
			return list;
		}
	}
}