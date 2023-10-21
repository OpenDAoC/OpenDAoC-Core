using Core.AI.Brain;
using Core.GS.Effects;

namespace Core.GS.Spells.Atlantis
{
	[SpellHandler("AllStatsDebuff")]
	public class AllStatsDebuffSpell : SpellHandler
	{
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			effect.Owner.DebuffCategory[(int)EProperty.Dexterity] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Strength] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Constitution] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Acuity] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Piety] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Empathy] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Quickness] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Intelligence] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Charisma] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.ArmorAbsorption] += (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.MagicAbsorption] += (int)m_spell.Value;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumberance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
		}
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			effect.Owner.DebuffCategory[(int)EProperty.Dexterity] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Strength] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Constitution] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Acuity] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Piety] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Empathy] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Quickness] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Intelligence] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.Charisma] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.ArmorAbsorption] -= (int)m_spell.Value;
			effect.Owner.DebuffCategory[(int)EProperty.MagicAbsorption] -= (int)m_spell.Value;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumberance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
			return base.OnEffectExpires(effect, noMessages);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			if (target.Realm == 0 || Caster.Realm == 0)
			{
				target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
			}
			else
			{
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
			}
			if (target is GameNpc)
			{
				var aggroBrain = ((GameNpc)target).Brain as StandardMobBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
			}
		}
		public AllStatsDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
