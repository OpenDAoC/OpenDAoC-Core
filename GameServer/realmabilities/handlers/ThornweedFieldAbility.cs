using System;
using System.Collections;
using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Events;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class ThornweedFieldAbility : TimedRealmAbility
	{
		public ThornweedFieldAbility(DbAbilities dba, int level) : base(dba, level) { }
		private int m_dmgValue;
		private uint m_duration;
		private GamePlayer m_player;

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			GamePlayer caster = living as GamePlayer;
			if (caster == null)
				return;

			if (caster.IsMoving)
			{
				caster.Out.SendMessage("You must be standing still to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (caster.GroundTarget == null )
            {
                caster.Out.SendMessage( "You must set a ground target to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow );
                return;
            }
            else if(!caster.IsWithinRadius( caster.GroundTarget, 1500 ))
			{
				caster.Out.SendMessage("Your ground target is too far away to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			this.m_player = caster;
			if (caster.attackComponent.AttackState)
			{
				caster.attackComponent.StopAttack();
			}
			caster.StopCurrentSpellcast();

			/*
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch (Level)
				{
					case 1: m_dmgValue = 25; m_duration = 10; break;
					case 2: m_dmgValue = 50; m_duration = 15; break;
					case 3: m_dmgValue = 100; m_duration = 20; break;
					case 4: m_dmgValue = 175; m_duration = 25; break;
					case 5: m_dmgValue = 250; m_duration = 30; break;
					default: return;
				}
			}
			else
			{
				switch (Level)
				{
					case 1: m_dmgValue = 25; m_duration = 10; break;
					case 2: m_dmgValue = 100; m_duration = 20; break;
					case 3: m_dmgValue = 250; m_duration = 30; break;
					default: return;
				}
			}*/

			m_dmgValue = caster.Level * 2;
			m_duration = 30;

			foreach (GamePlayer i_player in caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
			{
				if (i_player == caster)
				{
					i_player.MessageToSelf("You cast " + this.Name + "!", eChatType.CT_Spell);
				}
				else
				{
					i_player.MessageFromArea(caster, caster.Name + " casts a spell!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
				}

				i_player.Out.SendSpellCastAnimation(caster, 7028, 0);
			}

			if (caster.RealmAbilityCastTimer != null)
			{
				caster.RealmAbilityCastTimer.Stop();
				caster.RealmAbilityCastTimer = null;
				caster.Out.SendMessage("You cancel your Spell!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
			}

			caster.RealmAbilityCastTimer = new ECSGameTimer(caster);
			caster.RealmAbilityCastTimer.Callback = new ECSGameTimer.ECSTimerCallback(EndCast);
			caster.RealmAbilityCastTimer.Start(0);
		}

		protected virtual int EndCast(ECSGameTimer timer)
		{
			if (m_player.IsMezzed || m_player.IsStunned || m_player.IsSitting)
				return 0;
			Statics.ThornweedFieldBase twf = new Statics.ThornweedFieldBase(m_dmgValue);
			twf.CreateStatic(m_player, m_player.GroundTarget, m_duration, 5, 500);
			DisableSkill(m_player);
			timer.Stop();
			timer = null;
			return 0;
		}

		public override int GetReUseDelay(int level)
		{
			return 600;
		}
	}
}
