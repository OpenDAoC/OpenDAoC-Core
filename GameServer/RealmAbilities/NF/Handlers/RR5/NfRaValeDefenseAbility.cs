using System.Collections.Generic;
using Core.Database;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities
{
	public class NfRaValeDefenseAbility : Rr5RealmAbility
	{
		public NfRaValeDefenseAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			GamePlayer player = living as GamePlayer;

			if (player == null)
				return;

			Spell subspell = SkillBase.GetSpellByID(7063);
			ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(player, subspell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
			if(player.Group==null)
			{
				spellhandler.StartSpell(player);
			}
			else foreach (GamePlayer member in player.Group.GetPlayersInTheGroup())
			{
				if(member!=null) spellhandler.StartSpell(member);
			}
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 420;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			list.Add("Vale Defense.");
			list.Add("");
			list.Add("Target: Group");
			list.Add("Duration: 10 min");
			list.Add("Casting time: instant");
		}

	}
}
