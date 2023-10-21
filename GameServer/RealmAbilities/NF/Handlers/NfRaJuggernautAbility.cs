using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities
{
	public class NfRaJuggernautAbility : TimedRealmAbility
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public NfRaJuggernautAbility(DbAbility dba, int level) : base(dba, level) { }

		int m_range = 1500;
		int m_duration = 60;
		byte m_value = 0;

		public override void Execute(GameLiving living)
		{
			GamePlayer player = living as GamePlayer;
			#region preCheck
			if (living == null)
			{
				log.Warn("Could not retrieve player in JuggernautAbilityHandler.");
				return;
			}

			if (!(living.IsAlive))
			{
				if(player != null)
					player.Out.SendMessage("You cannot use this ability while dead!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (living.IsMezzed)
			{
				if(player != null)
					player.Out.SendMessage("You cannot use this ability while mesmerized!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (living.IsStunned)
			{
				if(player != null)
					player.Out.SendMessage("You cannot use this ability while stunned!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (living.IsSitting)
			{
				if(player != null)
					player.Out.SendMessage("You cannot use this ability while sitting!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (living.ControlledBrain == null)
			{
				if(player != null)
					player.Out.SendMessage("You must have a pet controlled to use this ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (!living.IsWithinRadius( player.ControlledBrain.Body, m_range ))
			{
				if(player != null)
					player.Out.SendMessage("Your pet is too far away!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
            GameSpellEffect ml9=SpellHandler.FindEffectOnTarget(living.ControlledBrain.Body,"SummonMastery");
            if (ml9 != null)
            {
				if(player != null)
	                player.Out.SendMessage("Your Pet already has an ability of this type active", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                return;
            }

			#endregion

			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch (this.Level)
				{
					case 1:
						m_value = 10;
						break;
					case 2:
						m_value = 15;
						break;
					case 3:
						m_value = 20;
						break;
					case 4:
						m_value = 25;
						break;
					case 5:
						m_value = 30;
						break;
					default:
						return;
				}
			}
			else
			{
				switch (this.Level)
				{
					case 1:
						m_value = 10;
						break;
					case 2:
						m_value = 20;
						break;
					case 3:
						m_value = 30;
						break;
					default:
						return;
				}
			}

			new NfRaJuggernautEffect().Start(living, m_duration, m_value);

			DisableSkill(living);
		}

		public override int GetReUseDelay(int level)
		{
			return 900;
		}
	}
}
