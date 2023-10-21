using System.Collections;
using System.Collections.Generic;
using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
    public class NfRaMarkOfPreyAbility : Rr5RealmAbility
	{
		public NfRaMarkOfPreyAbility(DbAbility dba, int level) : base(dba, level) { }

        int RANGE = 1000;
        public const int DURATION = 30 * 1000;
        public const double VALUE = 5.1;

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			GamePlayer player = living as GamePlayer;
			if (player == null) return;

			ArrayList targets = new ArrayList();
			if (player.Group == null)
				targets.Add(player);
			else
				foreach (GamePlayer grpMate in player.Group.GetPlayersInTheGroup())
					if (player.IsWithinRadius( grpMate, RANGE ) && grpMate.IsAlive)
						targets.Add(grpMate);

			foreach (GamePlayer target in targets)
			{
				NfRaMarkOfPreyEffect MarkOfPrey = target.EffectList.GetOfType<NfRaMarkOfPreyEffect>();
                if (MarkOfPrey != null)
                    MarkOfPrey.Cancel(false);

                new NfRaMarkOfPreyEffect().Start(player,target);
			}

			DisableSkill(living);
        }
		
		public override int GetReUseDelay(int level)
		{
			return 600;
		}

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Function: damage add");
            list.Add("");
            list.Add("Target's melee attacks do additional damage.");
			list.Add("");
			list.Add("Damage: 5.1");
			list.Add("Target: Group");
			list.Add("Range: 1000");
            list.Add("Duration: 30 sec");
            list.Add("Casting time: Instant");
			list.Add("");
			list.Add("Can use every: 10:00 min");
        }
    }
}
