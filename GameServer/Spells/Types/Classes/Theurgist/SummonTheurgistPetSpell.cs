using System;
using Core.GS.AI.Brains;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells;

[SpellHandler("SummonTheurgistPet")]
public class SummonTheurgistPetSpell : SummonSpellHandler
{
	private static string[] m_petTypeNames = Enum.GetNames(typeof(ETheurgistPetType));
	private ETheurgistPetType m_petType;

	public SummonTheurgistPetSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
	{
		string spellName = m_spell.Name;

		// Deduce the pet type from the spell name.
		// It would be better to have a spell handler for each pet type instead.
		for (int i = 1; i < m_petTypeNames.Length; i++)
		{
			string petTypeName = m_petTypeNames[i];

			if (spellName.Contains(petTypeName, StringComparison.OrdinalIgnoreCase))
			{
				m_petType = (ETheurgistPetType)Enum.Parse(typeof(ETheurgistPetType), petTypeName);
				break;
			}
		}
	}

	/// <summary>
	/// Check whether it's possible to summon a pet.
	/// </summary>
	public override bool CheckBeginCast(GameLiving selectedTarget)
	{
		if (Caster.PetCount >= ServerProperty.THEURGIST_PET_CAP)
		{
			MessageToCaster("You have too many controlled creatures!", EChatType.CT_SpellResisted);
			return false;
		}

		return base.CheckBeginCast(selectedTarget);
	}

	/// <summary>
	/// Summon the pet.
	/// </summary>
	public override void ApplyEffectOnTarget(GameLiving target)
	{
		base.ApplyEffectOnTarget(target);

		m_pet.TargetObject = target;
		(m_pet.Brain as IOldAggressiveBrain).AddToAggroList(target, 1);
		m_pet.Brain.Think();
		Caster.UpdatePetCount(true);
	}

	/// <summary>
	/// Despawn the pet.
	/// </summary>
	/// <returns>Immunity timer (in milliseconds).</returns>
	public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
	{
		Caster.UpdatePetCount(false);
		return base.OnEffectExpires(effect, noMessages);
	}

	protected override GameSummonedPet GetGamePet(INpcTemplate template)
	{
		switch (m_petType)
		{
			case ETheurgistPetType.Earth:
				return new TheurgistEarthPet(template);
			case ETheurgistPetType.Ice:
				return new TheurgistIcePet(template);
			case ETheurgistPetType.Air:
				return new TheurgistAirPet(template);
		}

		// Happens only if the name of the spell doesn't contains "earth", "ice", or "air".
		return new TheurgistPet(template);
	}

	protected override IControlledBrain GetPetBrain(GameLiving owner)
	{
		switch (m_petType)
		{
			case ETheurgistPetType.Earth:
				return new TheurgistEarthPetBrain(owner);
			case ETheurgistPetType.Ice:
				return new TheurgistIcePetBrain(owner);
			case ETheurgistPetType.Air:
				return new TheurgistAirPetBrain(owner);
		}

		// Happens only if the name of the spell doesn't contains "earth", "ice", or "air".
		return new TheurgistPetBrain(owner);
	}

	protected override void SetBrainToOwner(IControlledBrain brain) { }

	protected override void GetPetLocation(out int x, out int y, out int z, out ushort heading, out Region region)
	{
		base.GetPetLocation(out x, out y, out z, out _, out region);
		heading = Caster.Heading;
	}
}