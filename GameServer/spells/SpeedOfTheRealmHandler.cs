using DOL.GS.Effects;
using DOL.Database;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Increases the target's movement speed.
	/// </summary>
	[SpellHandler(eSpellType.SpeedOfTheRealm)]
	public class SpeedOfTheRealmHandler : SpeedEnhancementSpellHandler
	{
		private const ushort SECONDEFFECT = 2086;
		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			base.FinishSpellCast(target);
			foreach (GamePlayer player in Caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellCastAnimation(Caster, SECONDEFFECT, 20);
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			return Spell.Duration;
		}

        public override DbPlayerXEffect GetSavedEffect(GameSpellEffect e)
        {
            DbPlayerXEffect eff = new DbPlayerXEffect();
            eff.Var1 = Spell.ID;
            eff.Duration = e.RemainingTime;
            eff.IsHandler = true;
            eff.SpellLine = SpellLine.KeyName;
            return eff;
        }

        public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
		{
			OnEffectStart(effect);
		}

		public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
		{
			return OnEffectExpires(effect, false);
		}		

		/// <summary>
		/// The spell handler constructor
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="line"></param>
		public SpeedOfTheRealmHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
