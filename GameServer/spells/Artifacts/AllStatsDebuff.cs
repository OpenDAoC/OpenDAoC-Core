using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS.Spells.Atlantis
{
	/// <summary>
	/// All stats debuff spell handler
	/// </summary>
	[SpellHandler(eSpellType.AllStatsDebuff)]
	public class AllStatsDebuff : SpellHandler
	{
		public override string ShortDescription => $"Decreases the target's stats by {Spell.Value}.";

		public AllStatsDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			effect.Owner.DebuffCategory[eProperty.Dexterity] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Strength] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Constitution] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Acuity] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Piety] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Empathy] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Quickness] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Intelligence] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Charisma] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.ArmorAbsorption] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.MagicAbsorption] += (int)m_spell.Value;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumbrance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
		}
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			effect.Owner.DebuffCategory[eProperty.Dexterity] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Strength] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Constitution] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Acuity] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Piety] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Empathy] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Quickness] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Intelligence] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.Charisma] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.ArmorAbsorption] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[eProperty.MagicAbsorption] -= (int)m_spell.Value;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumbrance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
			return base.OnEffectExpires(effect, noMessages);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);

			if (target is GameNPC)
			{
				var aggroBrain = ((GameNPC)target).Brain as StandardMobBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
			}
		}
	}
}
