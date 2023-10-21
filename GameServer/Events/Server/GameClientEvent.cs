namespace Core.Events;

/// <summary>
/// This class holds all possible GameClient events.
/// Only constants defined here!
/// </summary>
public class GameClientEvent : CoreEvent
{
	/// <summary>
	/// Constructs a new GameClientEvent
	/// </summary>
	/// <param name="name">the name of the event</param>
	protected GameClientEvent(string name) : base(name)
	{
	}

	/// <summary>
	/// The Created event is fired whenever a GameClient is created
	/// </summary>
	public static readonly GameClientEvent Created = new GameClientEvent("GameClient.Created");
	/// <summary>
	/// The Connected event is fired whenever a GameClient has connected
	/// </summary>
	public static readonly GameClientEvent Connected = new GameClientEvent("GameClient.Connected");
	/// <summary>
	/// The Disconnected event is fired whenever a GameClient has disconnected
	/// </summary>
	public static readonly GameClientEvent Disconnected = new GameClientEvent("GameClient.Disconnected");
	/// <summary>
	/// The PlayerLoaded event is fired whenever a player is set for the GameClient
	/// </summary>
	public static readonly GameClientEvent PlayerLoaded = new GameClientEvent("GameClient.PlayerLoaded");
	/// <summary>
	/// The StateChanged event is fired whenever the GameClient's state changed
	/// </summary>
	public static readonly GameClientEvent StateChanged = new GameClientEvent("GameClient.StateChanged");
	/// <summary>
	/// The AccountLoaded event is fired whenever an account has been set for the GameClient
	/// </summary>
	public static readonly GameClientEvent AccountLoaded = new GameClientEvent("GameClient.AccountLoaded");
}