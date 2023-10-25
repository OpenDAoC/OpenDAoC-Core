using System.Collections.Generic;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class NfRaBlindingDustAbility : Rr5RealmAbility
{
	public NfRaBlindingDustAbility(DbAbility dba, int level) : base(dba, level) { }

	/// <summary>
	/// Action
	/// </summary>
	/// <param name="living"></param>
	public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

		SendCasterSpellEffectAndCastMessage(living, 7040, true);

		bool deactivate = false;
		foreach (GamePlayer player in living.GetPlayersInRadius(350))
		{
			if (GameServer.ServerRules.IsAllowedToAttack(living, player, true))
			{
				DamageTarget(player, living);
				deactivate = true;
			}
		}

		foreach (GameNpc npc in living.GetNPCsInRadius(350))
		{
			if (GameServer.ServerRules.IsAllowedToAttack(living, npc, true))
			{
				DamageTarget(npc, living);
				deactivate = true;
			}
		}
		if (deactivate)
			DisableSkill(living);
	}

	private void DamageTarget(GameLiving target, GameLiving caster)
	{
		if (!target.IsAlive)
			return;
		if (target.EffectList.GetOfType<NfRaBlindingDustEffect>() == null)
		{
			NfRaBlindingDustEffect effect = new NfRaBlindingDustEffect();
			effect.Start(target);
		}

	}

	public override int GetReUseDelay(int level)
	{
		return 300;
	}

	public override void AddEffectsInfo(IList<string> list)
	{
		list.Add("Insta-cast PBAE Attack that causes the enemy to have a 25% chance to fumble melee/bow attacks for the next 15 seconds.");
		list.Add("");
		list.Add("Radius: 350");
		list.Add("Target: Enemy");
		list.Add("Duration: 15 sec");
		list.Add("Casting time: instant");
	}

}