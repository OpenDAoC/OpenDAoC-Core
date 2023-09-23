using DOL.Database.Attributes;

namespace DOL.Database
{
	/// <summary>
	/// Database Storage of Mob DropTemplate Relation
	/// </summary>
	[DataTable(TableName = "MobDropTemplate")]
	public class DbMobDropTemplates : DbMobXLootTemplate
	{
		public DbMobDropTemplates()
		{
		}

		[PrimaryKey(AutoIncrement = true)]
		public long ID { get; set; }
	}
}
