using Core.GS.Effects;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("Climbing")]
public class ClimbingSpell : SpellHandler
{
	private GamePlayer gp;
	
	public override void OnEffectStart(GameSpellEffect effect)
	{
		gp = effect.Owner as GamePlayer;
		if (gp != null)
		{
			gp.AddAbility(SkillBase.GetAbility(AbilityConstants.Climbing));
			gp.Out.SendUpdatePlayerSkills();
		}
	}

	public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
	{
		gp = effect.Owner as GamePlayer;
		if (gp != null)
		{
			gp.RemoveAbility(AbilityConstants.Climbing);
			gp.Out.SendUpdatePlayerSkills();
		}
		return 0;
	}

	public ClimbingSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}