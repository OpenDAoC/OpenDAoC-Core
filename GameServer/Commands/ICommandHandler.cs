namespace Core.GS.Commands;

/// <summary>
/// Interface for classes that will handle commands
/// </summary>
public interface ICommandHandler
{
	/// <summary>
	/// Called when a command needs to be executed
	/// </summary>
	/// <param name="client">Client executing the command</param>
	/// <param name="args">Extra arguments for the command</param>
	void OnCommand(GameClient client, string[] args);
}