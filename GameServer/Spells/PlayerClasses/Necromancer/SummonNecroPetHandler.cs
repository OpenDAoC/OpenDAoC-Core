using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.RealmAbilities;
using DOL.Language;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell handler to summon a necromancer pet.
	/// </summary>
	/// <author>Aredhel</author>
	[SpellHandler("SummonNecroPet")]
	public class SummonNecroPetHandler : SummonHandler
	{
		public SummonNecroPetHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		private int m_summonConBonus;
		private int m_summonHitsBonus;

		/// <summary>
		/// Note bonus constitution and bonus hits from items and RAs.
		/// </summary>
		public void SetConAndHitsBonus()
		{
			// Check current item bonuses for constitution and hits (including cap increases) of the caster.
			// Bonus from Aug.Con is applied as well. Thoughness is applied later on.
			int hitsCap = MaxHealthCalculator.GetItemBonusCap(Caster) + MaxHealthCalculator.GetItemBonusCapIncrease(Caster);
			int conFromRa = OfRaHelpers.GetStatEnhancerAmountForLevel(Caster is GamePlayer playerOwner  ? OfRaHelpers.GetAugConLevel(playerOwner) : 0);

			m_summonConBonus = Caster.GetModifiedFromItems(EProperty.Constitution) + conFromRa;
			m_summonHitsBonus = Math.Min(Caster.ItemBonus[(int)EProperty.MaxHealth], hitsCap);
		}

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
			if (Caster.EffectList.GetOfType<NfRaCallOfDarknessEffect>() != null)
				return 3000;

			return base.CalculateCastingTime();
		}

		/// <summary>
		/// Create the pet and transfer stats.
		/// </summary>
		/// <param name="target">Target that gets the effect</param>
		/// <param name="effectiveness">Factor from 0..1 (0%-100%)</param>
		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			base.ApplyEffectOnTarget(target, effectiveness);

			if (Caster is GamePlayer)
				(Caster as GamePlayer).Shade(true);

			// Cancel RR5 Call of Darkness if on caster.

			IGameEffect callOfDarkness = FindStaticEffectOnTarget(Caster, typeof(NfRaCallOfDarknessEffect));
			if (callOfDarkness != null)
				callOfDarkness.Cancel(false);
		}

		/// <summary>
		/// Delve info string.
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var delve = new List<string>();
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Function"));
				delve.Add("");
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Description"));
				delve.Add("");
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Target", Spell.Target));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.Power", Math.Abs(Spell.Power)));
                delve.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SummonNecromancerPet.DelveInfo.CastingTime", (Spell.CastTime / 1000).ToString("0.0## " + LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SpellHandler.DelveInfo.Sec"))));
				return delve;
			}
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new NecroPetBrain(owner);
		}

		protected override GameSummonedPet GetGamePet(INpcTemplate template)
		{
			return new NecromancerPet(template, m_summonConBonus, m_summonHitsBonus);
		}
	}
}
