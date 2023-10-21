using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.RealmAbilities;

namespace Core.GS.Spells
{
	//no shared timer

	[SpellHandler("SummonMastery")]
	public class SummonMasterySpell : MasterLevelSpellHandling
		//public class Convoker9Handler : MasterlevelBuffHandling
	{
		private GameNpc m_living;
		private GamePlayer m_player;

		//public override eProperty Property1 { get { return eProperty.MeleeDamage; } }

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			foreach (NfRaJuggernautEffect jg in target.EffectList.GetAllOfType<NfRaJuggernautEffect>())
			{
				if (jg != null)
				{
					MessageToCaster("Your Pet already has an ability of this type active", EChatType.CT_SpellResisted);
					return;
				}
			}

			// Add byNefa 04.02.2011 13:35
			// Check if Necro try to use ML9 Convoker at own Pet
			if (m_player != null && m_player.PlayerClass.ID == (int)EPlayerClass.Necromancer)
			{
				// Caster is a Necro
				NecromancerPet necroPet = target as NecromancerPet;
				if (necroPet == null || necroPet.Owner == m_player)
				{
					// Caster is a Nekro and his Target is his Own Pet
					MessageToCaster("You cant use this ability on your own Pet", EChatType.CT_SpellResisted);
					return;
				}
			}

			base.ApplyEffectOnTarget(target);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			m_living = m_player.ControlledBrain.Body;
			m_living.Level += 20;
			m_living.BaseBuffBonusCategory[(int)EProperty.MeleeDamage] += 275;
			m_living.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] += 75;
			m_living.Size += 40;
			base.OnEffectStart(effect);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			m_living.Level -= 20;
			m_living.BaseBuffBonusCategory[(int)EProperty.MeleeDamage] -= 275;
			m_living.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] -= 75;
			m_living.Size -= 40;
			return base.OnEffectExpires(effect, noMessages);
		}

		public SummonMasterySpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
			m_player = caster as GamePlayer;
		}
	}
}