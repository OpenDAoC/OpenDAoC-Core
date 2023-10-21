namespace Core.GS
{
	/// <summary>
	/// Special NPC for giving DR players items
	/// </summary>
	public class RoyalTreasuryClerk : GameNpc
	{
		private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Interact with the NPC.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player) || player == null)
				return false;

			
			SayTo(player, $"Hello {player.PlayerClass.Name}, you can come to me if you lost your Personal Bind Recall Stone.\n");

			if (player.Inventory.CountItemTemplate("Personal_Bind_Recall_Stone", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv) == 0)
			{
				SayTo(player, "It looks like you need my service.  Do you need [another] Personal Bind Recall Stone?");
			}

			return true;
		}

		/// <summary>
		/// Talk to the NPC.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="str"></param>
		/// <returns></returns>
		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text) || !(source is GamePlayer))
				return false;

			GamePlayer player = source as GamePlayer;

			if (text.ToLower() == "another")
			{
				if (player.Inventory.CountItemTemplate("Personal_Bind_Recall_Stone", EInventorySlot.Min_Inv, EInventorySlot.Max_Inv) == 0)
				{
					SayTo(player, "Very well then, here's your Personal Bind Recall Stone, may it serve you well.");
					player.ReceiveItem(this, "Personal_Bind_Recall_Stone");
				}
				return true;
			}

			return true;
		}
	}
}
