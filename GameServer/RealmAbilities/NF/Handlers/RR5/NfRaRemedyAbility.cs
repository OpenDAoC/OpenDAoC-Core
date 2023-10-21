namespace Core.GS.RealmAbilities;

[SkillHandler(Abilities.Remedy)]
public class NfRaRemedyAbility : IAbilityActionHandler
{
	/// <summary>
	/// Action
	/// </summary>
	/// <param name="living"></param>
    public void Execute(Ability ab, GamePlayer player)
	{
        if (!player.IsAlive || player.IsSitting || player.IsMezzed || player.IsStunned)
            return;

		if (player != null)
		{
            player.Out.SendSpellEffectAnimation(player, player, 7060, 0, false, 1);
            //SendCasterSpellEffectAndCastMessage(player, 7060, true);
			NfRaRemedyEffect effect = new NfRaRemedyEffect();
			effect.Start(player);

            player.DisableSkill(ab, 300 * 1000);
		}
	}

    /*
    public override void AddEffectsInfo(IList<string> list)
    {
        list.Add("Gives you immunity to weapon poisons for 1 minute. This spell wont purge already received poisons!");
        list.Add("This spell costs 10% of your HP. These will be regained by the end of the effect.");
        list.Add("");
        list.Add("Target: Self");
        list.Add("Duration: 60 sec");
        list.Add("Casting time: instant");
    }
     */ 
}