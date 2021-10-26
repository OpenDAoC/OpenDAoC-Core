using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Events;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_PerfectRecovery : TimedRealmAbility, ISpellCastingAbilityHandler
    {
		public AtlasOF_PerfectRecovery(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        private bool LastTargetLetRequestExpire = false;
        private const string RESURRECT_CASTER_PROPERTY = "RESURRECT_CASTER";

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        private void CreateRezSpell(GamePlayer caster)
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Perfect Recovery Rez";
            m_dbspell.Icon = 0;
            m_dbspell.ClientEffect = 7019;
            m_dbspell.Damage = 0;
            m_dbspell.Target = "corpse"; // Rez spells are of that type so that they only work on dead realm members.
            m_dbspell.Radius = 0;
            m_dbspell.Type = eSpellType.Resurrect.ToString();
            m_dbspell.Value = 0;
            m_dbspell.Duration = 0;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.Range = 1500;
            m_dbspell.RecastDelay = GetReUseDelay(0); // Spell code is responsible for disabling this ability and will use this value.
            m_dbspell.ResurrectHealth = 100;
            m_dbspell.ResurrectMana = 100;
            m_spell = new Spell(m_dbspell, caster.Level);
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", false);
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

            CreateRezSpell(m_caster);

            if (m_spell != null)
            {
                m_caster.castingComponent.StartCastSpell(m_spell, m_spellline, this);
            }

            // Cleanup
            m_spell = null;
            m_dbspell = null;
            m_spellline = null;

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
        }

        public void OnRezDeclined(GamePlayer rezzer)
        {
            rezzer.DisableSkill(this, 0); // Re-enable PR;
        }

        // Override base DOL behavior to re-enable PR if rez target does not accept it in time.
//         protected override int ResurrectExpiredCallback(RegionTimer callingTimer)
//         {
//             GamePlayer target = (GamePlayer)callingTimer.Properties.getProperty<object>("targetPlayer", null);
//             GameLiving rezzer = (GameLiving)target.TempProperties.getProperty<object>(RESURRECT_CASTER_PROPERTY, null);
//             
//             // Remove the rez request
//             GameTimer resurrectExpiredTimer = null;
//             lock (m_resTimersByLiving.SyncRoot)
//             {
//                 resurrectExpiredTimer = (GameTimer)m_resTimersByLiving[target];
//                 m_resTimersByLiving.Remove(target);
//             }
// 
//             resurrectExpiredTimer?.Stop();
// 
//             target?.TempProperties.removeProperty(RESURRECT_CASTER_PROPERTY);
//             target?.Out.SendMessage("Your resurrection spell has expired.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
//             rezzer.DisableSkill(this, 0); // Re-enable PR;
//             LastTargetLetRequestExpire = true;
//             return 0;
//         }

        // Override base DOL behavior in order to allow resetting the cooldown if the target lets the rez request expire (15 seconds).
        // Without the LastTargetLetRequestExpire early-out below, subsequent requests on the same target that let the rez expire result
        // in ResurrectResponceHandler being called immediately when the rezzer casts PR on that target again, and it gets immediately declined
        // and we enter this loop where the rezzer cannot ever PR this person because we process this auto-decline response.
//         protected override void ResurrectResponceHandler(GamePlayer player, byte response)
//         {
//             if (LastTargetLetRequestExpire)
//             {
//                 LastTargetLetRequestExpire = false;
//                 return;
//             }
//             
//             GameTimer resurrectExpiredTimer = null;
//             lock (m_resTimersByLiving.SyncRoot)
//             {
//                 resurrectExpiredTimer = (GameTimer)m_resTimersByLiving[player];
//                 m_resTimersByLiving.Remove(player);
//             }
//             if (resurrectExpiredTimer != null)
//             {
//                 resurrectExpiredTimer.Stop();
//             }
// 
//             GameLiving rezzer = (GameLiving)player.TempProperties.getProperty<object>(RESURRECT_CASTER_PROPERTY, null);
//             if (!player.IsAlive)
//             {
//                 if (rezzer == null)
//                 {
//                     player.Out.SendMessage("No one is currently trying to resurrect you.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
//                 }
//                 else
//                 {
//                     if (response == 1)
//                     {
//                         ResurrectLiving(player, rezzer); //accepted
//                     }
//                     else
//                     {
//                         player.Out.SendMessage("You decline to be resurrected.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
//                         //Dont need to refund anything with PR
//                         //m_caster.Mana += CalculateNeededPower(player);
//                         //but we do need to give them PR back
//                         //Seems like the best way to do this is to send a 0 duration to DisableSkill, which will enable to ability
//                         (rezzer as GameLiving).DisableSkill(this, 0);
// 
//                     }
//                 }
//             }
//             player.TempProperties.removeProperty(RESURRECT_CASTER_PROPERTY);
//         }
    }
}