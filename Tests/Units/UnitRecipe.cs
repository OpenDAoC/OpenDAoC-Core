using NUnit.Framework;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Crafting;
using Core.GS.Server;

namespace Core.Tests.Units;

[TestFixture]
class UnitRecipe
{
    [OneTimeSetUp]
    public void SetupServer()
    {
        FakeServer.Load();
    }

    [Test]
    public void GetIngredientCosts_OneIngredientWithPrice2_2()
    {
        var item = new DbItemTemplate();
        item.Price = 2;
        var ingredient = new IngredientDb(1, item);
        var recipe = new RecipeMgr(null, new List<IngredientDb>() { ingredient});

        var actual = recipe.CostToCraft;

        var expected = 2;
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GetIngredientCosts_OneIngredientWithPrice2AndOneWithPrice4_6()
    {
        var item = new DbItemTemplate() { Price = 2 };
        var ingredient1 = new IngredientDb(1, item);
        var ingredient2 = new IngredientDb(2, item);
        var recipe = new RecipeMgr(null, new List<IngredientDb>() { ingredient1, ingredient2 });

        var actual = recipe.CostToCraft;

        var expected = 6;
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void SetRecommendedProductPriceInDB_ProductWithPrice2AndNoIngredients_ProductPriceIs2()
    {
        var product = new DbItemTemplate() { Price = 2 };
        var ingredients = new List<IngredientDb>() { };
        var recipe = new RecipeMgr(product, ingredients);

        recipe.SetRecommendedProductPriceInDB();

        var actual = product.Price;
        var expected = 2;
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void SetRecommendedProductPriceInDB_ProductWithPrice2AndIngredientCostIs100_ProductPriceIs190()
    {
        var product = new DbItemTemplate() { Price = 2 };
        var count = 1;
        var material = new DbItemTemplate() { Price = 100 };
        var ingredients = new List<IngredientDb>() { new IngredientDb(count, material) };
        var recipe = new RecipeMgr(product, ingredients);
        ServerProperty.CRAFTING_SELLBACK_PERCENT = 95;

        recipe.SetRecommendedProductPriceInDB();

        var actual = product.Price;
        var expected = 95 * 2;
        Assert.AreEqual(expected, actual);
    }
}

[TestFixture]
public class UT_Ingredient
{
    [Test]
    public void Cost_CountIsOneItemPriceIsOne_One()
    {
        var item = new DbItemTemplate() { Price = 1 };
        var ingredient = new IngredientDb(1, item);

        var actual = ingredient.Cost;

        var expected = 1;
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void Cost_2ItemsWithPriceOne_2()
    {
        var item = new DbItemTemplate() { Price = 1 };
        var ingredient = new IngredientDb(2, item);

        var actual = ingredient.Cost;

        var expected = 2;
        Assert.AreEqual(expected, actual);
    }
}