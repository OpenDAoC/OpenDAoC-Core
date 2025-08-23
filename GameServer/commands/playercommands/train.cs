using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [Cmd(
        "&train",
        ["&trainline", "&trainskill"], // New aliases to work around 1.105 client /train command.
        ePrivLevel.Player,
        "Trains a line by the specified amount",
        "/train <line> <level>",
        "e.g. /train Dual Wield 50")]
    public class TrainCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const string CANNOT_TRAIN_SPEC = "You can't train in this specialization again this level!";
        private const string NOT_ENOUGH_POINTS = "You don't have that many specialization points left for this level.";

        public TrainCommandHandler() { }

        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "train"))
                return;

            // No longer used since 1.105, except if we explicitly want.
            if (client.Version >= GameClient.eClientVersion.Version1105)
            {
                if (!ServerProperties.Properties.CUSTOM_TRAIN)
                {
                    client.Out.SendTrainerWindow();
                    return;
                }
            }

            if (!CanTrain(client))
                return;

            // Make sure the user gave us atleast the specialization line and the level to train it to.
            if (args.Length < 3)
            {
                DisplaySyntax(client);
                return;
            }

            // Get the level to train the specialization line to.
            if (!int.TryParse(args[^1], out int level))
            {
                DisplaySyntax(client);
                return;
            }

            // Get the specialization line.
            string line = string.Join(' ', args, 1, args.Length - 2);
            line = GameServer.Database.Escape(line);
            Specialization spec;
            var dbSpec = DOLDB<DbSpecialization>.SelectObject(DB.Column("KeyName").IsLike($"{line}%"));

            if (dbSpec != null)
                spec = client.Player.GetSpecializationByName(dbSpec.KeyName);
            else
                spec = client.Player.GetSpecializationByName(line); // If this is a custom line, it might not be in the DB, so search for exact match on player.

            if (spec == null)
            {
                client.Out.SendMessage("The provided skill could not be found.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!Train(client, spec, level))
                return;

            OnTrained(client);
        }

        public static bool Train(GameClient client, Specialization spec, int level)
        {
            // Make sure the player can actually train the given specialization.
            int currentSpecLevel = spec.Level;

            if (currentSpecLevel >= client.Player.BaseLevel)
            {
                client.Out.SendMessage(CANNOT_TRAIN_SPEC, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (level <= currentSpecLevel)
            {
                client.Out.SendMessage("You have already trained the skill to this amount!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            // Calculate the points to remove for training the specialization.
            level -= currentSpecLevel;
            ushort skillSpecialtyPoints = 0;
            int specLevel = 0;
            bool changed = false;
            bool canAutoTrain = client.Player.GetAutoTrainPoints(spec, 4) != 0;
            int autoTrainPoints = client.Player.GetAutoTrainPoints(spec, 3);

            for (int i = 0; i < level; i++)
            {
                if (spec.Level + specLevel >= client.Player.BaseLevel)
                {
                    client.Out.SendMessage(CANNOT_TRAIN_SPEC, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }

                if (client.Player.SkillSpecialtyPoints + autoTrainPoints - skillSpecialtyPoints >= spec.Level + specLevel + 1)
                {
                    changed = true;
                    skillSpecialtyPoints += (ushort) (spec.Level + specLevel + 1);

                    if (spec.Level + specLevel < client.Player.Level / 4 && canAutoTrain)
                        skillSpecialtyPoints -= (ushort) (spec.Level + specLevel + 1);

                    specLevel++;
                }
                else
                {
                    client.Out.SendMessage($"That specialization costs {spec.Level + 1} specialization points! {NOT_ENOUGH_POINTS}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    break;
                }
            }

            if (changed)
            {
                if (client.Player.SkillSpecialtyPoints >= skillSpecialtyPoints)
                {
                    spec.Level += specLevel;
                    OnSpecTrained(client, spec);
                }
                else
                    client.Out.SendMessage($"That specialization costs {spec.Level + 1} specialization points! {NOT_ENOUGH_POINTS}", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            return changed;
        }

        public static bool CanTrain(GameClient client)
        {
            if (ServerProperties.Properties.ALLOW_TRAIN_ANYWHERE || (ePrivLevel) client.Account.PrivLevel is not ePrivLevel.Player)
                return true;

            // A trainer of the appropriate class must be around (or global trainer, with TrainedClass = eCharacterClass.Unknown).
            if (client.Player.TargetObject is GameTrainer trainer && (trainer.CanTrain(client.Player) || trainer.CanTrainChampionLevels(client.Player)))
                return true;

            client.Out.SendMessage("You must select a valid trainer for your class.", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
            return false;
        }

        public static void OnSpecTrained(GameClient client, Specialization specialization)
        {
            GamePlayer player = client.Player;
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GamePlayer.OnSkillTrained.YouSpend", specialization.Level, specialization.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GamePlayer.OnSkillTrained.YouHave", player.SkillSpecialtyPoints), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            Message.SystemToOthers(client.Player, LanguageMgr.GetTranslation(client.Account.Language, "GamePlayer.OnSkillTrained.TrainsInVarious", client.Player.GetName(0, true)), eChatType.CT_System);
            player.CharacterClass.OnSkillTrained(player, specialization);
        }

        public static void OnTrained(GameClient client)
        {
            client.Player.RefreshSpecDependantSkills(true);
            client.Out.SendUpdatePoints();
            client.Out.SendUpdatePlayer();
            client.Out.SendCharResistsUpdate();
            client.Out.SendCharStatsUpdate();
            client.Out.SendUpdatePlayerSkills(true);
            client.Out.SendTrainerWindow();
            client.Out.SendMessage("Training complete!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
