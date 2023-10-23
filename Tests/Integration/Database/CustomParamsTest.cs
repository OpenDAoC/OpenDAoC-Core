using System;
using System.Linq;
using Core.Database;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class CustomParamsTest
{
	public CustomParamsTest()
	{
		Database = DatabaseSetUp.Database;
	}
	
	protected SqlObjectDatabase Database { get; set; }
	
	[Test]
	public void TableParamSaveLoadTest()
	{
		Database.RegisterDataObject(typeof(TableCustomParams));
		Database.RegisterDataObject(typeof(TableWithCustomParams));
		
		var TestData = new TableWithCustomParams();
		TestData.TestValue = "NUnitTest";
		TestData.CustomParams = new [] { new TableCustomParams(TestData.TestValue, "TestParam", Convert.ToString(true)) };
		
		// Cleanup
		var Cleanup = Database.SelectAllObjects<TableWithCustomParams>();
		foreach (var obj in Cleanup)
			Database.DeleteObject(obj);
		
		// Check Dynamic object is not Persisted
		Assert.IsFalse(TestData.IsPersisted, "Newly Created Data Object should not be persisted...");
		Assert.IsFalse(TestData.CustomParams.First().IsPersisted, "Newly Created Param Object should not be persisted...");
		
		// Insert Object
		var paramsInserted = TestData.CustomParams.Select(o => Database.AddObject(o)).ToArray();
		var inserted = Database.AddObject(TestData);
		
		Assert.IsTrue(inserted, "Test Object not inserted properly in Database !");
		Assert.IsTrue(paramsInserted.All(result => result), "Params Objects not inserted properly in Database !");
		
		// Check Saved Object is Persisted
		Assert.IsTrue(TestData.IsPersisted, "Newly Created Data Object should be persisted...");
		Assert.IsTrue(TestData.CustomParams.First().IsPersisted, "Newly Created Param Object should be persisted...");

		// Retrieve Object From Database
		var RetrieveData = Database.FindObjectByKey<TableWithCustomParams>(TestData.ObjectId);
		
		// Check Retrieved object is Persisted
		Assert.IsTrue(RetrieveData.IsPersisted, "Retrieved Data Object should be persisted...");
		Assert.IsTrue(RetrieveData.CustomParams.First().IsPersisted, "Retrieved Param Object should be persisted...");
		
		// Compare both Objects
		Assert.AreEqual(TestData.ObjectId, RetrieveData.ObjectId, "Newly Created and Inserted Data Object should have the same ID than Retrieved Object.");
		
		Assert.AreEqual(TestData.CustomParams.Length,
		                RetrieveData.CustomParams.Length,
		                "Saved Object and Retrieved Object doesn't have the same amount of Custom Params");
		
		Assert.AreEqual(TestData.CustomParams.First(param => param.KeyName == "TestParam").Value,
		                RetrieveData.CustomParams.First(param => param.KeyName == "TestParam").Value,
		               "Both Saved Object and Retrieved Object should have similar Custom Params...");
	}
}