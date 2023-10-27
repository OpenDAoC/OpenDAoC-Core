using System.Collections.Generic;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class NfRaRestorativeMendAbility : Rr5RealmAbility
{
	public NfRaRestorativeMendAbility(DbAbility dba, int level) : base(dba, level) { }

	/// <summary>
	/// Action
	/// </summary>
	/// <param name="living"></param>
	public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;



		bool deactivate = false;

		GamePlayer player = living as GamePlayer;
		if (player != null)
		{
			if (player.Group != null)
			{
				SendCasterSpellEffectAndCastMessage(living, 7071, true);
				foreach (GamePlayer member in player.Group.GetPlayersInTheGroup())
				{
					NfRaRestorativeMendEffect aog = member.EffectList.GetOfType<NfRaRestorativeMendEffect>();
					if (!CheckPreconditions(member, DEAD) && aog == null
						&& living.IsWithinRadius( member, 2000 ))
					{
						NfRaRestorativeMendEffect effect = new NfRaRestorativeMendEffect();
						effect.Start(member);
						deactivate = true;
					}
				}
			}
			else
			{
				NfRaRestorativeMendEffect aog = player.EffectList.GetOfType<NfRaRestorativeMendEffect>();
				if (!CheckPreconditions(player, DEAD) && aog == null)
				{
					NfRaRestorativeMendEffect effect = new NfRaRestorativeMendEffect();
					effect.Start(player);
					deactivate = true;
				}
			}
		}
		if (deactivate)
			DisableSkill(living);
	}

	public override int GetReUseDelay(int level)
	{
		return 600;
	}


	public override void AddEffectsInfo(IList<string> list)
	{
		list.Add("Group Frigg that heals health, power, and endurance over 30 seconds for a total of 50%. (5% is granted every 3 seconds regardless of combat state)");
		list.Add("");
		list.Add("Target: Group");
		list.Add("Duration: 30 sec");
		list.Add("Casting time: instant");
	}
}