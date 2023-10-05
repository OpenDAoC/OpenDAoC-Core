using System;
using System.Linq;

namespace DOL.GS.Commands;

[Command(
	"&heal",
	ePrivLevel.GM,
	"GMCommands.Heal.Description",
	"GMCommands.Heal.Usage",
	"/heal me - heals self")]
public class HealCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		try
		{
			GameLiving target = client.Player.TargetObject as GameLiving;
			
			if (target == null || (args.Length > 1 && args[1].ToLower() == "me"))
				target = (GameLiving)client.Player;

			target.Health = target.MaxHealth;
			target.Endurance = target.MaxEndurance;
			target.Mana = target.MaxMana;

			if (target.effectListComponent.ContainsEffectForEffectType(eEffect.ResurrectionIllness))
			{
				EffectService.RequestCancelEffect(target.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == eEffect.ResurrectionIllness));
			}
			
			if (target.effectListComponent.ContainsEffectForEffectType(eEffect.RvrResurrectionIllness))
			{
				EffectService.RequestCancelEffect(target.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == eEffect.RvrResurrectionIllness));
			}
		}
		catch (Exception)
		{
			DisplaySyntax(client);
		}
	}
}