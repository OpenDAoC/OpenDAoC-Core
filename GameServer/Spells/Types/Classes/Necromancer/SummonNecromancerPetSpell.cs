using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.RealmAbilities;

namespace Core.GS.Spells
{
    /// <summary>
    /// Spell handler to summon a necromancer pet.
    /// </summary>
    [SpellHandler("SummonNecroPet")]
    public class SummonNecromancerPetSpell : SummonSpellHandler
    {
        public SummonNecromancerPetSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// Check if caster is already in shade form.
        /// </summary>
        /// <param name="selectedTarget"></param>
        /// <returns></returns>
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (EffectListService.GetAbilityEffectOnTarget(Caster, EEffect.Shade) != null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.CheckBeginCast.ShadeEffectIsNotNull"), EChatType.CT_System);
                return false;
            }

            if (Caster is GamePlayer && Caster.ControlledBrain != null)
            {
                MessageToCaster(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "Summon.CheckBeginCast.AlreadyHaveaPet"), EChatType.CT_SpellResisted);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        /// <summary>
        /// Necromancer RR5 ability: Call of Darkness
        /// When active, the necromancer can summon a pet with only a 3 second cast time. 
        /// The effect remains active for 15 minutes, or until a pet is summoned.
        /// </summary>
        /// <returns></returns>
        public override int CalculateCastingTime()
        {
            return Caster.EffectList.GetOfType<NfRaCallOfDarknessEffect>() != null ? 3000 : base.CalculateCastingTime();
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);

            if (Caster is GamePlayer playerCaster)
                playerCaster.Shade(true);

            // Cancel RR5 Call of Darkness if on caster.
            FindStaticEffectOnTarget(Caster, typeof(NfRaCallOfDarknessEffect))?.Cancel(false);
        }

        /// <summary>
        /// Delve info string.
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                List<string> delve = new()
                {
                    LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Function"),
                    "",
                    LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Description"),
                    "",
                    LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Target", Spell.Target),
                    LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Power", Math.Abs(Spell.Power)),
                    LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.CastingTime", (Spell.CastTime / 1000).ToString("0.0## " + LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SpellHandler.DelveInfo.Sec")))
                };

                return delve;
            }
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new NecromancerPetBrain(owner);
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template)
        {
            return new NecromancerPet(template);
        }
    }
}
