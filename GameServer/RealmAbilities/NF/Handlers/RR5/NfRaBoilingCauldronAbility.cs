/*
 * 1.108 The Spill Over of this ability has been decreased to 3.5 seconds, and its damage increased to 650.
 */

using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
    public class NfRaBoilingCauldronAbility : Rr5RealmAbility
    {
        public const int DURATION = 4500;

        public NfRaBoilingCauldronAbility(DbAbility dba, int level) : base(dba, level) { }

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
            	NfRaBoilingCauldronEffect BoilingCauldron = player.EffectList.GetOfType<NfRaBoilingCauldronEffect>();
                if (BoilingCauldron != null)
                    BoilingCauldron.Cancel(false);

                new NfRaBoilingCauldronEffect().Start(player);
            }
            DisableSkill(living);
        }

        public override int GetReUseDelay(int level)
        {
            return 900;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Summon a cauldron that boil in place for 3.5s before spilling and doing damage to all those nearby. 15min RUT.");
            list.Add("");
            list.Add("Target: Enemy");
            list.Add("Duration: 3.5s");
            list.Add("Casting time: Instant");
        }

    }
}
