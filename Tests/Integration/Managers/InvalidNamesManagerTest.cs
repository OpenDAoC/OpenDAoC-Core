﻿using Core.GS.Players;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class TestInvalidNamesManager
{
	public TestInvalidNamesManager()
	{
	}

	[Test]
	public void InvalidNamesManager_LoadContainsAndCheckOneString_Match()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "Bob" } );
		
		Assert.IsTrue(manager["bob"], "'bob' should match 'Bob' Pattern with case insensitive...");
		Assert.IsTrue(manager["boba fett"], "'boba fett' should match 'Bob' Pattern with case insensitive...");
		Assert.IsTrue(manager["FOOBobBAR"], "'FOOBobBAR' should match 'Bob' Pattern with case insensitive...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadContainsAndCheckOneString_NoMatch()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "Bob" } );
		
		Assert.IsFalse(manager["john"], "'john' should not match 'Bob' Pattern...");
		Assert.IsFalse(manager["han solo"], "'han solo' not should match 'Bob' Pattern...");
		Assert.IsFalse(manager["FOOBAR"], "'FOOBAR' should not match 'Bob' Pattern...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadContainsAndCheckTwoString_Match()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "Bob" } );
		
		Assert.IsTrue(manager["bo", "b"], "'bo' 'b' should match 'Bob' Pattern with case insensitive...");
		Assert.IsTrue(manager["boba", "fett"], "'boba' 'fett' should match 'Bob' Pattern with case insensitive...");
		Assert.IsTrue(manager["FOOB", "obBAR"], "'FOOB' 'obBAR' should match 'Bob' Pattern with case insensitive...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadContainsAndCheckTwoString_NoMatch()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "Bob" } );
		
		Assert.IsFalse(manager["jo", "hn"], "'jo' 'hn' should not match 'Bob' Pattern...");
		Assert.IsFalse(manager["han", "solo"], "'han' 'solo' not should match 'Bob' Pattern...");
		Assert.IsFalse(manager["FO", "OBAR"], "'FO' 'OBAR' should not match 'Bob' Pattern...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadRegexAndCheckOneString_Match()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/" } );
		
		Assert.IsTrue(manager["bob"], "'bob' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["boba fett"], "'boba fett' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["FOOBobBAR"], "'FOOBobBAR' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["F O O B o b B A R"], "'F O O B o b B A R' should match '/B.*o.*b/' Pattern with case insensitive...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadRegexAndCheckOneString_NoMatch()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/" } );
		
		Assert.IsFalse(manager["john"], "'john' should not match '/B.*o.*b/' Pattern...");
		Assert.IsFalse(manager["han solo"], "'han solo' not should match '/B.*o.*b/' Pattern...");
		Assert.IsFalse(manager["FOOBAR"], "'FOOBAR' should not match '/B.*o.*b/' Pattern...");
		Assert.IsFalse(manager["F O O B A R"], "'F O O B A R' should not match '/B.*o.*b/' Pattern...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadRegexAndCheckTwoString_Match()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/" } );
		
		Assert.IsTrue(manager["bo", "b"], "'bo' 'b' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["boba", "fett"], "'boba' 'fett' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["FOOB", "obBAR"], "'FOOB' 'obBAR' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["F O O", " B o b B A R"], "'F O O' ' B o b B A R' should match '/B.*o.*b/' Pattern with case insensitive...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadRegexAndCheckTwoString_NoMatch()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/" } );
		
		Assert.IsFalse(manager["jo", "hn"], "'jo' 'hn' should not match '/B.*o.*b/' Pattern...");
		Assert.IsFalse(manager["han", "solo"], "'han' 'solo' not should match '/B.*o.*b/' Pattern...");
		Assert.IsFalse(manager["FOOB", "AR"], "'FOOB' 'AR' should not match '/B.*o.*b/' Pattern...");
		Assert.IsFalse(manager["F O O", " B A R"], "'F O O' ' B A R' should not match '/B.*o.*b/' Pattern...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadMultiConstraintsCheckOneString_Match()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/", "/B.*a.*r/", "FOO", "Luke" } );
		
		Assert.IsTrue(manager["bar"], "'bar' should match '/B.*a.*r/' Pattern with case insensitive...");
		Assert.IsTrue(manager["boba fett"], "'boba fett' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["FOOBobBAR"], "'FOOBobBAR' should match 'FOO' Pattern with case insensitive...");
		Assert.IsTrue(manager["LukeSkywalker"], "'LukeSkywalker' should match 'Luke' Pattern with case insensitive...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadMultiConstraintsCheckOneString_NoMatch()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/", "/B.*a.*r/", "FOO", "Luke" } );
		
		Assert.IsFalse(manager["john"], "'john' should not match any Pattern...");
		Assert.IsFalse(manager["han solo"], "'han solo' should not match any Pattern...");
		Assert.IsFalse(manager["FARBABBoo"], "'FARBABBoo' should not match any Pattern...");
		Assert.IsFalse(manager["AnakinSkywalker"], "'AnakinSkywalker' should not match any Pattern...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadMultiConstraintsCheckTwoString_Match()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/", "/B.*a.*r/", "FOO", "Luke" } );
		
		Assert.IsTrue(manager["ba", "r"], "'ba' 'r' should match '/B.*a.*r/' Pattern with case insensitive...");
		Assert.IsTrue(manager["boba", "fett"], "'boba' 'fett' should match '/B.*o.*b/' Pattern with case insensitive...");
		Assert.IsTrue(manager["FOOB", "obBAR"], "'FOOB' 'obBAR' should match 'FOO' Pattern with case insensitive...");
		Assert.IsTrue(manager["LukeSky", " walker"], "'LukeSky' ' walker' should match 'Luke' Pattern with case insensitive...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadMultiConstraintsCheckTwoString_NoMatch()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "/B.*o.*b/", "/B.*a.*r/", "FOO", "Luke" } );
		
		Assert.IsFalse(manager["jo", "hn"], "'jo' 'hn' should not match any Pattern...");
		Assert.IsFalse(manager["han", "solo"], "'han' 'solo' should not match any Pattern...");
		Assert.IsFalse(manager["FARB", "ABBoo"], "'FARB' 'ABBoo' should not match any Pattern...");
		Assert.IsFalse(manager["AnakinSky", " walker"], "'AnakinSky' ' walker' should not match any Pattern...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadCommentCheckOneString_NoMatch()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "BA #R", "#Luke", "bob" } );
		
		Assert.IsFalse(manager["Luke"], "'Luke' should not match any Pattern...");
		Assert.IsFalse(manager["B AR"], "'B AR' should not match any Pattern...");
		Assert.IsFalse(manager["han solo"], "'han solo' should not match any Pattern...");
	}
	
	[Test]
	public void InvalidNamesManager_LoadCommentCheckOneString_Match()
	{
		var manager = new InvalidNamesMgr(string.Empty);
		manager.LoadFromLines(new [] { "BA #R", "#Luke", "bob" } );
		
		Assert.IsTrue(manager["Bob"], "'Bob' should match 'Bob' Pattern...");
		Assert.IsTrue(manager["BA F"], "'BA F' should match 'BA' Pattern...");
		Assert.IsTrue(manager["bobaFett"], "'boba' should match 'BA' Pattern...");
	}
}