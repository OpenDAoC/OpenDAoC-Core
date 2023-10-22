using System;

namespace Core.GS.World;

/// <summary>
/// GameServer Manager to Handle World Data and Region events for this GameServer.
/// </summary>
public sealed class WorldEventMgr
{
	/// <summary>
	/// Reference to the Instanced GameServer
	/// </summary>
	private GameServer GameServerInstance { get; set; }

	/// <summary>
	/// Reference to the World Weather Manager
	/// </summary>
	public WeatherMgr WeatherManager { get; private set; }
	
	/// <summary>
	/// Create a new instance of <see cref="WorldEventMgr"/>
	/// </summary>
	public WorldEventMgr(GameServer GameServerInstance)
	{
		if (GameServerInstance == null)
			throw new ArgumentNullException("GameServerInstance");

		this.GameServerInstance = GameServerInstance;
		
		WeatherManager = new WeatherMgr(this.GameServerInstance.Scheduler);
	}
}