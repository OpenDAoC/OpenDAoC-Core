namespace DOL.GS.Spells
{
    [SpellHandler("PowerOverTime")]
    public class PotSpell : SpellHandler
    {
        /// <summary>
        /// Execute heal over time spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            // TODO: correct formula
            Effectiveness = 1.25;
            if (Caster is GamePlayer)
            {
                double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
                if (lineSpec < 1)
                    lineSpec = 1;
                Effectiveness = 0.75;
                if (Spell.Level > 0)
                {
                    Effectiveness += (lineSpec - 1.0) / Spell.Level * 0.5;
                    if (Effectiveness > 1.25)
                        Effectiveness = 1.25;
                }
            }

            base.ApplyEffectOnTarget(target);
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new GameSpellEffect(this, Spell.Duration, Spell.Frequency, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
            //"{0} seems calm and healthy."
            MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)),
                EChatType.CT_Spell, effect.Owner);
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);
            OnDirectEffect(effect.Owner);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target.InCombat) return;
            if (target.ObjectState != GameObject.eObjectState.Active) return;
            if (target.IsAlive == false) return;
            if (target is GamePlayer)
            {
                GamePlayer player = target as GamePlayer;
                if (player.PlayerClass.ID == (int)EPlayerClass.Vampiir
                    || player.PlayerClass.ID == (int)EPlayerClass.MaulerHib
                    || player.PlayerClass.ID == (int)EPlayerClass.MaulerMid
                    || player.PlayerClass.ID == (int)EPlayerClass.MaulerAlb)
                    return;
            }

            base.OnDirectEffect(target);
            double heal = Spell.Value * Effectiveness;
            if (heal < 0) target.Mana += (int)(-heal * target.MaxMana / 100);
            else target.Mana += (int)heal;
            //"You feel calm and healthy."
            MessageToLiving(target, Spell.Message1, EChatType.CT_Spell);
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            if (!noMessages)
            {
                //"Your meditative state fades."
                MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
                //"{0}'s meditative state fades."
                MessageUtil.SystemToArea(effect.Owner,
                    Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), EChatType.CT_SpellExpires,
                    effect.Owner);
            }

            return 0;
        }


        // constructor
        public PotSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    } 
}