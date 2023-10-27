﻿using System;
using Core.GS.Scheduler;
using Core.GS.Server;
using Core.GS.World;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class WeatherManagerTest
{
	public WeatherManagerTest()
	{
	}

	[OneTimeSetUp]
	public void SetUp()
	{
		log4net.Config.BasicConfigurator.Configure(
			new log4net.Appender.ConsoleAppender
			{
				Layout = new log4net.Layout.SimpleLayout()
			});
	}

	[OneTimeTearDown]
	public void TearDown()
	{
		log4net.LogManager.Shutdown();
	}

	Region FakeRegion()
	{
		ServerProperty.DISABLED_REGIONS = string.Empty;
		ServerProperty.DISABLED_EXPANSIONS = string.Empty;
		var region = Region.Create(new RegionData { Id = 1 });
		region.Zones.Add(new Zone(region, 1, string.Empty, 0, 0, 65535, 65535, 1, false, 0, false, 0, 0, 0, 0, 1));
		return region;
	}

	[Test]
	public void WeatherManager_GetNonExistentRegion_ReturnNull()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		Assert.IsNull(weatherMgr[1]);
	}

	[Test]
	public void WeatherManager_ChangeWeatherNonExistentRegion_ReturnFalse()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		Assert.IsFalse(weatherMgr.ChangeWeather(1, weather => weather.Clear()));
	}

	[Test]
	public void WeatherManager_RegisterRegion_ReturnRegion()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);

		Assert.AreEqual(weatherMgr[1].Region, region);
	}

	[Test]
	public void WeatherManager_UnRegisterRegion_ReturnNull()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);
		weatherMgr.UnRegisterRegion(region);

		Assert.IsNull(weatherMgr[1]);
	}

	[Test]
	public void WeatherManager_StartWeatherRegion_ReturnTrue()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);

		Assert.IsTrue(weatherMgr.StartWeather(1));
	}

	[Test]
	public void WeatherManager_StopWeatherRegion_ReturnTrue()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);

		weatherMgr.StartWeather(1);
		Assert.IsTrue(weatherMgr.StopWeather(1));
	}

	[Test]
	public void WeatherManager_StartWeatherRegion_StartTime()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);

		weatherMgr.StartWeather(1);
		Assert.Greater(weatherMgr[1].StartTime, 0);
	}

	[Test]
	public void WeatherManager_StopWeatherRegion_StartTime()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);

		weatherMgr.StartWeather(1);
		weatherMgr.StopWeather(1);
		Assert.AreEqual(0, weatherMgr[1].StartTime);
	}

	[Test]
	public void WeatherManager_ChangeWeatherRegion_WeatherEqual()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);

		weatherMgr.ChangeWeather(1, weather => weather.CreateWeather(65000, 300, 100, 16000, 0));

		Assert.AreEqual((65535 + 65000) / 300, weatherMgr[1].Duration / 1000);
		Assert.AreEqual(65000, weatherMgr[1].Width);
	}

	[Test]
	public void WeatherManager_ChangeWeatherRegionException_WeatherEqual()
	{
		var weatherMgr = new WeatherMgr(new SimpleScheduler());

		var region = FakeRegion();
		weatherMgr.RegisterRegion(region);

		weatherMgr.ChangeWeather(1, weather =>
		{
			weather.CreateWeather(65000, 300, 100, 16000, 0);
			throw new Exception();
		});

		Assert.AreEqual((65535 + 65000) / 300, weatherMgr[1].Duration / 1000);
		Assert.AreEqual(65000, weatherMgr[1].Width);
	}

	[Test]
	public void WeatherRegion_InitWeather_MinMaxEqualZone()
	{
		var region = FakeRegion();
		var weather = new RegionWeather(region);
		weather.CreateWeather(65000, 300, 100, 16000, 0);

		var duration = (65535 + 65000) * 1000 / 300;

		Assert.AreEqual(65535 + 65000, weather.CurrentPosition(duration));
		Assert.AreEqual(0, weather.CurrentPosition(0));
	}
}