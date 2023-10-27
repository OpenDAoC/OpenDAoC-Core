using System.Reflection;
using Core.GS.Enums;
using Core.GS.Spells;
using log4net;

namespace Core.GS.AI;

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
			Body.SwitchWeapon(EActiveWeaponSlot.Distance);

		base.Attack(target);
	}

	#endregion
}