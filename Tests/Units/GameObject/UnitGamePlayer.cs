﻿using Core.GS;
using Core.GS.Enums;
using Core.GS.Players;
using NUnit.Framework;

namespace Core.Tests.Units;

[TestFixture]
class UnitGamePlayer
{
    [SetUp]
    public void Init()
    {
        GameLiving.LoadCalculators();
    }

    [Test]
    public void Constitution_Level50PlayerWith100ConstBaseBuff_Return62()
    {
        var player = NewPlayer();
        player.Level = 50;
        player.BaseBuffBonusCategory[EProperty.Constitution] = 100;

        int actual = player.Constitution;

        int expected = (int)(50 * 1.25);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Constitution_Level50PlayerWith100ConstSpecBuff_Return93()
    {
        var player = NewPlayer();
        player.Level = 50;
        player.SpecBuffBonusCategory[EProperty.Constitution] = 100;

        int actual = player.Constitution;

        int expected = (int)(50 * 1.875);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Constitution_Level50Player100ConstFromItems_Return75()
    {
        var player = NewPlayer();
        player.Level = 50;
        player.ItemBonus[EProperty.Constitution] = 100;

        int actual = player.Constitution;

        int expected = (int)(1.5 * 50);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Intelligence_Level50AnimistWith50AcuityFromItems_Return50()
    {
        var player = NewPlayer(new ClassAnimistBase());
        player.Level = 50;
        player.ItemBonus[EProperty.Acuity] = 50;

        int actual = player.Intelligence;

        Assert.AreEqual(50, actual);
    }

    [Test]
    public void Constitution_Level50Player150ConAnd100MythicalConCap_Return127()
    {
        var player = NewPlayer();
        player.Level = 50;
        player.ItemBonus[EProperty.MythicalConCapBonus] = 100;
        player.ItemBonus[EProperty.Constitution] = 150;

        int actual = player.Constitution;

        Assert.AreEqual(127, actual);
    }

    [Test]
    public void Constitution_Level50PlayerWith5MythicalConCap100ConCap_Return106()
    {
        var player = NewPlayer();
        player.Level = 50;
        player.ItemBonus[EProperty.MythicalConCapBonus] = 5;
        player.ItemBonus[EProperty.ConCapBonus] = 100;
        player.ItemBonus[EProperty.Constitution] = 150;

        int actual = player.Constitution;

        Assert.AreEqual(106, actual);
    }

    [Test]
    public void CalcValue_GetIntelligenceFromLevel50AnimistWith50Acuity_Return50()
    {
        var player = NewPlayer(new ClassAnimistBase());
        player.Level = 50;
        player.BaseBuffBonusCategory[(int)EProperty.Acuity] = 50;

        int actual = player.Intelligence;

        Assert.AreEqual(50, actual);
    }

    [Test]
    public void Intelligence_Level50AnimistWith200AcuityAnd30AcuCapEachFromItems_Return127()
    {
        var player = NewPlayer(new ClassAnimistBase());
        player.Level = 50;
        player.ItemBonus[EProperty.Acuity] = 200;
        player.ItemBonus[EProperty.AcuCapBonus] = 30;
        player.ItemBonus[EProperty.MythicalAcuCapBonus] = 30;

        int actual = player.Intelligence;

        Assert.AreEqual(127, actual);
    }

    [Test]
    public void Intelligence_Level50AnimistWith30AcuityAnd30IntelligenceFromItems_Return60()
    {
        var player = NewPlayer(new ClassAnimistBase());
        player.Level = 50;
        player.ItemBonus[EProperty.Acuity] = 30;
        player.ItemBonus[EProperty.Intelligence] = 30;

        int actual = player.Intelligence;

        Assert.AreEqual(60, actual);
    }

    [Test]
    public void Constitution_Level30AnimistWith200ConAnd20ConCapEachViaItems_Return81()
    {
        var player = NewPlayer(new ClassAnimistBase());
        player.Level = 30;
        player.ItemBonus[EProperty.Constitution] = 200;
        player.ItemBonus[EProperty.ConCapBonus] = 20;
        player.ItemBonus[EProperty.MythicalConCapBonus] = 20;

        int actual = player.Constitution;

        Assert.AreEqual(81, actual);
    }

    private static GamePlayer NewPlayer()
    {
        return GamePlayer.CreateTestableGamePlayer();
    }

    private static GamePlayer NewPlayer(IPlayerClass charClass)
    {
        return GamePlayer.CreateTestableGamePlayer(charClass);
    }
}