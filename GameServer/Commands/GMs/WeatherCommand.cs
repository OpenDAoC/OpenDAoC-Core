using System;

namespace DOL.GS.Commands;

[Command(
	"&weather",
	EPrivLevel.GM,
	"Sets the weather for the current region",
	"'/weather info' for information about the current weather in this region",
	"'/weather start <line> <duration> <speed> <diffusion> <intensity>' to start a storm in this region",
	"'/weather start' to start a random storm in this region",
	"'/weather restart' to restart the storm in this region",
	"'/weather stop' to stop the storm in this region")]
public class WeatherCommand : ACommandHandler, ICommandHandler
{
	/// <summary>
	/// Execute Weather Command
	/// </summary>
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length >= 2)
		{
			var action = args[1].ToLower();
			
			switch (action)
			{
				case "info":
					break;
				case "restart":
					if (GameServer.Instance.WorldManager.WeatherManager.RestartWeather(client.Player.CurrentRegionID))
						DisplayMessage(client, "Weather (restart): Restarting Weather in this region!");
					else
						DisplayMessage(client, "Weather (restart): Weather could not be restarted in this region!");
					break;
				case "stop":
					if (GameServer.Instance.WorldManager.WeatherManager.StopWeather(client.Player.CurrentRegionID))
						DisplayMessage(client, "Weather (stop): Weather was Stopped in this Region!");
					else
						DisplayMessage(client, "Weather (stop): Weather could not be Stopped in this Region!");
					break;
				case "start":
					if (args.Length > 2)
					{
						try
						{
							uint position = Convert.ToUInt32(args[2]);
							uint width = Convert.ToUInt32(args[3]);
							ushort speed = Convert.ToUInt16(args[4]);
							ushort diffusion = Convert.ToUInt16(args[5]);
							ushort intensity = Convert.ToUInt16(args[6]);
							if (!GameServer.Instance.WorldManager.WeatherManager.StartWeather(client.Player.CurrentRegionID, position, width, speed, diffusion, intensity))
							{
								DisplayMessage(client, "Weather (start): Weather could not be started in this Region!");
								break;
							}
						}
						catch
						{
							DisplayMessage(client, "Weather (start): Wrong Arguments...");
							DisplaySyntax(client);
							return;
						}
					}
					else
					{
						if (!GameServer.Instance.WorldManager.WeatherManager.StartWeather(client.Player.CurrentRegionID))
						{
							DisplayMessage(client, "Weather (start): Weather could not be started in this Region!");
							break;
						}
					}
					
					DisplayMessage(client, "Weather (start): The Weather has been started for this region!");
					break;
			}
			PrintInfo(client);
			return;
		}
		
		DisplaySyntax(client);
	}
	
	/// <summary>
	/// Display Weather Info to Client
	/// </summary>
	/// <param name="client"></param>
	public void PrintInfo(GameClient client)
	{
		var weather = GameServer.Instance.WorldManager.WeatherManager[client.Player.CurrentRegionID];
		
		if (weather == null)
		{
			DisplayMessage(client, "Weather (info): No Weather Registered for current Region...");
		}
		else
		{
			if (weather.StartTime == 0)
				DisplayMessage(client, "Weather (info): Weather is stopped for current Region...");
			else
				DisplayMessage(client, "Weather (info): Current Position - {0} - {1}", weather.CurrentPosition(Scheduler.SimpleScheduler.Ticks), weather);
		}
	}
}