using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS
{
	/// <summary>
	/// SpellLinePassiveAbility is a Specific Ability that Will trigger Self-Buff when activated based on Attached Spell Line
	/// Level Change should trigger cast of higher level spells, and cancel previous ones
	/// </summary>
	public class SpellLinePassiveAbility : SpellLineAbstractAbility
	{
		
		public override void Activate(GameLiving living, bool sendUpdates)
		{
			base.Activate(living, sendUpdates);
			
			var spell = Spell;
			var line = SpellLine;
			
			if (line != null && spell != null && spell.Target == eSpellTarget.SELF)
			{
				living.CastSpell(this);
			}
		}
		
		public override void OnLevelChange(int oldLevel, int newLevel = 0)
		{
			base.OnLevelChange(oldLevel, newLevel);
			
			// deactivate old spell and activate new one
			if (m_activeLiving != null)
			{
				var oldSpell = GetSpellForLevel(oldLevel);
				
				if (oldSpell != null)
				{
					var pulsing = m_activeLiving.FindPulsingSpellOnTarget(oldSpell);

					if (pulsing != null)
						pulsing.Cancel(false);
					
					var effect = m_activeLiving.FindEffectOnTarget(oldSpell);

					if (effect != null)
						effect.Cancel(false);
				}

				var spell = Spell;
				var line = SpellLine;

				if (line != null && spell != null && spell.Target == eSpellTarget.SELF)
				{
					m_activeLiving.CastSpell(this);
				}
			}
		}
		
		public override void Deactivate(GameLiving living, bool sendUpdates)
		{
			var spell = Spell;
			var line = SpellLine;

			// deactivate spell
			if (m_activeLiving != null && line != null && spell != null)
			{
					var pulsing = m_activeLiving.FindPulsingSpellOnTarget(spell);
					if (pulsing != null)
						pulsing.Cancel(false);
					
					var effect = m_activeLiving.FindEffectOnTarget(spell);
					if (effect != null)
						effect.Cancel(false);
			}

			base.Deactivate(living, sendUpdates);
		}
		
		public SpellLinePassiveAbility(DbAbility dba, int level)
			: base(dba, level)
		{
		}
	}
}
