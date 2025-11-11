using System;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&heal",
        ePrivLevel.GM,
        "GMCommands.Heal.Description",
        "GMCommands.Heal.Usage",
        "/heal me - heals self")]
    public class HealCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            try
            {
                if (client.Player.TargetObject is not GameLiving target || (args.Length > 1 && args[1].Equals("me", StringComparison.OrdinalIgnoreCase)))
                    target = client.Player;

                target.Health = target.MaxHealth;
                target.Endurance = target.MaxEndurance;
                target.Mana = target.MaxMana;

                foreach (ECSGameEffect effect in target.effectListComponent.GetEffects())
                {
                    if (effect.EffectType is eEffect.ResurrectionIllness or eEffect.RvrResurrectionIllness)
                        effect.End();
                }
            }
            catch (Exception)
            {
                DisplaySyntax(client);
            }
        }
    }
}
