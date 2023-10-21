using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
    public class NfRaBlooddrinkingAbility : Rr5RealmAbility
    {
        public const int DURATION = 30 * 1000; // 30 secs
        public const double HEALPERCENT = 20; // 20% heal
        public const int EFFECT = 1567;

        public NfRaBlooddrinkingAbility(DbAbility dba, int level) : base(dba, level) { }

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
                NfRaBloodDrinkingEffect BloodDrinking = (NfRaBloodDrinkingEffect)player.EffectList.GetOfType<NfRaBloodDrinkingEffect>();
                if (BloodDrinking != null)
                    BloodDrinking.Cancel(false);

                new NfRaBloodDrinkingEffect().Start(player);
            }
            DisableSkill(living);
        }

        public override int GetReUseDelay(int level)
        {
            return 300;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Cause the Shadowblade to be healed for 20% of all damage he does for 30 seconds");
            list.Add("");
            list.Add("Target: Self");
            list.Add("Duration: 30 sec");
            list.Add("Casting time: Instant");
            list.Add("Re-use : 5 minutes");

        }

    }
}
