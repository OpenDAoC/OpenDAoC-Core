using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
    public class NfRaFanaticismHandler :  Rr5RealmAbility
	{
		public NfRaFanaticismHandler(DbAbilities dba, int level) : base(dba, level) { }
 
		int RANGE = 2000;
        public const int DURATION = 45 * 1000;
        public const int VALUE = 25;

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
						if(grpMate.CharacterClass.ClassType == eClassType.Hybrid
				  			|| grpMate.CharacterClass.ClassType == eClassType.PureTank)
							targets.Add(grpMate);
			
			foreach (GamePlayer target in targets)
			{
				NfRaFanaticismEffect fanaticism = target.EffectList.GetOfType<NfRaFanaticismEffect>();
                if (fanaticism != null)
                    fanaticism.Cancel(false);

                new NfRaFanaticismEffect().Start(target);
			}
			DisableSkill(player);
        }
		
		public override int GetReUseDelay(int level)
		{
			return 600;
		}
		
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("All Heretic groupmates who are able to bind at a keep or tower lord receive a reduction in all spell damage taken for 45 seconds.");
            list.Add("");
            list.Add("Target: Group");
            list.Add("Duration: 45s");
            list.Add("Casting time: Instant");
        }
    }
}