using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
	   "&dismisspet",
	   ePrivLevel.Player,
		 "Dismiss the novelty pet", "/dismisspet")]
	public class DismissPetCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "dismisspet"))
				return;

			if (client.Player.TempProperties.GetProperty<bool>(NoveltyPetBrain.HAS_PET))
			{
				foreach (GameSummonedPet pet in client.Player.GetNPCsInRadius(500))
				{
					if (pet.Brain is NoveltyPetBrain)
					{
						if (pet.Owner == client.Player)
						{
							pet.RemoveFromWorld();
							client.Player.TempProperties.RemoveProperty(NoveltyPetBrain.HAS_PET);
							client.Player.MessageToSelf("You have dismissed your companion pet.",eChatType.CT_Spell);
						}
		
					}
				}
			}
			else
			{
				client.Player.MessageToSelf("You have no companion pet.",eChatType.CT_SpellResisted);
			}
		}

	}
}
