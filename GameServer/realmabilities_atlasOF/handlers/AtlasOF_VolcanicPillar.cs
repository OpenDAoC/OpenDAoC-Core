using System;
using System.Collections;
using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Events;
using DOL.Database;
namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_VolcanicPillar : TimedRealmAbility, ISpellCastingAbilityHandler
    {
		public AtlasOF_VolcanicPillar(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private const int m_dmgValue = 700; // Takii - Temp value. Unclear what the real OF value was.
		private const int m_range = 1875; // bolt range
        private const int m_radius = 500; // post-1.62 nerf value (was 700)

		private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 14; }
		public override int GetReUseDelay(int level) { return 900; } // 15 mins

        private void CreateSpell(GamePlayer caster)
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Volcanic Pillar";
            m_dbspell.Icon = 0;
            m_dbspell.ClientEffect = 7025;
            m_dbspell.Damage = m_dmgValue;
			m_dbspell.DamageType = 13; // heat
            m_dbspell.Target = "Enemy";
            m_dbspell.Radius = m_radius;
			m_dbspell.Type = eSpellType.DirectDamageNoVariance.ToString();
            m_dbspell.Value = 0;
            m_dbspell.Duration = 0;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.RecastDelay = GetReUseDelay(0); // Spell code is responsible for disabling this ability and will use this value.
            m_dbspell.Range = m_range;
			m_spell = new Spell(m_dbspell, caster.Level);
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer m_caster = living as GamePlayer;
			if (m_caster == null || m_caster.castingComponent == null)
				return;

            GameLiving m_target = m_caster.TargetObject as GameLiving;
            if (m_target == null)
                return;

            CreateSpell(m_caster);

            if (m_target.IsAlive && m_spell != null)
            {
                m_caster.castingComponent.StartCastSpell(m_spell, m_spellline, this);
            }

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().

            // 			if (m_caster.TargetObject == null)
            // 			{
            // 				m_caster.Out.SendMessage("You need a target for this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            // 				m_caster.DisableSkill(this, 3 * 1000);
            // 				return;
            // 			}
            // 
            // 			if ( !m_caster.IsWithinRadius( m_caster.TargetObject, (int)(m_range * m_caster.GetModified(eProperty.SpellRange) * 0.01 ) ) )
            // 			{
            // 				m_caster.Out.SendMessage(m_caster.TargetObject + " is too far away.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            // 				return;
            // 			}
            // 
            // 			foreach (GamePlayer i_player in m_caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            // 			{
            // 				if (i_player == m_caster)
            // 				{
            // 					i_player.MessageToSelf("You cast " + this.Name + "!", eChatType.CT_Spell);
            // 				}
            // 				else
            // 				{
            // 					i_player.MessageFromArea(m_caster, m_caster.Name + " casts a spell!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
            // 				}
            // 			}
            // 
            // 			foreach (GamePlayer player in m_caster.TargetObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            // 			{
            // 				player.Out.SendSpellEffectAnimation(m_caster, (m_caster.TargetObject as GameLiving), 7025, 0, false, 1);
            // 			}
            // 
            // 			foreach (GameNPC mob in m_caster.TargetObject.GetNPCsInRadius(m_radius))
            // 			{
            // 				if (!GameServer.ServerRules.IsAllowedToAttack(m_caster, mob, true))
            // 					continue;
            // 
            // 				mob.TakeDamage(m_caster, eDamageType.Heat, m_dmgValue, 0);
            // 				m_caster.Out.SendMessage("You hit the " + mob.Name + " for " + m_dmgValue + " damage.", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            // 				foreach (GamePlayer player2 in m_caster.TargetObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            // 				{
            // 					player2.Out.SendSpellEffectAnimation(m_caster, mob, 7025, 0, false, 1);
            // 				}
            // 			}
            // 
            // 			foreach (GamePlayer aeplayer in m_caster.TargetObject.GetPlayersInRadius(500))
            // 			{
            // 				if (!GameServer.ServerRules.IsAllowedToAttack(m_caster, aeplayer, true))
            // 					continue;
            // 
            // 				aeplayer.TakeDamage(m_caster, eDamageType.Heat, m_dmgValue, 0);
            // 				m_caster.Out.SendMessage("You hit " + aeplayer.Name + " for " + m_dmgValue + " damage.", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            // 				aeplayer.Out.SendMessage(m_caster.Name + " hits you for " + m_dmgValue + " damage.", eChatType.CT_YouWereHit, eChatLoc.CL_SystemWindow); 
            // 				foreach (GamePlayer player3 in m_caster.TargetObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            // 				{
            // 					player3.Out.SendSpellEffectAnimation(m_caster, aeplayer, 7025, 0, false, 1);
            // 				}
            // 			}
		}
	}
}
