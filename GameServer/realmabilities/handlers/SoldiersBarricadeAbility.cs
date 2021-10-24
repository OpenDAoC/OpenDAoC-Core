using System.Collections;
using DOL.GS.Effects;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{

	public class SoldiersBarricadeAbility : TimedRealmAbility
	{
		public SoldiersBarricadeAbility(DBAbility dba, int level) : base(dba, level) { }

        /// [Atlas - Takii] Remove the "BoF/SB don't stack" rule from NF by giving them unique names.
        //public const string BofBaSb = "RA_DAMAGE_DECREASE";
        public const string BofBaSb = "RA_ATLAS_SB";

		int m_range = 1500;
		int m_duration = 30000; // 30s
		int m_value = 0;

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer player = living as GamePlayer;

			/// [Atlas - Takii] We don't want this "does not stack" functionality in OF.
// 			if (player.TempProperties.getProperty(BofBaSb, false))
// 			{
// 				player.Out.SendMessage("You already an effect of that type!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
// 				return;
// 			}

			m_value = GetArmorFactorAmount();
			
			DisableSkill(living);
			ArrayList targets = new ArrayList();
			if (player.Group == null)
				targets.Add(player);
			else
			{
				foreach (GamePlayer grpMate in player.Group.GetPlayersInTheGroup())
				{
					if (player.IsWithinRadius(grpMate, m_range ) && grpMate.IsAlive)
						targets.Add(grpMate);
				}
			}
			bool success;
			foreach (GamePlayer target in targets)
			{
				//send spelleffect
				if (!target.IsAlive) continue;
				success = !target.TempProperties.getProperty(BofBaSb, false);
				foreach (GamePlayer visPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					visPlayer.Out.SendSpellEffectAnimation(player, target, 7015, 0, false, System.Convert.ToByte(success));
				if (success)
					if (target != null)
					{
						new SoldiersBarricadeECSEffect(new ECSGameEffectInitParams(target, m_duration, m_value));
					}
			}

		}

		public override int GetReUseDelay(int level)
		{
			return 600;
		}

		protected virtual int GetArmorFactorAmount()
        {
            if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
                switch (Level)
                {
                    case 1: return 5;
                    case 2: return 10;
                    case 3: return 15;
                    case 4: return 20;
                    case 5: return 30;
                }
            }
            else
            {
                switch (Level)
                {
                    case 1: return 5;
                    case 2: return 15;
                    case 3: return 25;
                }
            }
			return 0;
        }
	}
}
