using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.RealmAbilities;

public class NfRaVehementRenewalAbility : TimedRealmAbility
{
	public NfRaVehementRenewalAbility(DbAbility dba, int level) : base(dba, level) { }

	/// <summary>
	/// Action
	/// </summary>
	/// <param name="living"></param>
	public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED | NOTINGROUP)) return;

		int heal = 0;
		
		if(ServerProperty.USE_NEW_ACTIVES_RAS_SCALING)
		{
			switch (Level)
			{
				case 1: heal = 375; break;
				case 2: heal = 525; break;
				case 3: heal = 750; break;
				case 4: heal = 1125; break;
				case 5: heal = 1500; break;
			}
		}
		else
		{
			switch (Level)
			{
				case 1: heal = 375; break;
				case 2: heal = 750; break;
				case 3: heal = 1500; break;
			}
		}

		bool used = false;

		GamePlayer player = living as GamePlayer;
		if (player != null)
		{
			if (player.Group == null)
			{
				player.Out.SendMessage("You are not in a group.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			SendCasterSpellEffectAndCastMessage(living, 7017, true);

			foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
			{
				if (p == player) continue;
				if (!player.IsWithinRadius( p, 2000 )) continue;
				if (!p.IsAlive) continue;
				int healed = p.ChangeHealth(living, EHealthChangeType.Spell, heal);
				if (healed > 0)
					used = true;

				if (healed > 0) p.Out.SendMessage(player.Name + " heals your for " + healed + " hit points.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				if (heal > healed)
				{
					p.Out.SendMessage("You are fully healed.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				}
			}
		}
		if (used) DisableSkill(living);
	}

	public override int GetReUseDelay(int level)
	{
		return 600;
	}

	public override void AddEffectsInfo(IList<string> list)
	{
		if(ServerProperty.USE_NEW_ACTIVES_RAS_SCALING)
		{
			list.Add("Level 1: Value: 375");
			list.Add("Level 2: Value: 525");
			list.Add("Level 3: Value: 750");
			list.Add("Level 4: Value: 1125");
			list.Add("Level 5: Value: 1500");
			list.Add("");
			list.Add("Target: Group");
			list.Add("Casting time: instant");
		}
		else
		{
			list.Add("Level 1: Value: 375");
			list.Add("Level 2: Value: 750");
			list.Add("Level 3: Value: 1500");
			list.Add("");
			list.Add("Target: Group");
			list.Add("Casting time: instant");
		}
	}
}