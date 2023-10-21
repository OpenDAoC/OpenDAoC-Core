using System.Collections.Generic;
using Core.Database;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.RealmAbilities
{
    public class NfRaSearingPetAbility : Rr5RealmAbility
    {
        public const int DURATION = 19 * 1000;

        public NfRaSearingPetAbility(DbAbility dba, int level) : base(dba, level) { }

        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            GamePlayer player = living as GamePlayer;
            if (player != null && player.ControlledBrain != null && player.ControlledBrain.Body != null)
            {
                GameNpc pet = player.ControlledBrain.Body as GameNpc;
                if (pet.IsAlive)
                {
					NfRaSearingPetEffect SearingPet = pet.EffectList.GetOfType<NfRaSearingPetEffect>();
                    if (SearingPet != null) SearingPet.Cancel(false);
                    new NfRaSearingPetEffect(player).Start(pet);
                }
                DisableSkill(living);
            }
            else if (player != null)
            {
                player.Out.SendMessage("You must have a controlled pet to use this ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                player.DisableSkill(this, 3 * 1000);
            }
        }

        public override int GetReUseDelay(int level)
        {
            return 120;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add(" PBAoE Pet pulsing effect, 350units, 25 damage, 6 ticks, 2min RUT.");
            list.Add("");
            list.Add("Target: Pet");
            list.Add("Duration: 18s");
            list.Add("Casting time: Instant");
        }

    }
}


