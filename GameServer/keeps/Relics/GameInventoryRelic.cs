using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS
{
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

		/// <summary>
		/// Can this item be saved or loaded from the database?
		/// </summary>
		public override bool CanPersist
		{
			get { return false; } // relics can never be saved
		}
	}
}
