using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{
    public class NfRaFungalUnionAbility : Rr5RealmAbility
    {
        public NfRaFungalUnionAbility(DbAbility dba, int level) : base(dba, level) { }


        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;



            GamePlayer player = living as GamePlayer;
            if (player != null)
            {
                SendCasterSpellEffectAndCastMessage(player, 7062, true);
                NfRaFungalUnionEffect effect = new NfRaFungalUnionEffect();
                effect.Start(player);
            }
            DisableSkill(living);
        }

        public override int GetReUseDelay(int level)
        {
            return 420;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Fungal Union.");
            list.Add("");
            list.Add("Target: Self");
            list.Add("Duration: 60 seconds");
            list.Add("Casting time: instant");
        }

    }
}