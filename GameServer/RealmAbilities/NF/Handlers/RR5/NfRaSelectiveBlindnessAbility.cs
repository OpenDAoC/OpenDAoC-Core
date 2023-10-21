using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.RealmAbilities
{
    public class NfRaSelectiveBlindnessAbility : Rr5RealmAbility
    {
        public const int DURATION = 20 * 1000;
        private const int SpellRange = 1500;
        private const ushort SpellRadius = 150;
        private GamePlayer m_player = null;
        private GamePlayer m_targetPlayer = null;

        public NfRaSelectiveBlindnessAbility(DbAbility dba, int level) : base(dba, level) { }

        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            if (living is GamePlayer)
            {
                m_player = living as GamePlayer;
                if (m_player.TargetObject == null)
                {
                    m_player.Out.SendMessage("You need a target for this ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    m_player.DisableSkill(this, 3 * 1000);
                    return;
                }
                if (!(m_player.TargetObject is GamePlayer))
                {
                    m_player.Out.SendMessage("This work only on players!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    m_player.DisableSkill(this, 3 * 1000);
                    return;
                }
                if (!GameServer.ServerRules.IsAllowedToAttack(m_player, (GamePlayer)m_player.TargetObject, true))
                {
                    m_player.Out.SendMessage("This work only on enemies!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                    m_player.DisableSkill(this, 3 * 1000);
                    return;
                }
                if ( !m_player.IsWithinRadius( m_player.TargetObject, SpellRange ) )
                {
                    m_player.Out.SendMessage(m_player.TargetObject + " is too far away!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                    m_player.DisableSkill(this, 3 * 1000);
                    return;
                }
                foreach (GamePlayer radiusPlayer in m_player.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
                {
					if (radiusPlayer == m_player)
					{
						radiusPlayer.MessageToSelf("You cast " + this.Name + "!", EChatType.CT_Spell);
					}
					else
					{
						radiusPlayer.MessageFromArea(m_player, m_player.Name + " casts a spell!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}

                    radiusPlayer.Out.SendSpellCastAnimation(m_player, 7059, 0);
                }

                if (m_player.RealmAbilityCastTimer != null)
                {
                    m_player.RealmAbilityCastTimer.Stop();
                    m_player.RealmAbilityCastTimer = null;
                    m_player.Out.SendMessage("You cancel your Spell!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                }

                m_targetPlayer = m_player.TargetObject as GamePlayer;

                //[StephenxPimentel]
                //1.108 - this ability is now instant cast.
                EndCast();
            }
        }

        private void EndCast()
        {
            if (m_player == null || !m_player.IsAlive) return;
            if (m_targetPlayer == null || !m_targetPlayer.IsAlive) return;

            if (!GameServer.ServerRules.IsAllowedToAttack(m_player, m_targetPlayer, true))
            {
                m_player.Out.SendMessage("This work only on enemies.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                m_player.DisableSkill(this, 3 * 1000);
                return;
            }
            if ( !m_player.IsWithinRadius( m_targetPlayer, SpellRange ) )
            {
                m_player.Out.SendMessage(m_targetPlayer + " is too far away.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
                m_player.DisableSkill(this, 3 * 1000);
                return;
            }
            foreach (GamePlayer radiusPlayer in m_targetPlayer.GetPlayersInRadius(SpellRadius))
            {
                if (!GameServer.ServerRules.IsAllowedToAttack(m_player, radiusPlayer, true))
                    continue;

				NfRaSelectiveBlindnessEffect SelectiveBlindness = radiusPlayer.EffectList.GetOfType<NfRaSelectiveBlindnessEffect>();
                if (SelectiveBlindness != null) SelectiveBlindness.Cancel(false);
                new NfRaSelectiveBlindnessEffect(m_player).Start(radiusPlayer);
            }
        }

        public override int GetReUseDelay(int level)
        {
            return 300;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("AE target 150 unit radius, 1500 unit range, blind enemies to user. 20s duration or attack by user, 5min RUT.");
            list.Add("");
            list.Add("Target: Enemy");
            list.Add("Duration: 20s");
            list.Add("Casting time: Instant");
        }

    }
}
