using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain;

/// <summary>
/// A brain that can be controlled
/// </summary>
public class ArcherSubPetBrain : SubPetBrain
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs new controlled npc brain
	/// </summary>
	/// <param name="owner"></param>
	public ArcherSubPetBrain(GameLiving owner) : base(owner) { }

	#region AI

	/// <summary>
	/// No Abilities or spells
	/// </summary>
	public override void CheckAbilities() { }
	protected override bool CheckDefensiveSpells(Spell spell) { return false; }
	protected override bool CheckOffensiveSpells(Spell spell) { return false; }
	protected override bool CheckInstantSpells(Spell spell) { return false; }

	public override void Attack(GameObject target)
	{
		if (m_orderAttackTarget != target)
			Body.SwitchWeapon(eActiveWeaponSlot.Distance);

		base.Attack(target);
	}

	#endregion
}