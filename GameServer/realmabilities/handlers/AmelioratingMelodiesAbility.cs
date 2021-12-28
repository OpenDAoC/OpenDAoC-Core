using System;
using System.Collections;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;
using System.Collections.Generic;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Ameliorating Melodies realm ability
	/// </summary>
	public class AmelioratingMelodiesAbility : TimedRealmAbility
	{
		private const int m_duration = 30000; // 30s
		
		/// <summary>
		/// Constructs the Ameliorating Melodies handler
		/// </summary>
		public AmelioratingMelodiesAbility(DBAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			GamePlayer player = living as GamePlayer;
			if (player == null) return;
			if (CheckPreconditions(living, DEAD | SITTING | STUNNED | MEZZED )) return;

			EffectListService.TryCancelFirstEffectOfTypeOnTarget(player, eEffect.AmelioratingMelodies);

			SendCasterSpellEffectAndCastMessage(living, 3021, true);

			int heal = GetHealAmountPerTick();

			new AmelioratingMelodiesECSEffect(new ECSGameEffectInitParams(player, 30000, heal));

			DisableSkill(living);
		}

		/// <summary>
		/// Returns the re-use delay of the ability
		/// </summary>
		/// <param name="level">Level of the ability</param>
		/// <returns>Delay in seconds</returns>
		public override int GetReUseDelay(int level)
		{
			return 900;
		}

		/// <summary>
		/// Delve information
		/// </summary>
		public override void AddEffectsInfo(IList<string> list)
		{
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				list.Add("Level 1: Heals 100 / tick");
				list.Add("Level 2: Heals 175 / tick");
				list.Add("Level 3: Heals 250 / tick");
				list.Add("Level 4: Heals 325 / tick");
				list.Add("Level 5: Heals 400 / tick");
				list.Add("");
				list.Add("Target: Group, except the user");
				list.Add("Duration: 30 sec");
				list.Add("Casting time: instant");				
			}
			else
			{
				list.Add("Level 1: Heals 100 / tick");
				list.Add("Level 2: Heals 250 / tick");
				list.Add("Level 3: Heals 400 / tick");
				list.Add("");
				list.Add("Target: Group, except the user");
				list.Add("Duration: 30 sec");
				list.Add("Casting time: instant");				
			}
		}

		protected virtual int GetHealAmountPerTick()
        {
            if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
                switch (Level)
                {
                    case 1:
                        return 100;
                    case 2:
						return 175;
                    case 3:
						return 250;
                    case 4:
						return 325;
                    case 5:
						return 400;
                }
            }
            else
            {
                switch (Level)
                {
                    case 1:
						return 100;
                    case 2:
						return 250;
                    case 3:
						return 400;
                }
            }

			return 0;
        }
	}
}