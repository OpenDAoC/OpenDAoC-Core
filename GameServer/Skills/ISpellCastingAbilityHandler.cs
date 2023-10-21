namespace Core.GS
{
	/// <summary>
	/// ISpellCastingAbilityHandler is an Interface for Providing Basic access to an Ability Casting a Spell
	/// </summary>
	public interface ISpellCastingAbilityHandler
	{
		Spell Spell { get; }
		SpellLine SpellLine { get; }
		Ability Ability { get; }
	}
}
