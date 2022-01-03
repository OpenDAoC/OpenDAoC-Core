using System;
using System.Collections;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System.Collections.Generic;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Purge Ability, removes negative effects
    /// </summary>
    public class PurgeAbility : TimedRealmAbility
    {
        public PurgeAbility(DBAbility dba, int level) : base(dba, level) { }

        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING)) return;
            
            if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
            	int seconds = 0;
            	switch(Level)
            	{
            		case 1:
            			seconds = 5;
            		break;
            	}
            	
            	if(seconds > 0)
            	{
	                PurgeTimer timer = new PurgeTimer(living, this, seconds);
	                timer.Interval = 1000;
	                timer.Start(1);
	                DisableSkill(living);            		
            	}
            	else
            	{
	                SendCastMessage(living);
	                if (RemoveNegativeEffects(living, this))
	                {
	                    DisableSkill(living);
	                }            		
            	}
            }
            else
            {
	            if (Level < 2)
	            {
	                PurgeTimer timer = new PurgeTimer(living, this, 5);
	                timer.Interval = 1000;
	                timer.Start(1);
	                DisableSkill(living);
	            }
	            else
	            {
	                SendCastMessage(living);
	                if (RemoveNegativeEffects(living, this))
	                {
	                    DisableSkill(living);
	                }
	            }
            }
        }

        protected static bool RemoveNegativeEffects(GameLiving living, PurgeAbility purge, bool isFromGroupPurge = false)
        {
            bool removed = false;
            ArrayList effectsToRemove = new ArrayList();

            GamePlayer player = (GamePlayer)living;

            if (player == null)
                return false;

            EffectListComponent effectListComponent = null;

            if (player.CharacterClass.ID == (int)eCharacterClass.Necromancer)
            {
                NecromancerPet necroPet = (NecromancerPet)player.ControlledBrain.Body;

                if (necroPet != null)
                {
                    effectListComponent = necroPet.effectListComponent;
                }
                else
                {
                    effectListComponent = player.effectListComponent;
                }
            }
            else
            {
                effectListComponent = player.effectListComponent;
            }

            if (effectListComponent == null)
                return false;

            // Gather effects to cancel
            foreach (ECSGameEffect e in effectListComponent.GetAllEffects())
            {
                if (e.HasPositiveEffect)
                    continue;

                if (e is ECSImmunityEffect)
                    continue;

                effectsToRemove.Add(e);
            }

            // Cancel effects
            foreach (ECSGameEffect e in effectsToRemove)
            {
                EffectService.RequestCancelEffect(e);
                removed = true;
            }

            // Show spell effect
            foreach (GamePlayer rangePlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                rangePlayer.Out.SendSpellEffectAnimation(effectListComponent.Owner, effectListComponent.Owner, 7011, 0, false, (byte)(removed ? 1 : 0));
            }

            // Disable purge if an effect was purged
            if (removed)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PurgeAbility.RemoveNegativeEffects.FallFromYou"), eChatType.CT_Advise, eChatLoc.CL_SystemWindow);
                player.Stealth(false);
            }
            else if (!isFromGroupPurge)
            {
                player.DisableSkill(purge, 5);
            }

            return removed;
        }

        protected class PurgeTimer : GameTimer
        {
            GameLiving m_caster;
            PurgeAbility m_purge;
            int counter;

            public PurgeTimer(GameLiving caster, PurgeAbility purge, int count)
                : base(caster.CurrentRegion.TimeManager)
            {
                m_caster = caster;
                m_purge = purge;
                counter = count;
            }
            protected override void OnTick()
            {
                if (!m_caster.IsAlive)
                {
                    Stop();
                    if (m_caster is GamePlayer)
                    {
                        ((GamePlayer)m_caster).DisableSkill(m_purge, 0);
                    }
                    return;
                }
                if (counter > 0)
                {
                    GamePlayer player = m_caster as GamePlayer;
                    if (player != null)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "PurgeAbility.OnTick.PurgeActivate", counter), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    counter--;
                    return;
                }
                m_purge.SendCastMessage(m_caster);
                RemoveNegativeEffects(m_caster, m_purge);
                Stop();
            }
        }

        public override int GetReUseDelay(int level)
        {
        	if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
        	{
        		switch(level)
        		{
					case 3 :
						return 600;
        			case 4 :
        				return 450;
        			case 5 :
        				return 300;
        			default :
        				return 900;        				
        		}
        	}
        	else 
        	{
            	return (level < 3) ? 900 : 300;
        	}
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "PurgeAbility.AddEffectsInfo.Info1"));
            list.Add("");
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "PurgeAbility.AddEffectsInfo.Info2"));
            list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "PurgeAbility.AddEffectsInfo.Info3"));
        }
    }
}