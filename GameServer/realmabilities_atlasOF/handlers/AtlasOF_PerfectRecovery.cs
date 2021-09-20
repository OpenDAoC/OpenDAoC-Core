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
	public class AtlasOF_PerfectRecovery : PerfectRecoveryAbility
	{
		public AtlasOF_PerfectRecovery(DBAbility dba, int level) : base(dba, level) { }

        private bool LastTargetLetRequestExpire = false;
        private const string RESURRECT_CASTER_PROPERTY = "RESURRECT_CASTER";

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        protected override int GetResurrectValue() { return 100; }

        // Override base DOL behavior to re-enable PR if rez target does not accept it in time.
        protected override int ResurrectExpiredCallback(RegionTimer callingTimer)
        {
            
            GamePlayer target = (GamePlayer)callingTimer.Properties.getProperty<object>("targetPlayer", null);
            GameLiving rezzer = (GameLiving)target.TempProperties.getProperty<object>(RESURRECT_CASTER_PROPERTY, null);
            
            // Remove the rez request
            GameTimer resurrectExpiredTimer = null;
            lock (m_resTimersByLiving.SyncRoot)
            {
                resurrectExpiredTimer = (GameTimer)m_resTimersByLiving[target];
                m_resTimersByLiving.Remove(target);
            }

            resurrectExpiredTimer?.Stop();

            target?.TempProperties.removeProperty(RESURRECT_CASTER_PROPERTY);
            target?.Out.SendMessage("Your resurrection spell has expired.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            rezzer.DisableSkill(this, 0); // Re-enable PR;
            LastTargetLetRequestExpire = true;
            return 0;
        }

        // Override base DOL behavior in order to allow resetting the cooldown if the target lets the rez request expire (15 seconds).
        // Without the LastTargetLetRequestExpire early-out below, subsequent requests on the same target that let the rez expire result
        // in ResurrectResponceHandler being called immediately when the rezzer casts PR on that target again, and it gets immediately declined
        // and we enter this loop where the rezzer cannot ever PR this person because we process this auto-decline response.
        protected override void ResurrectResponceHandler(GamePlayer player, byte response)
        {
            if (LastTargetLetRequestExpire)
            {
                LastTargetLetRequestExpire = false;
                return;
            }
            
            GameTimer resurrectExpiredTimer = null;
            lock (m_resTimersByLiving.SyncRoot)
            {
                resurrectExpiredTimer = (GameTimer)m_resTimersByLiving[player];
                m_resTimersByLiving.Remove(player);
            }
            if (resurrectExpiredTimer != null)
            {
                resurrectExpiredTimer.Stop();
            }

            GameLiving rezzer = (GameLiving)player.TempProperties.getProperty<object>(RESURRECT_CASTER_PROPERTY, null);
            if (!player.IsAlive)
            {
                if (rezzer == null)
                {
                    player.Out.SendMessage("No one is currently trying to resurrect you.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    if (response == 1)
                    {
                        ResurrectLiving(player, rezzer); //accepted

                    }
                    else
                    {
                        player.Out.SendMessage("You decline to be resurrected.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        //Dont need to refund anything with PR
                        //m_caster.Mana += CalculateNeededPower(player);
                        //but we do need to give them PR back
                        //Lifeflight: Seems like the best way to do this is to send a 0 duration to DisableSkill, which will enable to ability
                        (rezzer as GameLiving).DisableSkill(this, 0);

                    }
                }
            }
            player.TempProperties.removeProperty(RESURRECT_CASTER_PROPERTY);
        }
    }
}