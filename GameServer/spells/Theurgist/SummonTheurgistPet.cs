/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Summon a theurgist pet.
	/// </summary>
	[SpellHandler("SummonTheurgistPet")]
	public class SummonTheurgistPet : SummonSpellHandler
	{
		private enum PetType
		{
			None,
			Earth,
			Ice,
			Air
		};

		private static string[] m_petTypeNames = Enum.GetNames(typeof(PetType));
		private PetType m_petType;

		public SummonTheurgistPet(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
			string spellName = m_spell.Name;

			// Deduce the pet type from the spell name.
			// It would be better to have a spell handler for each pet type instead.
			for (int i = 1; i < m_petTypeNames.Length; i++)
			{
				string petTypeName = m_petTypeNames[i];

				if (spellName.Contains(petTypeName, StringComparison.OrdinalIgnoreCase))
				{
					m_petType = (PetType)Enum.Parse(typeof(PetType), petTypeName);
					break;
				}
			}
		}

		/// <summary>
		/// Check whether it's possible to summon a pet.
		/// </summary>
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (Caster.PetCount >= ServerProperties.Properties.THEURGIST_PET_CAP)
			{
				MessageToCaster("You have too many controlled creatures!", eChatType.CT_SpellResisted);
				return false;
			}

			return base.CheckBeginCast(selectedTarget);
		}

		/// <summary>
		/// Summon the pet.
		/// </summary>
		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			base.ApplyEffectOnTarget(target, effectiveness);

			m_pet.TargetObject = target;
			(m_pet.Brain as IOldAggressiveBrain).AddToAggroList(target, 1);
			m_pet.Brain.Think();

			Caster.PetCount++;
		}

		/// <summary>
		/// Despawn the pet.
		/// </summary>
		/// <returns>Immunity timer (in milliseconds).</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (Caster.PetCount > 0)
				Caster.PetCount--;

			return base.OnEffectExpires(effect, noMessages);
		}

		protected override GameSummonedPet GetGamePet(INpcTemplate template)
		{
			switch (m_petType)
			{
				case PetType.Earth:
					return new TheurgistEarthPet(template);
				case PetType.Ice:
					return new TheurgistIcePet(template);
				case PetType.Air:
					return new TheurgistAirPet(template);
			}

			// Happens only if the name of the spell doesn't contains "earth", "ice", or "air".
			return new TheurgistPet(template);
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			switch (m_petType)
			{
				case PetType.Earth:
					return new TheurgistEarthPetBrain(owner);
				case PetType.Ice:
					return new TheurgistIcePetBrain(owner);
				case PetType.Air:
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
}
