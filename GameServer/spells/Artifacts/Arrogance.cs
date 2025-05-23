/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;

namespace DOL.GS.Spells.Atlantis
{
    /// <summary>
    /// Arrogance spell handler
    /// </summary>
    [SpellHandler(eSpellType.Arrogance)]
    public class Arrogance : SpellHandler
    {
    	GamePlayer playertarget = null;
    	
        /// <summary>
        /// The timer that will cancel the effect
        /// </summary>
        protected ECSGameTimer m_expireTimer;
        public override void OnEffectStart(GameSpellEffect effect)
        {
        	base.OnEffectStart(effect);
            effect.Owner.BaseBuffBonusCategory[eProperty.Dexterity] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Strength] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Constitution] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Acuity] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Piety] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Empathy] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Quickness] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Intelligence] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Charisma] += (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.ArmorAbsorption] += (int)m_spell.Value;                       
            
            if (effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumbrance();
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();       
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            effect.Owner.BaseBuffBonusCategory[eProperty.Dexterity] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Strength] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Constitution] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Acuity] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Piety] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Empathy] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Quickness] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Intelligence] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.Charisma] -= (int)m_spell.Value;
            effect.Owner.BaseBuffBonusCategory[eProperty.ArmorAbsorption] -= (int)m_spell.Value;
             
            if (effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumbrance();
                player.UpdatePlayerStatus();
            	player.Out.SendUpdatePlayer();  
                Start(player);
            }
            return base.OnEffectExpires(effect,noMessages);
        }

        protected virtual void Start(GamePlayer player)
        {
        	playertarget = player;
            StartTimers();
            player.DebuffCategory[eProperty.Dexterity] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Strength] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Constitution] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Acuity] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Piety] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Empathy] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Quickness] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Intelligence] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.Charisma] += (int)m_spell.Value;
            player.DebuffCategory[eProperty.ArmorAbsorption] += (int)m_spell.Value;
            
            player.Out.SendCharStatsUpdate();
            player.UpdateEncumbrance();
            player.UpdatePlayerStatus();
          	player.Out.SendUpdatePlayer(); 
        }

        protected virtual void Stop()
        {
            if (playertarget != null)
            {     
	            playertarget.DebuffCategory[eProperty.Dexterity] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Strength] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Constitution] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Acuity] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Piety] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Empathy] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Quickness] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Intelligence] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.Charisma] -= (int)m_spell.Value;;
	            playertarget.DebuffCategory[eProperty.ArmorAbsorption] -= (int)m_spell.Value;;
	            
            	playertarget.Out.SendCharStatsUpdate();
            	playertarget.UpdateEncumbrance();
            	playertarget.UpdatePlayerStatus();
          		playertarget.Out.SendUpdatePlayer(); 
            }
            StopTimers();
        }
        protected virtual void StartTimers()
        {
            StopTimers();
            m_expireTimer = new ECSGameTimer(playertarget, new ECSGameTimer.ECSTimerCallback(ExpiredCallback), 10000);
        }
        protected virtual void StopTimers()
        {
            if (m_expireTimer != null)
            {
                m_expireTimer.Stop();
                m_expireTimer = null;
            }
        }
        protected virtual int ExpiredCallback(ECSGameTimer callingTimer)
        {
            Stop();
            return 0;
        }

        public Arrogance(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
