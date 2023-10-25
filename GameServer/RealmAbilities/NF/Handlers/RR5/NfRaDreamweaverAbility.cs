using System.Collections.Generic;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class NfRaDreamweaverAbility : Rr5RealmAbility
{
	public NfRaDreamweaverAbility(DbAbility dba, int level) : base(dba, level) { }

	/// <summary>
	/// Action
	/// </summary>
	/// <param></param>
	public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

		GamePlayer player = living as GamePlayer;
		if (player != null)
		{
			SendCasterSpellEffectAndCastMessage(player, 7052, true);
			NfRaDreamweaverEffect effect = new NfRaDreamweaverEffect();
			effect.Start(player);
		}
		DisableSkill(living);
	}

	public override int GetReUseDelay(int level)
	{
		return 420;
	}

	public override void AddEffectsInfo(IList<string> list)
	{
		list.Add("Dreamweaver.");
		list.Add("");
		list.Add("Target: Self");
		list.Add("Duration: 5 min");
		list.Add("Casting time: instant");
	}
}