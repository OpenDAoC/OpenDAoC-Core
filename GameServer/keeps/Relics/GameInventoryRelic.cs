using System.Reflection;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// This class represents a relic in a players inventory
	/// </summary>
	public class GameInventoryRelic : GameInventoryItem
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

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

		/// <summary>
		/// Can this item be saved or loaded from the database?
		/// </summary>
		public override bool CanPersist
		{
			get { return false; } // relics can never be saved
		}
	}
}
