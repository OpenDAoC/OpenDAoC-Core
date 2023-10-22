using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells;

[SpellHandler("SpeedOfTheRealm")]
public class SpeedOfTheRealmSpell : SpeedEnhancementSpell
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

	protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
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
	public SpeedOfTheRealmSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}