using System;

namespace Core.GS;

/// <summary>
/// GameServer Manager to handle Npc Data and Other Behavior for the whole Instance.
/// </summary>
public sealed class NpcManager
{
	/// <summary>
	/// Reference to the Instanced GameServer
	/// </summary>
	private GameServer GameServerInstance { get; set; }
	
	public MobAmbientBehaviourManager AmbientBehaviour { get; private set; }
	
	/// <summary>
	/// Create a new Instance of <see cref="NpcManager"/>
	/// </summary>
	public NpcManager(GameServer GameServerInstance)
	{
		if (GameServerInstance == null)
			throw new ArgumentNullException("GameServerInstance");

		this.GameServerInstance = GameServerInstance;
		
		AmbientBehaviour = new MobAmbientBehaviourManager(this.GameServerInstance.IDatabase);
	}
}