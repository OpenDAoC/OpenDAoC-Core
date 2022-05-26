using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class ChargeAbility : TimedRealmAbility
	{
		public const int DURATION = 15000;

		public ChargeAbility(DBAbility dba, int level) : base(dba, level) { }

		// no charge when snared
		public override bool CheckPreconditions(GameLiving living, long bitmask)
		{
			lock (living.EffectList)
			{
				//foreach (IGameEffect effect in living.EffectList)
				//{
				//	if (effect is GameSpellEffect)
				//	{
				//		GameSpellEffect oEffect = (GameSpellEffect)effect;
				//		if (oEffect.Spell.SpellType.ToString().ToLower().IndexOf("speeddecrease") != -1 && oEffect.Spell.Value != 99)
				//		{
				//			GamePlayer player = living as GamePlayer;
				//			if (player != null) player.Out.SendMessage("You may not use this ability while snared!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				//			return true;
				//		}
				//	}
				//}
				var effect = EffectListService.GetSpellEffectOnTarget(living, eEffect.MovementSpeedDebuff);
				if (effect != null && effect.SpellHandler.Spell.Value != 99)
                {
					GamePlayer player = living as GamePlayer;
                    if (player != null) player.Out.SendMessage("You may not use this ability while snared!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return true;
                }
			}
			return base.CheckPreconditions(living, bitmask);
		}
		
		public override void Execute(GameLiving living)
		{
			if (living == null) return;
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			if (living.TargetObject == null || living.TargetObject is not GamePlayer ||
			    (living.TargetObject is GamePlayer enemy && enemy.Realm == living.Realm))
			{
				if(living is GamePlayer p)p.Out.SendMessage($"You can only charge enemy players.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			//if (living.TempProperties.getProperty("Charging", false)
			//	|| living.EffectList.CountOfType(typeof(SpeedOfSoundEffect), typeof(ArmsLengthEffect), typeof(ChargeEffect)) > 0)
			//{
			//	if (living is GamePlayer)
			//		((GamePlayer)living).Out.SendMessage("You already an effect of that type!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
			//	return;
			//}
			ChargeECSGameEffect charge = (ChargeECSGameEffect)EffectListService.GetEffectOnTarget(living, eEffect.Charge);
			//ChargeEffect charge = living.EffectList.GetOfType<ChargeEffect>();
			if (charge != null)
				charge.Cancel(false);
			//if (living is GamePlayer)
			//	((GamePlayer)living).Out.SendUpdateMaxSpeed();

			//new ChargeEffect().Start(living);
			new ChargeECSGameEffect(new ECSGameEffectInitParams(living, DURATION, 1, null));
			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch (level)
				{
					case 1: return 900;
					case 2: return 600;
					case 3: return 300;
					case 4: return 180;
					case 5: return 90;
					default: return 600;
				}				
			}
			else
			{
				switch (level)
				{
						case 1: return 900;
						case 2: return 300;
						case 3: return 90;
				}
				return 600;
			}
		}

		public override bool CheckRequirement(GamePlayer player)
		{
			return player.Level >= 45;
		}
	}
}
