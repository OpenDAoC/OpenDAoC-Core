using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	public class NfRaVolcanicPillarAbility : TimedRealmAbility
	{
		public NfRaVolcanicPillarAbility(DbAbility dba, int level) : base(dba, level) { }
		private int m_dmgValue = 0;
		private GamePlayer m_caster = null;

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			m_caster = living as GamePlayer;
			if (m_caster == null)
				return;

			if (m_caster.TargetObject == null)
			{
				m_caster.Out.SendMessage("You need a target for this ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				m_caster.DisableSkill(this, 3 * 1000);
				return;
			}

			if ( !m_caster.IsWithinRadius( m_caster.TargetObject, (int)( 1500 * m_caster.GetModified(EProperty.SpellRange) * 0.01 ) ) )
			{
				m_caster.Out.SendMessage(m_caster.TargetObject + " is too far away.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				return;
			}

			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch (Level)
				{
					case 1: m_dmgValue = 200; break;
					case 2: m_dmgValue = 350; break;
					case 3: m_dmgValue = 500; break;
					case 4: m_dmgValue = 625; break;
					case 5: m_dmgValue = 750; break;
					default: return;
				}
			}
			else
			{
				switch (Level)
				{
					case 1: m_dmgValue = 200; break;
					case 2: m_dmgValue = 500; break;
					case 3: m_dmgValue = 750; break;
					default: return;
				}
			}

			foreach (GamePlayer i_player in m_caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (i_player == m_caster)
				{
					i_player.MessageToSelf("You cast " + this.Name + "!", EChatType.CT_Spell);
				}
				else
				{
					i_player.MessageFromArea(m_caster, m_caster.Name + " casts a spell!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				}

				i_player.Out.SendSpellCastAnimation(m_caster, 7025, 20);
			}

			if (m_caster.RealmAbilityCastTimer != null)
			{
				m_caster.RealmAbilityCastTimer.Stop();
				m_caster.RealmAbilityCastTimer = null;
				m_caster.Out.SendMessage("You cancel your Spell!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
			}

			m_caster.RealmAbilityCastTimer = new EcsGameTimer(m_caster);
			m_caster.RealmAbilityCastTimer.Callback = new EcsGameTimer.EcsTimerCallback(EndCast);
			m_caster.RealmAbilityCastTimer.Start(2000);
		}

		protected virtual int EndCast(EcsGameTimer timer)
		{
			if (m_caster.TargetObject == null)
			{
				m_caster.Out.SendMessage("You need a target for this ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				m_caster.DisableSkill(this, 3 * 1000);
				return 0;
			}

            if ( !m_caster.IsWithinRadius( m_caster.TargetObject, (int)( 1500 * m_caster.GetModified( EProperty.SpellRange ) * 0.01 ) ) )
			{
				m_caster.Out.SendMessage(m_caster.TargetObject + " is too far away.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
				return 0;
			}

			foreach (GamePlayer player in m_caster.TargetObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendSpellEffectAnimation(m_caster, (m_caster.TargetObject as GameLiving), 7025, 0, false, 1);
			}

			foreach (GameNPC mob in m_caster.TargetObject.GetNPCsInRadius(500))
			{
				if (!GameServer.ServerRules.IsAllowedToAttack(m_caster, mob, true))
					continue;

				mob.TakeDamage(m_caster, EDamageType.Heat, m_dmgValue, 0);
				m_caster.Out.SendMessage("You hit the " + mob.Name + " for " + m_dmgValue + " damage.", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
				foreach (GamePlayer player2 in m_caster.TargetObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					player2.Out.SendSpellEffectAnimation(m_caster, mob, 7025, 0, false, 1);
				}
			}

			foreach (GamePlayer aeplayer in m_caster.TargetObject.GetPlayersInRadius(500))
			{
				if (!GameServer.ServerRules.IsAllowedToAttack(m_caster, aeplayer, true))
					continue;

				aeplayer.TakeDamage(m_caster, EDamageType.Heat, m_dmgValue, 0);
				m_caster.Out.SendMessage("You hit " + aeplayer.Name + " for " + m_dmgValue + " damage.", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
				aeplayer.Out.SendMessage(m_caster.Name + " hits you for " + m_dmgValue + " damage.", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow); 
				foreach (GamePlayer player3 in m_caster.TargetObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					player3.Out.SendSpellEffectAnimation(m_caster, aeplayer, 7025, 0, false, 1);
				}
			}

			DisableSkill(m_caster);
			timer.Stop();
			timer = null;
			return 0;
		}

		public override int GetReUseDelay(int level)
		{
			return 900;
		}
	}
}
