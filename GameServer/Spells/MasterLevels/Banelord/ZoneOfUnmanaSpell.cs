namespace Core.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Banelord
    //shared timer 1

    [SpellHandler("CastingSpeedDebuff")]
    public class ZoneOfUnmanaSpell : MasterlevelDebuffHandling
    {
        public override EProperty Property1
        {
            get { return EProperty.CastingSpeed; }
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);
            target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
        }

        // constructor
        public ZoneOfUnmanaSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}