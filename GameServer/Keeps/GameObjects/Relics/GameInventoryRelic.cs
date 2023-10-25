using System.Reflection;
using Core.Database.Tables;
using log4net;

namespace Core.GS.Keeps;

/// <summary>
/// This class represents a relic in a players inventory
/// </summary>
public class GameInventoryRelic : GameInventoryItem
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public GameInventoryRelic()
		: base()
	{
	}

	public GameInventoryRelic(DbItemTemplate template)
		: base(template)
	{
	}

	public GameInventoryRelic(DbItemUnique template)
		: base(template)
	{
	}

	public GameInventoryRelic(DbInventoryItem item)
		: base(item)
	{
		OwnerID = item.OwnerID;
		ObjectId = item.ObjectId;
	}

	/// <summary>
	/// Can this item be saved or loaded from the database?
	/// </summary>
	public override bool CanPersist
	{
		get { return false; } // relics can never be saved
	}
}