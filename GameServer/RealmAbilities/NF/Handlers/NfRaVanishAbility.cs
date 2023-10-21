using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.PropertyCalc;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities
{
	public class NfRaVanishAbility : TimedRealmAbility
	{		public NfRaVanishAbility(DbAbility dba, int level) : base(dba, level) { }

		/// <summary>
		/// Action
		/// </summary>
		/// <param name="living"></param>
		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | STEALTHED)) return;

			SendCasterSpellEffectAndCastMessage(living, 7020, true);

			int duration = 0;
			double speedBonus = 1;

			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch (Level)
				{
					case 1: duration = 3000; speedBonus = 1.0; break;
					case 2: duration = 3000; speedBonus = MaxMovementSpeedCalculator.SPEED1; break;
					case 3: duration = 4000; speedBonus = MaxMovementSpeedCalculator.SPEED3; break;
					case 4: duration = 5000; speedBonus = MaxMovementSpeedCalculator.SPEED4; break;
					case 5: duration = 6000; speedBonus = MaxMovementSpeedCalculator.SPEED5; break;
				}
			}
			else
			{
				switch (Level)
				{
					case 1: duration = 1000; speedBonus = 1.0; break;
					case 2: duration = 2000; speedBonus = MaxMovementSpeedCalculator.SPEED1; break;
					case 3: duration = 5000; speedBonus = MaxMovementSpeedCalculator.SPEED5; break;
				}
			}

			GamePlayer player = living as GamePlayer;
			if (player != null)
			{
				NfRaVanishEffect vanish = new NfRaVanishEffect(duration, speedBonus);
				vanish.Start(player);

				foreach (GameSpellEffect effect in living.EffectList.GetAllOfType<GameSpellEffect>())
				{
					if (effect.SpellHandler is DamageOverTimeSpell ||
						effect.SpellHandler is StyleBleedingEffect ||
							effect.SpellHandler is ACrowdControlSpell ||
							effect.SpellHandler is SpeedDecreaseSpell)
					{
						effect.Cancel(false);
					}
				}
			}

			foreach (GameObject attacker in living.attackComponent.Attackers.Keys)
			{
				if (attacker is not GameLiving livingAttacker)
					continue;

				if (livingAttacker.TargetObject == living)
				{
					livingAttacker.TargetObject = null;

					if (attacker is GamePlayer attackerPlayer)
						attackerPlayer.Out.SendChangeTarget(null);
					else if (attacker is GameNpc npcAttacker)
					{
						if (npcAttacker.Brain is IOldAggressiveBrain npcAttackerBrain)
							npcAttackerBrain.RemoveFromAggroList(living);

						npcAttacker.attackComponent.StopAttack();
					}
				}
			}

			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 900;
		}

		public override void AddEffectsInfo(IList<string> list)
		{
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				list.Add("Level 1: Normal Speed");
				list.Add("Level 2: Speed 1");
				list.Add("Level 3: Speed 3");
				list.Add("Level 3: Speed 4");
				list.Add("Level 3: Speed 5");
				list.Add("");
				list.Add("Target: Self");
				list.Add("Level 1: Duration: 3 sec");
				list.Add("Level 2: Duration: 3 sec");
				list.Add("Level 3: Duration: 4 sec");
				list.Add("Level 3: Duration: 5 sec");
				list.Add("Level 3: Duration: 6 sec");
				list.Add("Casting time: instant");
			}
			else
			{
				list.Add("Level 1: Normal Speed");
				list.Add("Level 2: Speed 1");
				list.Add("Level 3: Speed 5");
				list.Add("");
				list.Add("Target: Self");
				list.Add("Level 1: Duration: 1 sec");
				list.Add("Level 2: Duration: 2 sec");
				list.Add("Level 3: Duration: 5 sec");
				list.Add("Casting time: instant");
			}
		}
	}
}