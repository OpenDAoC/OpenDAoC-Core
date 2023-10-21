using System.Collections;
using System.Collections.Generic;
using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
    public class NfRaValhallasBlessingAbility : Rr5RealmAbility
    {
        public const int DURATION = 30 * 1000;
        private const int SpellRadius = 1500;

        public NfRaValhallasBlessingAbility(DbAbility dba, int level) : base(dba, level) { }

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
                    foreach (GamePlayer grpplayer in player.Group.GetPlayersInTheGroup())
                    {
                        if (player.IsWithinRadius( grpplayer, SpellRadius ) && grpplayer.IsAlive)
                            targets.Add(grpplayer);
                    }
                }
                foreach (GamePlayer target in targets)
                {
                    //send spelleffect
                    if (!target.IsAlive) continue;
					NfRaValhallasBlessingEffect ValhallasBlessing = target.EffectList.GetOfType<NfRaValhallasBlessingEffect>();
                    if (ValhallasBlessing != null)
                        ValhallasBlessing.Cancel(false);
                    new NfRaValhallasBlessingEffect().Start(target);
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
            list.Add("Spells/Styles used by group have has a chance of not costing power or endurance. 30s duration, 10min RUT.");
            list.Add("");
            list.Add("Target: Group");
            list.Add("Duration: 30s");
            list.Add("Casting time: Instant");
        }

    }
}


