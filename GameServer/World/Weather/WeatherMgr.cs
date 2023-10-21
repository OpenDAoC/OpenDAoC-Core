﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Events;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.Scheduler;
using log4net;

namespace Core.GS
{
	/// <summary>
	/// WeatherManager class handle current weather in compatible Regions.
	/// </summary>
	public sealed class WeatherMgr
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Sync Lock Object
		/// </summary>
		private readonly object LockObject = new object();
		
		/// <summary>
		/// Server Scheduler Reference
		/// </summary>
		private SimpleScheduler Scheduler { get; set; }
		
		/// <summary>
		/// Default Weather Check Timer Interval
		/// </summary>
		private int DefaultTimerInterval { get { return Math.Max(1000, ServerProperties.Properties.WEATHER_CHECK_INTERVAL); } }
		
		/// <summary>
		/// Default Weather Chance
		/// </summary>
		private int DefaultWeatherChance { get { return Math.Min(99, ServerProperties.Properties.WEATHER_CHANCE); } }
		
		/// <summary>
		/// Log Weather Change to Info Logger
		/// </summary>
		private bool EventLogWeather { get { return ServerProperties.Properties.WEATHER_LOG_EVENTS; } }
		
		/// <summary>
		/// Dictionary of Regions to be handled.
		/// </summary>
		private Dictionary<ushort, RegionWeather> RegionsWeather { get; set; }
		
		/// <summary>
		/// Dictionary of Region's Tasks.
		/// </summary>
		private Dictionary<ushort, ScheduledTask> RegionsTasks { get; set; }
		
		/// <summary>
		/// Retrieve Region Weather from Region ID
		/// </summary>
		public RegionWeather this[ushort regionId]
		{
			get
			{
				RegionWeather weather;
				lock (LockObject)
					RegionsWeather.TryGetValue(regionId, out weather);
				
				return weather;
			}
		}
		
		/// <summary>
		/// Create a new Instance of <see cref="WeatherMgr"/>
		/// </summary>
		public WeatherMgr(SimpleScheduler Scheduler)
		{
			this.Scheduler = Scheduler;
			RegionsWeather = new Dictionary<ushort, RegionWeather>();
			RegionsTasks = new Dictionary<ushort, ScheduledTask>();

			GameEventMgr.AddHandler(RegionEvent.RegionStart, OnRegionStart);
			GameEventMgr.AddHandler(RegionEvent.RegionStop, OnRegionStop);
			GameEventMgr.AddHandler(RegionEvent.PlayerEnter, OnPlayerEnter);
		}
		
		/// <summary>
		/// Start a Random Weather for Region
		/// </summary>
		/// <param name="regionId">Region ID where weather must be changed</param>
		/// <returns>True if weather was changed</returns>
		public bool StartWeather(ushort regionId)
		{
			return ChangeWeather(regionId, weather => weather.CreateWeather(SimpleScheduler.Ticks));
		}
		
		/// <summary>
		/// Start a Parametrized Weather for Region
		/// </summary>
		/// <param name="regionId">Region ID where weather must be changed</param>
		/// <param name="position">Weather Position</param>
		/// <param name="width">Weather Width</param>
		/// <param name="speed">Weather Speed</param>
		/// <param name="diffusion">Weather Diffusion</param>
		/// <param name="intensity">Weather Intensity</param>
		/// <returns>True if weather was changed</returns>
		public bool StartWeather(ushort regionId, uint position, uint width, ushort speed, ushort diffusion, ushort intensity)
		{
			return ChangeWeather(regionId, weather => weather.CreateWeather(position, width, speed, intensity, diffusion, SimpleScheduler.Ticks));
		}
		
		/// <summary>
		/// Restart Weather for Region
		/// </summary>
		/// <param name="regionId">Region ID where weather must be restarted</param>
		/// <returns>True if weather was restarted</returns>
		public bool RestartWeather(ushort regionId)
		{
			return ChangeWeather(regionId, weather => weather.CreateWeather(weather.Position, weather.Width, weather.Speed, weather.Intensity, weather.FogDiffusion, SimpleScheduler.Ticks));
		}
		
		/// <summary>
		/// Stop Weather for Region
		/// </summary>
		/// <param name="regionId">Region ID where weather must be stopped</param>
		/// <returns>True if weather was stopped</returns>
		public bool StopWeather(ushort regionId)
		{
			return ChangeWeather(regionId, StopWeather);
		}
		
		/// <summary>
		/// Change Current Weather in Region
		/// </summary>
		/// <param name="regionId">Region ID where weather is changed</param>
		/// <param name="change">Weather Object to change</param>
		/// <returns>true if Weather changed</returns>
		public bool ChangeWeather(ushort regionId, Action<RegionWeather> change)
		{
			ScheduledTask task;
			lock (LockObject)
			{
				if (RegionsTasks.TryGetValue(regionId, out task))
					RegionsTasks.Remove(regionId);
			}
			
			// Stopping Timer is locking on Task Thread
			if (task != null)
				task.Stop();
			
			lock (LockObject)
			{
				RegionWeather weather;
				if (!RegionsWeather.TryGetValue(regionId, out weather))
					return false;
				
				if (RegionsTasks.ContainsKey(regionId))
					return false;
				
				try
				{
					change(weather);
				}
				catch (Exception ex)
				{
					if (log.IsErrorEnabled)
						log.Error("Exception While Changing Weather: ", ex);
				}
				
				// scope copy for thread safety
				var region = regionId;
				
				if (weather.StartTime != 0)
				{
					StartWeather(weather);
					RegionsTasks.Add(region, Scheduler.Start(() => OnWeatherTick(region), weather.Duration));
				}
				else
				{
					RegionsTasks.Add(region, Scheduler.Start(() => OnWeatherTick(region), DefaultTimerInterval));
				}
			}
			
			return true;
		}
		
		#region Update Handlers
		/// <summary>
		/// Stop Weather from given Weather Object
		/// </summary>
		/// <param name="weather">Weather to Stop</param>
		private void StopWeather(RegionWeather weather)
		{
			var weatherCurrentPosition = weather.CurrentPosition(SimpleScheduler.Ticks);
			// if (EventLogWeather && log.IsInfoEnabled)
			// log.InfoFormat("Weather Stopped in Region {0} (ID {1}) CurrentPosition : {2}\n{3}", weather.Region.Description, weather.Region.ID, weatherCurrentPosition, weather);

			weather.Clear();

			foreach (var player in weather.Region.Objects.OfType<GamePlayer>())
			{
				SendWeatherUpdate(weather, player);
				
				if (player.X > weatherCurrentPosition - weather.Width && player.X < weatherCurrentPosition)
					player.Out.SendMessage("The sky clears up again as the storm clouds disperse!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			}
		}
		
		/// <summary>
		/// Start Weather according given Weather Object
		/// </summary>
		/// <param name="weather">Weather to Start</param>
		private void StartWeather(RegionWeather weather)
		{
			if (EventLogWeather && log.IsInfoEnabled)
				log.InfoFormat("Weather Started in Region {0} (ID {1})\n{2}", weather.Region.Description, weather.Region.ID, weather);
			
			foreach (var player in weather.Region.Objects.OfType<GamePlayer>())
				SendWeatherUpdate(weather, player);
		}
		
		/// <summary>
		/// Send Weather Update for this Region's Player
		/// </summary>
		/// <param name="regionId">Region ID of Player</param>
		/// <param name="player">Player</param>
		private void SendWeatherUpdate(ushort regionId, GamePlayer player)
		{
			var current = this[regionId];
			if (current != null)
				SendWeatherUpdate(current, player);
		}
		
		/// <summary>
		/// Send Weather Update to Player
		/// </summary>
		/// <param name="weather">Weather to send</param>
		/// <param name="player">Player Targeted</param>
		private void SendWeatherUpdate(RegionWeather weather, GamePlayer player)
		{
			if (player == null || player.ObjectState != GameObject.eObjectState.Active)
				return;
			
			if (weather.StartTime == 0)
				player.Out.SendWeather(0, 0, 0, 0, 0);
			else
				player.Out.SendWeather(weather.CurrentPosition(SimpleScheduler.Ticks), weather.Width, weather.Speed, weather.FogDiffusion, weather.Intensity);
		}
		#endregion
		
		#region Event Handlers
		
		/// <summary>
		/// Weather Tick happen when Default Timer is Off or When Weather is Finished.
		/// </summary>
		/// <param name="regionId">Region Id of the Weather</param>
		/// <returns>Delay Time for next Tick</returns>
		private int OnWeatherTick(ushort regionId)
		{
			try
			{
				var weather = this[regionId];
				
				if (weather == null)
					return 0;
				
				if (!Util.Chance(DefaultWeatherChance))
				{
					if (weather.StartTime != 0)
						StopWeather(weather);
					
					return DefaultTimerInterval;
				}
				
				weather.CreateWeather(SimpleScheduler.Ticks);
				StartWeather(weather);
				
				return weather.Duration;
			}
			catch (Exception ex)
			{
				if (log.IsErrorEnabled)
					log.Error("Exception in Weather Manager On Tick: ", ex);
				
				return DefaultTimerInterval;
			}
		}
				
		/// <summary>
		/// When Region Start, Register to WeatherManager
		/// </summary>
		private void OnRegionStart(CoreEvent e, object sender, EventArgs arguments)
		{
			var region = sender as Region;
			
			if (region != null && !region.IsDungeon)
				RegisterRegion(region);
		}

		/// <summary>
		/// When Region Stop, unRegister to WeatherManager
		/// </summary>
		private void OnRegionStop(CoreEvent e, object sender, EventArgs arguments)
		{
			var region = sender as Region;
			
			if (region != null && !region.IsDungeon)
				UnRegisterRegion(region);
		}
		
		/// <summary>
		/// When Player Enter Region, Refresh Current Weather
		/// </summary>
		private void OnPlayerEnter(CoreEvent e, object sender, EventArgs arguments)
		{
			var region = sender as Region;
			var args = arguments as RegionPlayerEventArgs;
			
			if (region != null && args != null)
				SendWeatherUpdate(region.ID, args.Player);
		}
		#endregion
		
		#region Registering
		/// <summary>
		/// Register a new Region to Weather Manager
		/// Should not be used Externally
		/// </summary>
		/// <param name="region"></param>
		public void RegisterRegion(Region region)
		{
			lock (LockObject)
			{
				if (!RegionsWeather.ContainsKey(region.ID))
				{
					try
					{
						// scope copy for thread safety
						var regionId = region.ID;

						if (region.Zones.Any())
                        {
							RegionsWeather.Add(regionId, new RegionWeather(region));
							RegionsTasks.Add(regionId, Scheduler.Start(() => OnWeatherTick(regionId), 1));
						}
						
						
					}
					catch (Exception ex)
					{
						if (log.IsErrorEnabled)
							log.ErrorFormat("Error While Registering Region's Weather : {0} (ID:{1})\n{2}", region.Description, region.ID, ex);
					}
				}
				else
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Trying to Add Region {0} (ID:{1}) to WeatherManager while already Registered!", region.Description, region.ID);
				}
			}
		}
		
		/// <summary>
		/// UnRegister a Stopped Region from Weather Manager
		/// Should not be used Externally
		/// </summary>
		/// <param name="region"></param>
		public void UnRegisterRegion(Region region)
		{
			ScheduledTask task;
			lock (LockObject)
			{
				if (RegionsTasks.TryGetValue(region.ID, out task))
					RegionsTasks.Remove(region.ID);
			}
			
			// Stopping Timer is locking on Task Thread
			if (task != null)
				task.Stop();
			
			lock (LockObject)
			{
				RegionWeather weather;
				if (RegionsWeather.TryGetValue(region.ID, out weather))
				{
					RegionsWeather.Remove(region.ID);
					
					if (weather.StartTime != 0)
						StopWeather(weather);
				}
				else
				{
					if (log.IsWarnEnabled)
						log.WarnFormat("Trying to Remove Region {0} (ID:{1}) from WeatherManager but was not registered!", region.Description, region.ID);
				}
			}
		}
		#endregion	
	}
}
