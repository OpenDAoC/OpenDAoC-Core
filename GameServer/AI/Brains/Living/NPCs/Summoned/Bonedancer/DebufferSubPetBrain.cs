using System.Reflection;
using Core.GS.Spells;
using log4net;

namespace Core.GS.AI;

public class DebufferSubPetBrain : SubPetBrain
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs new controlled npc brain
	/// </summary>
	/// <param name="owner"></param>
	public DebufferSubPetBrain(GameLiving owner) : base(owner) { }

	#region AI

	/// <summary>
	/// Checks the Abilities
	/// </summary>
	public override void CheckAbilities() { }

	/// <summary>
	/// Checks the Positive Spells.  Handles buffs, heals, etc.
	/// </summary>
	protected override bool CheckDefensiveSpells(Spell spell) { return false; }

	#endregion
}