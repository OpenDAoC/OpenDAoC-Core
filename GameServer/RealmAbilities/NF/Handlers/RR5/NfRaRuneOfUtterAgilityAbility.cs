using System.Collections.Generic;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class NfRaRuneOfUtterAgilityAbility : Rr5RealmAbility
{
	public NfRaRuneOfUtterAgilityAbility(DbAbility dba, int level) : base(dba, level) { }

	/// <summary>
	/// Action
	/// </summary>
	/// <param name="living"></param>
	public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;



		GamePlayer player = living as GamePlayer;
		if (player != null)
		{
			SendCasterSpellEffectAndCastMessage(player, 7074, true);
			NfRaRuneOfUtterAgilityEffect effect = new NfRaRuneOfUtterAgilityEffect();
			effect.Start(player);
		}
		DisableSkill(living);
	}

	public override int GetReUseDelay(int level)
	{
		return 600;
	}

	public override void AddEffectsInfo(IList<string> list)
	{
		list.Add("Runemaster gets a 90% chance to evade all melee attacks (regardless of direction) for 15 seconds.");
		list.Add("");
		list.Add("Target: Self");
		list.Add("Duration: 15 sec");
		list.Add("Casting time: instant");
	}
}