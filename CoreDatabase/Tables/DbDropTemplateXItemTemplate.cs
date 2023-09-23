using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// 
	/// </summary>
	[DataTable(TableName = "DropTemplateXItemTemplate")]
	public class DbDropTemplateXItemTemplate : DbLootTemplates
	{
		public DbDropTemplateXItemTemplate()
		{
		}

		[PrimaryKey(AutoIncrement = true)]
		public long ID { get; set; }
	}
}
