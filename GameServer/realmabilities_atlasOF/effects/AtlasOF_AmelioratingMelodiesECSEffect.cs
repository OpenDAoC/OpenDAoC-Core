using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class AmelioratingMelodiesECSEffect : ECSGameAbilityEffect
    {
        private const int RANGE = 1500;
        private int _heal;

        public AmelioratingMelodiesECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.AmelioratingMelodies;
            PulseFreq = 1500; // 1.5s. Effect lasts 30s so that is 20 ticks.
            NextTick = StartTick;
            _heal = (int) Effectiveness; // Effectiveness value is used as a heal value per tick.
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon => 4250;
        public override string Name => "Ameliorating Melodies";
        public override bool HasPositiveEffect => true;

        public override void OnEffectPulse()
        {
            if (OwnerPlayer == null)
                return;

            ICollection<GamePlayer> playersToHeal = new List<GamePlayer>();

            // OF AM works on the caster as well, unlike NF AM.
            if (OwnerPlayer.Group == null)
                playersToHeal.Add(OwnerPlayer);
            else
                playersToHeal = OwnerPlayer.Group.GetPlayersInTheGroup();

            foreach (GamePlayer player in playersToHeal)
            {
                if ((player.Health < player.MaxHealth) && OwnerPlayer.IsWithinRadius(player, RANGE) && player.IsAlive)
                {
                    int heal = _heal;

                    if (player.Health + heal > player.MaxHealth)
                        heal = player.MaxHealth - player.Health;

                    player.ChangeHealth(OwnerPlayer, eHealthChangeType.Regenerate, heal);
                    OwnerPlayer.Out.SendMessage($"Your Ameliorating Melodies heal {player.Name} for {heal} hit points.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    player.Out.SendMessage($"{OwnerPlayer.Name} 's Ameliorating Melodies heals you for {heal} hit points.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
            }
        }
    }
}
