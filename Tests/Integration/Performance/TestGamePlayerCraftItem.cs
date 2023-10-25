using System.Diagnostics;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.Tests.Units;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture, Explicit]
class TestGamePlayerCraftItem
{
    private static ushort itemToCraftID = 1;

    [OneTimeSetUp]
    public void SetupFakeServer()
    {
        var sqliteDB = Create.TemporarySQLiteDB();
        sqliteDB.RegisterDataObject(typeof(DbCraftedItem));
        sqliteDB.RegisterDataObject(typeof(DbCraftedXItem));
        sqliteDB.RegisterDataObject(typeof(DbItemTemplate));

        var fakeServer = new FakeServer();
        fakeServer.SetDatabase(sqliteDB);
        GameServer.LoadTestDouble(fakeServer);

        AddOneCompleteRecipeToDatabase();
    }

    [Test]
    public void CraftItem_RecipeIsComplete_AThousandTimes_()
    {
        var player = new UnitFakePlayer();
        var repetitions = 1000;

        Stopwatch sw = Stopwatch.StartNew();
        for (int index = 0; index < repetitions; index++)
        {
            player.CraftItem(itemToCraftID);
        }
        var duration = sw.ElapsedMilliseconds;
        sw.Stop();
        Assert.Warn($"{repetitions} executions of GamePlayer.CraftItem took {duration} ms.");
    }

    private static DbCraftedItem AddOneCompleteRecipeToDatabase()
    {
        var itemToCraft = new DbItemTemplate();
        itemToCraft.Id_nb = "item_to_craft";
        itemToCraft.Name = "Item To Craft";
        itemToCraft.AllowedClasses = "";
        itemToCraft.CanUseEvery = 0;
        AddDatabaseEntry(itemToCraft);

        var craftedItem = new DbCraftedItem();
        craftedItem.CraftedItemID = itemToCraftID.ToString();
        craftedItem.Id_nb = itemToCraft.Id_nb;
        craftedItem.CraftingLevel = 1;
        craftedItem.CraftingSkillType = 1;
        AddDatabaseEntry(craftedItem);

        var ingredient1 = new DbCraftedXItem();
        ingredient1.Count = 1;
        ingredient1.ObjectId = "id1";
        ingredient1.CraftedItemId_nb = craftedItem.Id_nb;
        ingredient1.IngredientId_nb = "item1_id";
        AddDatabaseEntry(ingredient1);
        var ingredient2 = new DbCraftedXItem();
        ingredient2.Count = 2;
        ingredient2.ObjectId = "id2";
        ingredient2.CraftedItemId_nb = craftedItem.Id_nb;
        ingredient2.IngredientId_nb = "item2_id";
        AddDatabaseEntry(ingredient2);

        var ingredientItem1 = new DbItemTemplate();
        ingredientItem1.Id_nb = ingredient1.IngredientId_nb;
        ingredientItem1.Name = "First Ingredient Name";
        ingredientItem1.AllowedClasses = "";
        ingredientItem1.Price = 10000;
        ingredientItem1.CanUseEvery = 0;
        AddDatabaseEntry(ingredientItem1);
        var ingredientItem2 = new DbItemTemplate();
        ingredientItem2.Id_nb = ingredient2.IngredientId_nb;
        ingredientItem2.Name = "Second Ingredient Name";
        ingredientItem2.AllowedClasses = "";
        ingredientItem2.CanUseEvery = 0;
        ingredientItem2.Price = 20000;
        AddDatabaseEntry(ingredientItem2);

        return craftedItem;
    }

    private static void AddDatabaseEntry(DataObject dataObject)
    {
        dataObject.AllowAdd = true;
        GameServer.Database.AddObject(dataObject);
    }
}