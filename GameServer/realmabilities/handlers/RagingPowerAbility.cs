using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Raging power, power heal
	/// </summary>
	public class RagingPowerAbility : TimedRealmAbility
	{
		public RagingPowerAbility(DBAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			int heal = GetPowerHealAmount();
			
			int healed = living.ChangeMana(living, eManaChangeType.Spell, living.MaxMana * heal / 100);

			SendCasterSpellEffectAndCastMessage(living, 7009, healed > 0);

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				if (healed > 0) player.Out.SendMessage("You gain " + healed + " power.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				if (heal > healed)
				{
					player.Out.SendMessage("You have full power.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}
			}
			if (healed > 0) DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 600;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				list.Add("Level 1: Value: 25%");
				list.Add("Level 2: Value: 35%");
				list.Add("Level 3: Value: 50%");
				list.Add("Level 4: Value: 65%");
				list.Add("Level 5: Value: 80%");
				list.Add("");
				list.Add("Target: Self");
				list.Add("Casting time: instant");
			}
			else
			{
				list.Add("Level 1: Value: 25%");
				list.Add("Level 2: Value: 60%");
				list.Add("Level 3: Value: 100%");
				list.Add("");
				list.Add("Target: Self");
				list.Add("Casting time: instant");
			}
		}

		protected virtual int GetPowerHealAmount()
        {
            if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
                switch (Level)
                {
                    case 1: return 25;
                    case 2: return 35;
                    case 3: return 50;
                    case 4: return 65;
                    case 5: return 80;
                }
            }
            else
            {
                switch (Level)
                {
                    case 1: return 25;
                    case 2: return 60;
                    case 3: return 100;
                }
            }
			return 0;
        }
	}
}