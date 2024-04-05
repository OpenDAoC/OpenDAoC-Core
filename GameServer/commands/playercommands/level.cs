using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute("&level",
    ePrivLevel.Player,
    "Allows you to level 20 instantly if you have a level 50", "/level")]
    public class LevelCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.SLASH_LEVEL_TARGET <= 1)
            {
                DisplayMessage(client, "/level is disabled on this server.");
                return;
            }

            if (client.Player.TargetObject is not GameTrainer)
            {
                client.Player.Out.SendMessage("You need to be at your trainer to use this command", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!ServerProperties.Properties.ALLOW_CATA_SLASH_LEVEL)
            {
                switch ((eCharacterClass) client.Player.CharacterClass.ID)
                {
                    case eCharacterClass.Heretic:
                    case eCharacterClass.Valkyrie:
                    case eCharacterClass.Warlock:
                    case eCharacterClass.Vampiir:
                    case eCharacterClass.Bainshee:
                    case eCharacterClass.MaulerAlb:
                    case eCharacterClass.MaulerHib:
                    case eCharacterClass.MaulerMid:
                    {
                        client.Player.Out.SendMessage("Your class cannot use /level command.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }
                }
            }
            if (!client.Player.CanUseSlashLevel)
            {
                client.Player.Out.SendMessage($"You don't have a level {ServerProperties.Properties.SLASH_LEVEL_REQUIREMENT} on your account!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (client.Player.Experience >= client.Player.GetExperienceNeededForLevel(ServerProperties.Properties.SLASH_LEVEL_TARGET - 1))
            {
                client.Player.Out.SendMessage($"/level only allows you to level to {ServerProperties.Properties.SLASH_LEVEL_TARGET}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            int targetLevel = ServerProperties.Properties.SLASH_LEVEL_TARGET;

            if (targetLevel is < 1 or > 50)
                targetLevel = 20;

            long newXP = client.Player.GetExperienceNeededForLevel(targetLevel - 1) - client.Player.Experience;

            if (newXP < 0)
                newXP = 0;

            client.Player.GainExperience(eXPSource.Other, newXP);
            client.Player.UsedLevelCommand = true;
            client.Player.Out.SendMessage($"You have been rewarded enough experience to reach level {ServerProperties.Properties.SLASH_LEVEL_TARGET}. Right click on your trainer to gain levels!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            client.Player.SaveIntoDatabase();
        }
    }
}
