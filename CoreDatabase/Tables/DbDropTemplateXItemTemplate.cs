using Core.Database.Attributes;

namespace Core.Database
{
	/// <summary>
	/// 
	/// </summary>
	[DataTable(TableName = "DropTemplateXItemTemplate")]
	public class DbDropTemplateXItemTemplate : DbLootTemplate
	{
		public DbDropTemplateXItemTemplate()
		{
		}

		[PrimaryKey(AutoIncrement = true)]
		public long ID { get; set; }
	}
}
