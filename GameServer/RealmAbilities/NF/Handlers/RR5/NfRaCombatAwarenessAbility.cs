using System.Collections.Generic;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class NfRaCombatAwarenessAbility : Rr5RealmAbility
{
	public NfRaCombatAwarenessAbility(DbAbility dba, int level) : base(dba, level) { }

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
			SendCasterSpellEffectAndCastMessage(player, 7068, true);
			NfRaCombatAwarenessEffect effect = new NfRaCombatAwarenessEffect();
			effect.Start(player);
		}
		DisableSkill(living);
	}

	public override int GetReUseDelay(int level)
	{
		return 900;
	}

	public override void AddEffectsInfo(IList<string> list)
	{
		list.Add("Gives you a 50% 360� evade buff but also reduces your movement and melee damage by 50%");
		list.Add("");
		list.Add("Target: Self");
		list.Add("Duration: 30 sec");
		list.Add("Casting time: instant");
	}

}