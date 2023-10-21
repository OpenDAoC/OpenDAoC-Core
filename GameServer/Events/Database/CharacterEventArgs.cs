using System;
using DOL.Database;
using DOL.GS;

namespace DOL.Events;

public class CharacterEventArgs : EventArgs
{
	/// <summary>
	/// Holds the target character for this event
	/// </summary>
	private DbCoreCharacter m_character;

	/// <summary>
	/// Holds the character's creation client for this event
	/// </summary>
	private GameClient m_client;
	
	/// <summary>
    /// Constructs a new event argument class for the
    /// character events 
	/// </summary>
	/// <param name="character"></param>
	/// <param name="client"></param>
	public CharacterEventArgs(DbCoreCharacter character, GameClient client)
	{
		m_character = character;
		m_client = client;
	}

	/// <summary>
	/// Gets the character for this event
	/// </summary>
	public DbCoreCharacter Character
	{
		get
		{
			return m_character;
		}
	}

	/// <summary>
	/// Gets the client for this event
	/// </summary>
	public GameClient GameClient
	{
		get
		{
			return m_client;
		}
	}
}