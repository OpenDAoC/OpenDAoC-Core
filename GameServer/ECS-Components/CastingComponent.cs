using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;
using System;
using System.Linq;

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
        
        
        public bool StartCastSpell(Spell spell, SpellLine line, ISpellCastingAbilityHandler spellCastingAbilityHandler = null)
        {
            if (!EntityManager.GetLivingByComponent(typeof(CastingComponent)).Contains(owner))
                EntityManager.AddComponent(typeof(CastingComponent), owner);
            //Check for Conditions to Cast
            if (owner is GamePlayer p)
            {
                if (!CanCastSpell(p))
                {
                    return false; 
                }
            }

            ISpellHandler m_newSpellHandler = ScriptMgr.CreateSpellHandler(owner, spell, line);

            // Abilities that cast spells (i.e. Realm Abilities such as Volcanic Pillar) need to set this so the associated ability gets disabled if the cast is successful.
            m_newSpellHandler.Ability = spellCastingAbilityHandler;

            if (spellHandler != null)
            {
                if (m_newSpellHandler.Spell.IsInstantCast)
                {
                    queuedSpellHandler = spellHandler;
                    spellHandler = m_newSpellHandler;
                }
                else 
                {
                    if (owner is GamePlayer pl)
                    {
                        if (pl.SpellQueue)
                        {
                            pl.Out.SendMessage("You are already casting a spell! You prepare this spell as a follow up!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                            queuedSpellHandler = m_newSpellHandler;
                        } 
                        else
                        {
                            pl.Out.SendMessage("You are already casting a spell!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                        }
                    } 
                }
            }
            else
            {
                spellHandler = m_newSpellHandler;

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

            return true;
        }

        private bool CanCastSpell(GameLiving living)
        {
            var p = living as GamePlayer;
            /*
            if (spellHandler != null)
            {
                p.Out.SendMessage("You are already casting a spell.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }*/

            if (p != null && p.IsCrafting)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                p.CraftTimer = null;
                p.Out.SendCloseTimerWindow();
            }

            if (living != null)
            {
                if (living.IsStunned)
                {
                    p?.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.CastSpell.CantCastStunned"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    return false;
                }
                if (living.IsMezzed)
                {
                    p?.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.CastSpell.CantCastMezzed"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (living.IsSilenced)
                {
                    p?.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.CastSpell.CantCastFumblingWords"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    return false;
                }
            }
            return true;
        }

    }
}