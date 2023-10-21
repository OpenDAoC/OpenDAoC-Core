using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	[SpellHandler("HereticDamageOverTime")]
	public class HereticDotSpell : HereticPiercingMagicSpell
	{

		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		
		public override double GetLevelModFactor()
		{
			return 0;
		}


		public override bool IsOverwritable(EcsGameSpellEffect compare)
		{
			if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;
			if (base.IsOverwritable(compare) == false) return false;
			if (compare.SpellHandler.Spell.Duration != Spell.Duration) return false;
			return true;
		}


		public override AttackData CalculateDamageToTarget(GameLiving target)
		{
			AttackData ad = base.CalculateDamageToTarget(target);
			ad.CriticalDamage = 0;
			return ad;
		}


		public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			int speclevel = 1;
			if (m_caster is GamePlayer) 
			{
				speclevel = ((GamePlayer)m_caster).GetModifiedSpecLevel(m_spellLine.Spec);
			}
			min = 1;
			max = 1;

			if (target.Level>0) {
				min = 0.5 + (speclevel-1) / (double)target.Level * 0.5;
			}

			if (speclevel-1 > target.Level) {
				double overspecBonus = (speclevel-1 - target.Level) * 0.005;
				min += overspecBonus;
				max += overspecBonus;
			}

			if (min > max) min = max;
			if (min < 0) min = 0;
		}
		
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
		}


		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			// damage is not reduced with distance
			//return new GameSpellEffect(this, m_spell.Duration*10-1, m_spellLine.IsBaseLine ? 3000 : 2000, 1);
			return new GameSpellEffect(this, m_spell.Duration, m_spellLine.IsBaseLine ? 3000 : 2000, 1);
		}

		
		public override void OnEffectStart(GameSpellEffect effect)
		{			
			SendEffectAnimation(effect.Owner, 0, false, 1);
		}

		
		public override void OnEffectPulse(GameSpellEffect effect)
		{
            if ( !m_caster.IsAlive || !effect.Owner.IsAlive || m_caster.Mana < Spell.PulsePower || !m_caster.IsWithinRadius( effect.Owner, (int)( Spell.Range * m_caster.GetModified( EProperty.SpellRange ) * 0.01 ) ) || m_caster.IsMezzed || m_caster.IsStunned || ( m_caster.TargetObject is GameLiving ? effect.Owner != m_caster.TargetObject as GameLiving : true ) )
			{
				effect.Cancel(false);
				return;
			}
			base.OnEffectPulse(effect);
			SendEffectAnimation(effect.Owner, 0, false, 1);
			// An acidic cloud surrounds you!
			MessageToLiving(effect.Owner, Spell.Message1, EChatType.CT_Spell);
			// {0} is surrounded by an acidic cloud!
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), EChatType.CT_YouHit, effect.Owner);
			OnDirectEffect(effect.Owner);
		}


		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			base.OnEffectExpires(effect, noMessages);
			if (!noMessages) {
				// The acidic mist around you dissipates.
				MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
				// The acidic mist around {0} dissipates.
				MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), EChatType.CT_SpellExpires, effect.Owner);
			}
			return 0;
		}

		
		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null) return;
			if (!target.IsAlive || target.ObjectState!=GameLiving.eObjectState.Active) return;

			// no interrupts on DoT direct effect
			// calc damage
			AttackData ad = CalculateDamageToTarget(target);
			SendDamageMessages(ad);
			DamageTarget(ad);
		}

	
		public virtual void DamageTarget(AttackData ad)
		{
			ad.AttackResult = EAttackResult.HitUnstyled;
			ad.Target.OnAttackedByEnemy(ad);
			ad.Attacker.DealDamage(ad);
			foreach(GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE)) {
				player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
			}
		}

	
		public HereticDotSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
