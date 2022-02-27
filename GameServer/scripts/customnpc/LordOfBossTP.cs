using DOL.AI.Brain;
using DOL.GS.PacketHandler;

namespace DOL.GS {
    public class LordOfBossTP : GameTrainingDummy {
        
        public override bool AddToWorld()
        {
            Name = "Meow-rdred";
            GuildName = "Teleporter Of Bosses";
            Realm = 0;
            Model = 465;
            Size = 35;
            Level = 75;
            Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
            SetOwnBrain(new BlankBrain());

            return base.AddToWorld(); // Finish up and add him to the world.
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;
            TurnTo(player.X, player.Y);

            if (!this.Flags.HasFlag(GameNPC.eFlags.GHOST))
            {
                player.Out.SendMessage("Greetings, " + player.CharacterClass.Name + ".\n\n" + "I can teleport you to our [boss arena] if you think you stand a chance..", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
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
                case "boss arena":
                    switch (t.Realm)
                    {
                        case eRealm._FirstPlayerRealm:
                            t.MoveTo(90, 34871, 32471, 18850, 10);
                            break;
                        case eRealm._LastPlayerRealm:
                            t.MoveTo(47, 34871, 32471, 18850, 10);
                            break;
                        case eRealm.Midgard:
                            t.MoveTo(147, 34871, 32471, 18850, 10);
                            break;
                    }
                    
                    break;
            }
            return true;
        }
        private void SendReply(GamePlayer target, string msg)
        {
            target.Client.Out.SendMessage(
                msg,
                eChatType.CT_Say, eChatLoc.CL_PopupWindow);
        }

    }
}