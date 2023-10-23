
namespace Core.GS.Events;

/// <summary>
/// This class holds all possible server events.
/// Only constants defined here!
/// </summary>
public class GameServerEvent : CoreEvent
{
	/// <summary>
	/// Constructs a new GameServerEvent
	/// </summary>
	/// <param name="name">the name of the event</param>
	protected GameServerEvent(string name) : base(name)
	{
	}
	/// <summary>
	/// The Started event is fired whenever the GameServer has finished startup
	/// </summary>
	public static readonly GameServerEvent Started = new GameServerEvent("Server.Started");
	/// <summary>
	/// The Stopped event is fired whenever the GameServer is stopping
	/// </summary>
	public static readonly GameServerEvent Stopped = new GameServerEvent("Server.Stopped");
	/// <summary>
	/// The WorldSave event is fired whenever the GameServer saves the world
	/// </summary>
	public static readonly GameServerEvent WorldSave = new GameServerEvent("Server.WorldSave");
}