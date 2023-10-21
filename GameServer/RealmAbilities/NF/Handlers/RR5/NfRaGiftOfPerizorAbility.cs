using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
    public class NfRaGiftOfPerizorAbility : Rr5RealmAbility
    {
        public const int DURATION = 60 * 1000;
        private const int SpellRadius = 1500;

        public NfRaGiftOfPerizorAbility(DbAbility dba, int level) : base(dba, level) { }

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
				ArrayList targets = new ArrayList();
				if (player.Group == null)
					targets.Add(player);
				else
				{
					foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
					{
						if (player.IsWithinRadius(p, SpellRadius ) && p.IsAlive)
							targets.Add(p);
					}
				}
				foreach (GamePlayer target in targets)
				{
					//send spelleffect
					if (!target.IsAlive) continue;
					NfRaGiftOfPerizorEffect GiftOfPerizor = target.EffectList.GetOfType<NfRaGiftOfPerizorEffect>();
					if (GiftOfPerizor != null) GiftOfPerizor.Cancel(false);
					target.TempProperties.SetProperty("GiftOfPerizorOwner", player);
					new NfRaGiftOfPerizorEffect().Start(target);
				}
			}
			DisableSkill(living);
		}

        public override int GetReUseDelay(int level)
        {
            return 600;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Buff group with 25% damage reduction for 60 seconds, return damage reduced as power. 10min RUT.");
            list.Add("");
            list.Add("Target: Self");
            list.Add("Duration: 1 min");
            list.Add("Casting time: Instant");
        }

    }
}
