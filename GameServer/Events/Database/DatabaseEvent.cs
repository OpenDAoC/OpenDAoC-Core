namespace Core.Events;

public class DatabaseEvent : CoreEvent
{
	/// <summary>
	/// Constructs a new DatabaseEvent
	/// </summary>
	/// <param name="name">the name of the event</param>
	protected DatabaseEvent(string name) : base(name)
	{
	}

	/// <summary>
	/// The AccountCreated event is fired whenever an account is created.
	/// <seealso cref="AccountEventArgs"/>
	/// </summary>
	public static readonly DatabaseEvent AccountCreated = new DatabaseEvent("Database.AccountCreated");
	/// <summary>
	/// The CharacterCreated event is fired whenever a new character is created
	/// <seealso cref="CharacterEventArgs"/>
	/// </summary>
	public static readonly DatabaseEvent CharacterCreated = new DatabaseEvent("Database.CharacterCreated");
	/// <summary>
	/// The CharacterDeleted event is fired whenever an account is deleted
	/// <seealso cref="CharacterEventArgs"/>
	/// </summary>
	public static readonly DatabaseEvent CharacterDeleted = new DatabaseEvent("Database.CharacterDeleted");
	/// <summary>
	/// The NewsCreated event is fired whenever news is created
	/// </summary>
	public static readonly DatabaseEvent NewsCreated = new DatabaseEvent("Database.NewsCreated");
	/// <summary>
	/// The CharacterSelected event is fired whenever the player hit "play" button with a valid character..
	/// </summary>
	public static readonly DatabaseEvent CharacterSelected = new DatabaseEvent("Database.CharacterSelected");
}