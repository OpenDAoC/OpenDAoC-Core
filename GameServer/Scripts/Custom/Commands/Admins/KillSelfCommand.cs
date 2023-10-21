using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Commands
{
    /// <summary>
    /// Command handler for the /killself command
    /// </summary>
    [Command(
        "&suicide",
        EPrivLevel.Admin,
        "Kill yourself. You can't suicide while in combat!")]
    public class KillSelfCommand : ACommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player.InCombat)
            {
                DisplayMessage(client, "You can't kill yourself while in combat!");
                return;
            } else if (client.Player.CurrentZone.IsRvR)
            {
                DisplayMessage(client, "There are other ways to die in the Frontiers.");
                return;
            }
            else if (!client.Player.IsAlive)
            {
                DisplayMessage(client, "You are already dead!");
                return;
            }
            else
            {
                client.Out.SendCustomDialog("Do you want kill yourself?", new CustomDialogResponse(SuicideResponceHandler));
            }
        }
        protected virtual void SuicideResponceHandler(GamePlayer player, byte response)
        {
            //int amount = 10000;

            if (response == 1)
            {
                {
                    player.Emote(EEmote.SpellGoBoom);
                    player.TakeDamage(player, EDamageType.Natural, player.MaxHealth, 0);
                }
            }
            else
            {
                return;
            }

        }
    }
}
