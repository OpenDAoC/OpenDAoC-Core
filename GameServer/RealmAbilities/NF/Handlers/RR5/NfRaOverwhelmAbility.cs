using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
    public class NfRaOverwhelmAbility : Rr5RealmAbility
    {
        public const int DURATION = 30 * 1000; // 30 secs
        public const double BONUS = 0.15; // 15% bonus
        public const int EFFECT = 1564;

        public NfRaOverwhelmAbility(DbAbility dba, int level) : base(dba, level) { }

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
                NfRaOverwhelmEffect Overwhelm = (NfRaOverwhelmEffect)player.EffectList.GetOfType<NfRaOverwhelmEffect>();
                if (Overwhelm != null)
                    Overwhelm.Cancel(false);

                new NfRaOverwhelmEffect().Start(player);
            }
            DisableSkill(living);
        }

        public override int GetReUseDelay(int level)
        {
            return 300;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("a 15% increased chance to bypass their target�s block, parry, and evade defenses.");
            list.Add("");
            list.Add("Target: Self");
            list.Add("Duration: 30 sec");
            list.Add("Casting time: Instant");
            list.Add("Re-use : 5 minutes");

        }

    }
}
