using System.Text;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;
using Core.GS.Players;
using Core.GS.Server;

namespace Core.GS.Commands;

[Command(
	"&train",
	new string[] { "&trainline", "&trainskill" }, // new aliases to work around 1.105 client /train command
	EPrivLevel.Player,
	"Trains a line by the specified amount",
	"/train <line> <level>",
	"e.g. /train Dual Wield 50")]
public class TrainCommand : ACommandHandler, ICommandHandler
{
	private const string CantTrainSpec = "You can't train in this specialization again this level!";
	private const string NotEnoughPointsLeft = "You don't have that many specialization points left for this level.";

	// Allow to automate this command: no checks for spam command
	private bool automated = false;
	public TrainCommand() {}
	public TrainCommand(bool automated)
	{
		this.automated = automated;
	}
	
	public void OnCommand(GameClient client, string[] args)
	{
		if (!automated && IsSpammingCommand(client.Player, "train"))
		{
			return;
		}

		// no longer used since 1.105, except if we explicitely want
		if (client.Version >= EClientVersion.Version1105)
		{
			if (!ServerProperty.CUSTOM_TRAIN)
			{
				client.Out.SendTrainerWindow();
				return;
			}
		}

		GameTrainer trainer = client.Player.TargetObject as GameTrainer;
		// Make sure the player is at a trainer.
		if (!ServerProperty.ALLOW_TRAIN_ANYWHERE && client.Account.PrivLevel == (int)EPrivLevel.Player && (trainer == null || trainer.CanTrain(client.Player) == false))
		{
			client.Out.SendMessage("You have to be at your trainer to use this command.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		// Make sure the user gave us atleast the specialization line and the level to train it to.
		if (args.Length < 3)
		{
			DisplaySyntax(client);
			return;
		}

		// Get the level to train the specialization line to.
		int level;
		if (!int.TryParse(args[args.Length - 1], out level))
		{
			DisplaySyntax(client);
			return;
		}

		// Get the specialization line.
		string line = string.Join(" ", args, 1, args.Length - 2);
		line = GameServer.Database.Escape(line);

		var dbSpec = CoreDb<DbSpecialization>.SelectObject(DB.Column("KeyName").IsLike($"{line}%"));

		Specialization spec = null;

		if (dbSpec != null)
		{
			spec = client.Player.GetSpecializationByName(dbSpec.KeyName);
		}
		else
		{
			// if this is a custom line it might not be in the db so search for exact match on player
			spec = client.Player.GetSpecializationByName(line);
		}

		if (spec == null)
		{
			client.Out.SendMessage("The provided skill could not be found.", EChatType.CT_System, EChatLoc.CL_SystemWindow);

			return;
		}

		// Make sure the player can actually train the given specialization.
		int currentSpecLevel = spec.Level;

		if (currentSpecLevel >= client.Player.BaseLevel)
		{
			client.Out.SendMessage(CantTrainSpec, EChatType.CT_System, EChatLoc.CL_SystemWindow);

			return;
		}

		if (level <= currentSpecLevel)
		{
			client.Out.SendMessage("You have already trained the skill to this amount!", EChatType.CT_System,
			                       EChatLoc.CL_SystemWindow);

			return;
		}

		// Calculate the points to remove for training the specialization.
		level -= currentSpecLevel;
		ushort skillSpecialtyPoints = 0;
		int specLevel = 0;
		bool changed = false;
		bool canAutotrainSpec = client.Player.GetAutoTrainPoints(spec, 4) != 0;
		int autotrainPoints = client.Player.GetAutoTrainPoints(spec, 3);

		for (int i = 0; i < level; i++)
		{
			if (spec.Level + specLevel >= client.Player.BaseLevel)
			{
				client.Out.SendMessage(CantTrainSpec, EChatType.CT_System, EChatLoc.CL_SystemWindow);

				break;
			}

			// graveen: /train now match 1.87 autotrain rules
			if ((client.Player.SkillSpecialtyPoints + autotrainPoints) - skillSpecialtyPoints >= (spec.Level + specLevel) + 1)
			{
				changed = true;
				skillSpecialtyPoints += (ushort) ((spec.Level + specLevel) + 1);

				if (spec.Level + specLevel < client.Player.Level/4 && canAutotrainSpec)
				{
					skillSpecialtyPoints -= (ushort) ((spec.Level + specLevel) + 1);
				}

				specLevel++;
			}
			else
			{
				var sb = new StringBuilder();
				sb.AppendLine("That specialization costs " + (spec.Level + 1) + " specialization points!");
				sb.AppendLine(NotEnoughPointsLeft);

				client.Out.SendMessage(sb.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
				break;
			}
		}

		if (changed)
		{
			// tolakram - add some additional error checking to avoid overflow error
			if (client.Player.SkillSpecialtyPoints >= skillSpecialtyPoints)
			{
				spec.Level += specLevel;

				client.Player.OnSkillTrained(spec);

				client.Out.SendUpdatePoints();
				client.Out.SendTrainerWindow();

				client.Out.SendMessage("Training complete!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
			else
			{
				var sb = new StringBuilder();
				sb.AppendLine("That specialization costs " + (spec.Level + 1) + " specialization points!");
				sb.AppendLine(NotEnoughPointsLeft);

				client.Out.SendMessage(sb.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
	}
}