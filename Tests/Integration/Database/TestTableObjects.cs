using System;
using Core.Database;

namespace Core.Tests.Integration;

/// <summary>
/// Test Table with Multiple Overlapping Index
/// </summary>
[DataTable(TableName = "Test_TableWithMultiIndexes")]
public class TestTableWithMultiIndexes : DataObject
{
	[DataElement(IndexColumns = "Index2")]
	public string Index1 { get; set; }
	[DataElement(IndexColumns = "Index3")]
	public string Index2 { get; set; }
	[DataElement]
	public string Index3 { get; set; }
}

/// <summary>
/// Test Table Migration with No Primary Key
/// </summary>
[DataTable(TableName = "Test_TableMigrationNoPrimary")]
public class TestTableWithNoPrimaryV1 : DataObject
{
	[DataElement]
	public string Value { get; set; }
}

/// <summary>
/// Test Table Migration To Auto Increment Primary Key
/// </summary>
[DataTable(TableName = "Test_TableMigrationNoPrimary")]
public class TestTableWithNoPrimaryV2 : DataObject
{
	[PrimaryKey(AutoIncrement = true)]
	public int PrimaryKey { get; set; }
	[DataElement]
	public string Value { get; set; }
}

/// <summary>
/// Test Table Migration To Auto Increment Primary Key changing name
/// </summary>
[DataTable(TableName = "Test_TableMigrationNoPrimary")]
public class TestTableWithNoPrimaryV3 : DataObject
{
	[PrimaryKey(AutoIncrement = true)]
	public int PrimaryKey2 { get; set; }
	[DataElement]
	public string Value { get; set; }
}

/// <summary>
/// Test Table Migration Changing Primary Key type and column
/// </summary>
[DataTable(TableName = "Test_TableWithPrimaryChanging")]
public class TestTableWithPrimaryChangingV1 : DataObject
{
	[PrimaryKey]
	public int PrimaryKey { get; set; }
	[DataElement]
	public string Value { get; set; }
}

/// <summary>
/// Test Table Migration Changing Primary Key type and column
/// </summary>
[DataTable(TableName = "Test_TableWithPrimaryChanging")]
public class TestTableWithPrimaryChangingV2 : DataObject
{
	[DataElement]
	public long PrimaryKey { get; set; }
	[PrimaryKey]
	public string Value { get; set; }
}
/// <summary>
/// Test Table Migration Broken Primary Key
/// </summary>
[DataTable(TableName = "Test_TableWithBrokenPrimary")]
public class TestTableWithBrokenPrimaryV1 : DataObject
{
	[DataElement(Unique = true, AllowDbNull = false)]
	public int PrimaryKey { get; set; }
	[DataElement]
	public string Value { get; set; }
}

/// <summary>
/// Test Table Migration Broken Primary Key
/// </summary>
[DataTable(TableName = "Test_TableWithBrokenPrimary")]
public class TestTableWithBrokenPrimaryV2 : DataObject
{
	[PrimaryKey]
	public long PrimaryKey { get; set; }
	[DataElement]
	public string Value { get; set; }
}

/// <summary>
/// Test Table Migration with different Types
/// </summary>
[DataTable(TableName = "Test_TableMigrationTypes")]
public class TestTableDifferentTypesV1 : DataObject
{
	[DataElement(Varchar = 100)]
	public string StringValue { get; set; }
	[DataElement]
	public int IntValue { get; set; }
	[DataElement]
	public DateTime DateValue { get; set; }
}

/// <summary>
/// Test Table Migration with different Types
/// </summary>
[DataTable(TableName = "Test_TableMigrationTypes")]
public class TestTableDifferentTypesV2 : DataObject
{
    [DataElement]
    public string StringValue { get; set; }
    [DataElement]
    public byte IntValue { get; set; }
    [DataElement]
    public string DateValue { get; set; }
}

/// <summary>
/// Test Table Migration From null to non-null
/// </summary>
[DataTable(TableName = "Test_TableMigrationNull")]
public class TestTableMigrationNullToNonNull : DataObject
{
    [DataElement(AllowDbNull = true)]
    public string StringValue { get; set; }
}

/// <summary>
/// Test Table Migration From null to non-null
/// </summary>
[DataTable(TableName = "Test_TableMigrationNull")]
public class TestTableMigrationNullFromNull : DataObject
{
    [DataElement(AllowDbNull = false)]        
    public string StringValue { get; set; }
    
    [DataElement(AllowDbNull = false)]
    public int IntValue { get; set; }
}