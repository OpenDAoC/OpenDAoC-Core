using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;
using System;

namespace DOL.GS
{
    //this component will hold all data related to casting spells
    public class CastingComponent
    {
	    //entity casting the spell
        public GameLiving owner;
        
		/// Multiplier for melee and magic.
		public double Effectiveness
        {
            get { return 1.0; }
            set { }
        }
    
        public  bool IsCasting
        {
            get { return spellHandler != null && spellHandler.IsCasting; }
        }


        //data for the spell that they are casting
        public ISpellHandler spellHandler;

        //data for the spell they want to queue
        public ISpellHandler queuedSpellHandler;


        public CastingComponent(GameLiving owner)
        {
            this.owner = owner;
        }
        
        
        public void StartCastSpell(Spell spell, SpellLine line)
        {
            //Check for Conditions to Cast
            if (owner is GamePlayer p)
            {
                if (!CanCastSpell(p))
                {
                    return; 
                }
            }

            if(spellHandler != null)
            {
                if(owner is GamePlayer pl)
                {
                    pl.Out.SendMessage("You begin casting this spell as a follow-up!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                
                queuedSpellHandler = ScriptMgr.CreateSpellHandler(owner, spell, line);
            }
            else
            {
                spellHandler = ScriptMgr.CreateSpellHandler(owner, spell, line);

                //Special CastSpell rules
                if (spellHandler is SummonNecromancerPet necroPetHandler)
                {
                    int hitsCap = MaxHealthCalculator.GetItemBonusCap(necroPetHandler.Caster)
                        + MaxHealthCalculator.GetItemBonusCapIncrease(necroPetHandler.Caster);

                    necroPetHandler.m_summonConBonus = necroPetHandler.Caster.GetModifiedFromItems(eProperty.Constitution);
                    necroPetHandler.m_summonHitsBonus = Math.Min(necroPetHandler.Caster.ItemBonus[(int)(eProperty.MaxHealth)], hitsCap)
                        + necroPetHandler.Caster.AbilityBonus[(int)(eProperty.MaxHealth)];
                }
            }

            if (!spellHandler.SpellLine.IsBaseLine)
            {
                spellHandler.Spell.IsSpec = true;
            }

            // Cancel MoveSpeedBuff=========================May not be the best place for this========================================
            owner.OnAttack();
        }

        private bool CanCastSpell(GamePlayer p)
        {
            /*
            if (spellHandler != null)
            {
                p.Out.SendMessage("You are already casting a spell.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }*/

            if (p.IsCrafting)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                p.CraftTimer = null;
                p.Out.SendCloseTimerWindow();
            }
            
            if (p.IsStunned)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.CastSpell.CantCastStunned"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }
            if (p.IsMezzed)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.CastSpell.CantCastMezzed"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (p.IsSilenced)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.CastSpell.CantCastFumblingWords"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                return false;
            }
            
            return true;
        }

    }
}