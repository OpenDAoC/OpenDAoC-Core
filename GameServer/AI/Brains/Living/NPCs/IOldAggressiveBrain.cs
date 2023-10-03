using DOL.GS;

namespace DOL.AI.Brain
{
	public interface IOldAggressiveBrain
	{
		/// <summary>
		/// Aggressive Level in % 0..100, 0 means not Aggressive
		/// </summary>
		int AggroLevel { get; set; }

		/// <summary>
		/// Range in that this npc aggros
		/// </summary>
		int AggroRange { get; set; }

		/// <summary>
		/// Add living to the aggrolist
		/// aggroamount can be negative to lower amount of aggro
		/// </summary>
		void AddToAggroList(GameLiving living, int aggroamount);

		/// <summary>
		/// Get current amount of aggro on aggrotable
		/// </summary>
		long GetAggroAmountForLiving(GameLiving living);

		/// <summary>
		/// Remove one living from aggro list
		/// </summary>
		void RemoveFromAggroList(GameLiving living);

		/// <summary>
		/// Remove all livings from the aggrolist
		/// </summary>
		void ClearAggroList();

		/// <summary>
		/// Check if this npc has a high enough aggro level to aggro
		/// </summary>
		bool CanAggroTarget(GameLiving target);
	}
}