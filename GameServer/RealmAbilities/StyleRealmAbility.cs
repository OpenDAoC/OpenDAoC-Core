using Core.Database;
using Core.Database.Tables;
using Core.GS.Styles;

namespace Core.GS.RealmAbilities
{
	public abstract class StyleRealmAbility : TimedRealmAbility
	{
		public Style StyleToUse;

		public StyleRealmAbility(DbAbility ability, int level) : base(ability, level)
		{
			StyleToUse = CreateStyle();
		}

		public override int MaxLevel
		{
			get { return 1; }
		}

		public override int CostForUpgrade(int currentLevel)
		{
			return 10;
		}
		
		public override int GetReUseDelay(int level) { return 600; } // 10 mins

		public override void DisableSkill(GameLiving living)
		{
			StyleComponent styleComponent = living.styleComponent;

			// Remove RA styles from the backup slot so that it doesn't fire twice.
			if (styleComponent.NextCombatBackupStyle == StyleToUse)
				styleComponent.NextCombatBackupStyle = null;

			base.DisableSkill(living);
		}

		public override void Execute(GameLiving living)
		{
			StyleProcessor.TryToUseStyle(living, StyleToUse);
			base.Execute(living);
		}

		protected abstract Style CreateStyle();
	}
}