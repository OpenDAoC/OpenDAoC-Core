using Core.Database;
using Core.Database.Tables;
using Core.GS.Packets;

namespace Core.GS.RealmAbilities
{
	/// <summary>
	/// Base for all Realm Abilities
	/// </summary>
	public class RealmAbility : Ability
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RealmAbility(DbAbility ability, int level) : base(ability, level) { }

		public virtual int CostForUpgrade(int currentLevel)
		{
			return 1000;
		}

		/// <summary>
		/// true if player can immediately use that ability
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public virtual bool CheckRequirement(GamePlayer player)
		{
			return true;
		}

		/// <summary>
		/// max level this RA can reach
		/// </summary>
		public virtual int MaxLevel
		{
			get { return 0; }
		}

		/// <summary>
		/// Delve for this RA
		/// </summary>
		/// <param name="w"></param>
		public virtual void AddDelve(ref MiniDelveWriter w)
		{
			w.AddKeyValuePair("Name", Name);
			if (Icon > 0)
				w.AddKeyValuePair("icon", Icon);

			for (int i = 0; i <= MaxLevel - 1; i++)
			{
				if (CostForUpgrade(i) > 0)
					w.AddKeyValuePair(string.Format("TrainingCost_{0}", (i + 1)), CostForUpgrade(i));
			}
		}

		public override string Name
		{
			get
			{
				//Lifeflight: Right after a RA is trained the m_name already contains the roman numerals
				//So check to see if it ends with the correct RomanLevel, and if so just return m_name
				if (m_name.EndsWith(getRomanLevel()))
					return m_name;
				else
					return (Level <= 1) ? base.Name : m_name + " " + getRomanLevel();
			}
		}
		
		public override eSkillPage SkillType
		{
			get
			{
				return eSkillPage.RealmAbilities;
			}
		}
	}
}