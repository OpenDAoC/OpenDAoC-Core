namespace DOL.GS.Spells
{
	/// <summary>
	/// Palading heal chant works only in combat
	/// </summary>
	[SpellHandler("CombatHeal")]
	public class CombatHealSpell : HealSpell
	{
		public CombatHealSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

		/// <summary>
		/// Execute heal spell
		/// </summary>
		/// <param name="target"></param>
		public override bool StartSpell(GameLiving target)
		{
			m_startReuseTimer = true;

			foreach (GameLiving member in GetGroupAndPets(Spell))
			{
				new CombatHealEcsSpellEffect(new EcsGameEffectInitParams(member, Spell.Frequency, Caster.Effectiveness, this));
			}

			GamePlayer player = Caster as GamePlayer;

			if (!Caster.InCombat && (player==null || player.Group==null || !player.Group.IsGroupInCombat()))
				return false; // Do not start healing if not in combat

			return base.StartSpell(target);
		}
	}
}
