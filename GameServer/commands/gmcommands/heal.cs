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
				GameLiving target = client.Player.TargetObject as GameLiving;
				
				if (target == null || (args.Length > 1 && args[1].ToLower() == "me"))
					target = (GameLiving)client.Player;

				target.Health = target.MaxHealth;
				target.Endurance = target.MaxEndurance;
				target.Mana = target.MaxMana;

				foreach (ECSGameEffect effect in target.effectListComponent.GetAllEffects())
				{
					if (effect.EffectType is eEffect.ResurrectionIllness or eEffect.RvrResurrectionIllness)
						effect.Stop();
				}
			}
			catch (Exception)
			{
				DisplaySyntax(client);
			}
		}
	}
}