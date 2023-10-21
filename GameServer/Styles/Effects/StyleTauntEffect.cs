using System;
using Core.AI.Brain;

namespace Core.GS.Spells
{
	/// <summary>
	/// Style taunt effect spell handler
	/// </summary>
	[SpellHandler("StyleTaunt")]
	public class StyleTauntEffect : SpellHandler 
	{
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		/// <summary>
		/// Determines wether this spell is compatible with given spell
		/// and therefore overwritable by better versions
		/// spells that are overwritable cannot stack
		/// </summary>
		/// <param name="compare"></param>
		/// <returns></returns>
		public override bool IsOverwritable(EcsGameSpellEffect compare)
		{
            return false;
		}

        public override void OnDirectEffect(GameLiving target)
        {
            if (target is GameNpc)
            {
                AttackData ad = Caster.TempProperties.GetProperty<AttackData>(GameLiving.LAST_ATTACK_DATA, null);
                if (ad != null)
                {
                    IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
					if (aggroBrain != null)
					{
						int aggro = Convert.ToInt32(ad.Damage * Spell.Value);
						aggroBrain.AddToAggroList(Caster, aggro);

						//log.DebugFormat("Damage: {0}, Taunt Value: {1}, (de)Taunt Amount {2}", ad.Damage, Spell.Value, aggro.ToString());
					}
                }
            }
        }

		// constructor
        public StyleTauntEffect(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
