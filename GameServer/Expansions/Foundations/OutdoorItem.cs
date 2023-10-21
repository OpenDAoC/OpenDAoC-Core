using DOL.Database;

namespace DOL.GS.Housing;

public class OutdoorItem
{
	public OutdoorItem()
	{}

	public OutdoorItem(DbHouseOutdoorItem dbitem)
	{
		Model = dbitem.Model;
		Position = dbitem.Position;
		Rotation = dbitem.Rotation;
		BaseItem = GameServer.Database.FindObjectByKey<DbItemTemplate>(dbitem.BaseItemID);
		DatabaseItem = dbitem;
	}

	public int Model { get; set; }

	public int Position { get; set; }

	public int Rotation { get; set; }

	public DbItemTemplate BaseItem { get; set; }

	public DbHouseOutdoorItem DatabaseItem { get; set; }

	public DbHouseOutdoorItem CreateDBOutdoorItem(int houseNumber)
	{
		var dbitem = new DbHouseOutdoorItem
			            {
			             	HouseNumber = houseNumber,
			             	Model = Model,
			             	Position = Position,
			             	BaseItemID = BaseItem.Id_nb,
			             	Rotation = Rotation
			            };

		return dbitem;
	}
}