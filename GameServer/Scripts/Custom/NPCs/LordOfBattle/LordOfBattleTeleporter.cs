using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS {
    public class LordOfBattleTeleporter : GameTrainingDummy {
       

        public override bool AddToWorld()
        {
            Name = "Moooo-rdred";
            GuildName = "Teleporter Of Battle";
            Realm = 0;
            Model = 1190;
            Size = 15;
            Level = 75;
            Inventory = new GameNpcInventory(GameNpcInventoryTemplate.EmptyTemplate);
            SetOwnBrain(new BlankBrain());

            return base.AddToWorld(); // Finish up and add him to the world.
        }

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;
			TurnTo(player.X, player.Y);

			if (!this.Flags.HasFlag(ENpcFlags.GHOST))
            {
				player.Out.SendMessage("Greetings, " + player.PlayerClass.Name + ".\n\n" + "I can teleport you to our [fight club], if you promise not to speak of it to anyone.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);

			}

           
            return true;
			
			
		}
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!base.WhisperReceive(source, str)) return false;
			if (!(source is GamePlayer)) return false;
            if (source.InCombatInLast(10000)) return false;
			GamePlayer t = (GamePlayer)source;
			TurnTo(t.X, t.Y);
			switch (str)
			{
				case "fight club":
                    t.MoveTo(90, 34868, 33912, 19034, 4089);
                    break;
				default: break;
			}
			return true;
		}
		private void SendReply(GamePlayer target, string msg)
		{
			target.Client.Out.SendMessage(
				msg,
				EChatType.CT_Say, EChatLoc.CL_PopupWindow);
		}

	}
}
