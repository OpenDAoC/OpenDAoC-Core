
using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Pve Resurrection Illness
	/// </summary>
	[SpellHandler("PveResurrectionIllness")]
	public class PveResurrectionIllness : AbstractIllnessSpellHandler
	{
		public override void CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			GamePlayer targetPlayer = Target as GamePlayer;
			if (targetPlayer != null)
            {
                // Higher level rez spells reduce duration of rez sick.
                if (targetPlayer.TempProperties.getAllProperties().Contains(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS))
                {
					double rezSickEffectiveness = targetPlayer.TempProperties.getProperty<double>(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS);
                    targetPlayer.TempProperties.removeProperty(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS);
                    initParams.Duration = (int)(initParams.Duration * rezSickEffectiveness);
                }
                
                if (targetPlayer.GetModified(eProperty.ResIllnessReduction) > 0)
                {
	                initParams.Duration = initParams.Duration * (100-targetPlayer.GetModified(eProperty.ResIllnessReduction))/100;
                }
            }

			new ResurrectionIllnessEcsEffect(initParams);
		}

		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			//GamePlayer player = effect.Owner as GamePlayer;
			//if (player != null)
			//{
			//	player.Effectiveness -= Spell.Value * 0.01;
			//	player.Out.SendUpdateWeaponAndArmorStats();
			//	player.Out.SendStatusUpdate();
			//}
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
			//GamePlayer player = effect.Owner as GamePlayer;
			//if (player != null)
			//{
			//	player.Effectiveness += Spell.Value * 0.01;
			//	player.Out.SendUpdateWeaponAndArmorStats();
			//	player.Out.SendStatusUpdate();
			//}
			return 0;
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo 
		{
			get 
			{
				/*
				<Begin Info: Rusurrection Illness>
 
				The player's effectiveness is greatly reduced due to being recently resurrected.
 
				- Effectiveness penality: 50%
				- 4:56 remaining time
 
				<End Info>
				*/
				var list = new List<string>();

				list.Add(" "); //empty line
				list.Add(Spell.Description);
				list.Add(" "); //empty line
				list.Add("- Effectiveness penality: "+Spell.Value+"%");
				return list;
			}
		}

        /// <summary>
        /// Saves the effect when player quits
        /// </summary>        
        public override DbPlayerXEffects GetSavedEffect(GameSpellEffect e)
        {
            DbPlayerXEffects eff = new DbPlayerXEffects();
            eff.Var1 = Spell.ID;
            eff.Duration = e.RemainingTime;
            eff.IsHandler = true;
            eff.SpellLine = SpellLine.KeyName;
            return eff;
        }

        /// <summary>
        /// Restart the effects of resurrection illness
        /// </summary>        
        public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
		{
			OnEffectStart(effect);
		}

        /// <summary>
        /// Remove the effects of resurrection illness 
        /// </summary>        
		public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
		{
			return OnEffectExpires(effect, false);
		}		

		public PveResurrectionIllness(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}	
	}

	/// <summary>
	/// Contains all common code for illness spell handlers (and negative spell effects without animation) 
	/// </summary>
	public class AbstractIllnessSpellHandler : SpellHandler
	{
		public override bool HasPositiveEffect 
		{
			get 
			{ 
				return false;
			}
		}

		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			double modifier = 1.0;
			RealmAbilities.VeilRecoveryAbility ab = target.GetAbility<RealmAbilities.VeilRecoveryAbility>();
			if (ab != null)
				modifier -= ((double)ab.Amount / 100);

			return (int)((double)Spell.Duration * modifier); 
		}

		public AbstractIllnessSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	
	}
}
