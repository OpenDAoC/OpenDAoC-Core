using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;

namespace Core.GS.Commands
{
	[Command(
	   "&dismisspet",
	   EPrivLevel.Player,
		 "Dismiss the novelty pet", "/dismisspet")]
	public class DismissPetCommand : ACommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "dismisspet"))
				return;

			if (client.Player.TempProperties.GetProperty<bool>(NoveltyPetBrain.HAS_PET, false))
			{
				foreach (GameSummonedPet pet in client.Player.GetNPCsInRadius(500))
				{
					if (pet.Brain is NoveltyPetBrain)
					{
						if (pet.Owner == client.Player)
						{
							pet.RemoveFromWorld();
							client.Player.TempProperties.RemoveProperty(NoveltyPetBrain.HAS_PET);
							client.Player.MessageToSelf("You have dismissed your companion pet.",EChatType.CT_Spell);
						}
		
					}
				}
			}
			else
			{
				client.Player.MessageToSelf("You have no companion pet.",EChatType.CT_SpellResisted);
			}
		}

	}
}
