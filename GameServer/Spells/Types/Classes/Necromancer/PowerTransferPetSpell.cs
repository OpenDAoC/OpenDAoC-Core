using Core.GS.AI;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("PowerTransferPet")]
class PowerTransferPetSpell : PowerTransferSpell
{
	public override void OnDirectEffect(GameLiving target)
	{
		if (!(Caster is NecromancerPet))
			return;
		base.OnDirectEffect(target);
	}
	
			/// <summary>
	/// Returns a reference to the shade.
	/// </summary>
	/// <returns></returns>
	protected override GamePlayer Owner()
	{
		if (!(Caster is NecromancerPet))
			return null;

		return (((Caster as NecromancerPet).Brain) as IControlledBrain).Owner as GamePlayer;
	}

	/// <summary>
    /// Create a new handler for the power transfer spell.
	/// </summary>
	/// <param name="caster"></param>
	/// <param name="spell"></param>
	/// <param name="line"></param>
	public PowerTransferPetSpell (GameLiving caster, Spell spell, SpellLine line) 
        : base(caster, spell, line) { }
}