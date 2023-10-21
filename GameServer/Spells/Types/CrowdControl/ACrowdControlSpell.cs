using System;
using Core.Events;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    /// <summary>
    /// Abstract CC spell handler
    /// </summary>
    public abstract class ACrowdControlSpell : ImmunityEffectSpellHandler
    {
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target.HasAbility(Abilities.CCImmunity))
            {
                MessageToCaster(target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);
                return;
            }

            if (target.EffectList.GetOfType<NfRaChargeEffect>() != null || target.TempProperties.GetProperty("Charging", false))
            {
                MessageToCaster(target.Name + " is moving too fast for this spell to have any effect!", EChatType.CT_SpellResisted);
                return;
            }

            base.ApplyEffectOnTarget(target);
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
            if (effect.Owner == null)
                return 0;

            base.OnEffectExpires(effect, noMessages);

            if (effect.Owner is GamePlayer player)
            {
                player.Client.Out.SendUpdateMaxSpeed();

                if (player.Group != null)
                    player.Group.UpdateMember(player, false, false);
            }

            effect.Owner.Notify(GameLivingEvent.CrowdControlExpired, effect.Owner);
            return (effect.Name == "Pet Stun") ? 0 : 60000;
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = base.CalculateEffectDuration(target, effectiveness);

            // [Atlas - Takii] Disabling MOC effectiveness scaling in OF.
            // double mocFactor = 1.0;
            // MasteryofConcentrationEffect moc = Caster.EffectList.GetOfType<MasteryofConcentrationEffect>();
            // if (moc != null)
            // {
            //     AtlasOF_MasteryofConcentration ra = Caster.GetAbility<AtlasOF_MasteryofConcentration>();
            //     if (ra != null)
            //         mocFactor = System.Math.Round((double)ra.GetAmountForLevel(ra.Level) / 100, 2);
            //     duration = (double)Math.Round(duration * mocFactor);
            // }

            if (Spell.SpellType != ESpellType.StyleStun)
            {
                // capping duration adjustment to 100%, live cap unknown - Tolakram
                int hitChance = Math.Min(200, CalculateToHitChance(target));

                if (hitChance <= 0)
                {
                    duration = 0;
                }
                else if (hitChance < 55)
                {
                    duration -= (int)(duration * (55 - hitChance) * 0.01);
                }
                else if (hitChance > 100)
                {
                    duration += (int)(duration * (hitChance - 100) * 0.01);
                }
            }

            return (int)duration;
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            int resistChance;

            /*
            GameSpellEffect fury = SpellHandler.FindEffectOnTarget(target, "Fury");
            if (fury != null)
            {
                resist += (int)fury.Spell.Value;
            }*/

            // Bonedancer RR5.
            if (target.EffectList.GetOfType<NfRaAllureOfDeathEffect>() != null)
                return NfRaAllureOfDeathEffect.ccchance;

            if (m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect)
                return 0;
            if (HasPositiveEffect)
                return 0;

            int hitchance = CalculateToHitChance(target);

            // Calculate the resist chance.
            resistChance = 100 - hitchance;

            if (resistChance > 100)
                resistChance = 100;

            // Use ResurrectHealth = 1 if the CC should not be resisted.
            if (Spell.ResurrectHealth == 1)
                resistChance = 0;
            else if (resistChance < 1)
                resistChance = 1;

            return resistChance;
        }

        public ACrowdControlSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
