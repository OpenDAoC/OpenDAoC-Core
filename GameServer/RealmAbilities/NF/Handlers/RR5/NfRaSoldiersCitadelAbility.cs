using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
    public class NfRaSoldiersCitadelAbility : Rr5RealmAbility
    {
        public const int DURATION = 30 * 1000;
        public const int SECOND_DURATION = 15 * 1000;

        public NfRaSoldiersCitadelAbility(DbAbility dba, int level) : base(dba, level) { }

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
            	NfRaSoldiersCitadelEffect SoldiersCitadel = player.EffectList.GetOfType<NfRaSoldiersCitadelEffect>();
                if (SoldiersCitadel != null)
                    SoldiersCitadel.Cancel(false);

                new NfRaSoldiersCitadelEffect().Start(player);
            }
            DisableSkill(living);
        }

        public override int GetReUseDelay(int level)
        {
            return 600;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("+50% block/parry 30s, -10% block/parry 15s.");
            list.Add("");
            list.Add("Target: Self");
            list.Add("Duration: 45 sec");
            list.Add("Casting time: Instant");
        }

    }
}
