using System;
using Core.Events;
using Core.GS.Effects;

namespace Core.GS.Spells
{
	[SpellHandler("RangeShield")]
	public class RangeShieldSpell : BladeturnSpell 
	{
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttacked));
            return base.OnEffectExpires(effect, noMessages);
        }
        protected virtual void OnAttacked(CoreEvent e, object sender, EventArgs arguments)
        {
            AttackedByEnemyEventArgs attackArgs = arguments as AttackedByEnemyEventArgs;
            GameLiving living = sender as GameLiving;
            if (attackArgs == null) return;
            if (living == null) return;
            double value = 0;
            switch (attackArgs.AttackData.AttackType)
            {
                case EAttackType.Ranged:
                    value = Spell.Value * .01;
                    attackArgs.AttackData.Damage *= (int)value;
                    break;
                case EAttackType.Spell:
                    if (attackArgs.AttackData.SpellHandler.Spell.SpellType == ESpellType.Archery)
                    {
                        value = Spell.Value * .01;
                        attackArgs.AttackData.Damage *= (int)value;
                    }
                    break;
            }
        }
		public RangeShieldSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
