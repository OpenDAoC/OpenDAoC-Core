using System;
using System.Linq;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&heal",
	EPrivLevel.GM,
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

			if (target.effectListComponent.ContainsEffectForEffectType(EEffect.ResurrectionIllness))
			{
				EffectService.RequestCancelEffect(target.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == EEffect.ResurrectionIllness));
			}
			
			if (target.effectListComponent.ContainsEffectForEffectType(EEffect.RvrResurrectionIllness))
			{
				EffectService.RequestCancelEffect(target.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == EEffect.RvrResurrectionIllness));
			}
		}
		catch (Exception)
		{
			DisplaySyntax(client);
		}
	}
}