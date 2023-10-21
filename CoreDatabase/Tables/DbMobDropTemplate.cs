using Core.Database.Attributes;

namespace Core.Database
{
	/// <summary>
	/// Database Storage of Mob DropTemplate Relation
	/// </summary>
	[DataTable(TableName = "MobDropTemplate")]
	public class DbMobDropTemplate : DbMobXLootTemplate
	{
		public DbMobDropTemplate()
		{
		}

		[PrimaryKey(AutoIncrement = true)]
		public long ID { get; set; }
	}
}
