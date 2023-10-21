using System.Collections.Generic;
using Core.Database;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities
{
    public class NfRaMinionRescueAbility : Rr5RealmAbility
    {
        public const int DURATION = 6 * 1000;
        public const int SpellRadius = 500;

        public NfRaMinionRescueAbility(DbAbility dba, int level) : base(dba, level) { }

        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            GamePlayer player = living as GamePlayer;
            if (player == null)
                return;

            foreach (GamePlayer Targetplayer in player.GetPlayersInRadius((ushort)SpellRadius))
            {
                if (Targetplayer != null && Targetplayer.IsAlive && GameServer.ServerRules.IsAllowedToAttack(player, Targetplayer, true))
                {
                    NfRaMinionRescueEffect raEffect = new NfRaMinionRescueEffect();
                    raEffect.Start(player);
                    DisableSkill(living);
                    return;
                }
            }
        }

        public override int GetReUseDelay(int level)
        {
            return 900;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Summon pets that will follow and stun enemies.");
            list.Add("");
            list.Add("Target: Enemy");
            list.Add("Duration: 6 sec");
            list.Add("Casting time: Instant");
        }

    }
}
