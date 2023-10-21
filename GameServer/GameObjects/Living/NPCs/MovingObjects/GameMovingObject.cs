using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS;

/// <summary>
/// GameMovingObject is a base class for boats and siege weapons.
/// </summary>		
public abstract class GameMovingObject : GameNpc
{
	public GameMovingObject() : base()
	{
	}

	public virtual ushort Type()
	{
		return 2;
	}

	private ushort m_emblem;
	public ushort Emblem
	{
		get { return m_emblem; }
		set { m_emblem = value; }
	}

	public override ushort Model
	{
		get
		{
			return base.Model;
		}
		set
		{
			base.Model = value;
			if(ObjectState==eObjectState.Active)
				ClientService.UpdateObjectForPlayers(this);
		}
	}
	public override bool IsWorthReward
	{
		get {return false;}
	}
	/// <summary>
	/// This methode is override to remove XP system
	/// </summary>
	/// <param name="source">the damage source</param>
	/// <param name="damageType">the damage type</param>
	/// <param name="damageAmount">the amount of damage</param>
	/// <param name="criticalAmount">the amount of critical damage</param>
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		//Work around the XP system
		if (IsAlive)
		{
			Health -= (damageAmount + criticalAmount);
			if (!IsAlive)
			{
				Health = 0;
				Die(source);
			}
		}
	}
	/// <summary>
	/// Starts the power regeneration
	/// </summary>
	public override void StartPowerRegeneration()
	{
		//No regeneration for moving objects
		return;
	}
	/// <summary>
	/// Starts the endurance regeneration
	/// </summary>
	public override void StartEnduranceRegeneration()
	{
		//No regeneration for moving objects
		return;
	}
}