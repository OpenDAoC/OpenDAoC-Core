using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
    public class CastingComponent
    {
        
        public SpellHandler _spellHandler;

        public GamePlayer owner;

        public ISpellHandler spellHandler;

        public CastingComponent(GamePlayer owner)
        {
            this.owner = owner;
        }
        
        public void Tick()
        {
	        if (spellHandler == null)
	        {
		        return;
	        }
	        
	        spellHandler.Tick();
        }

        public bool CastSpell(Spell spell, SpellLine line)
        {

	        //Check if we are able to cast
	        if (!CheckSpellPrecast(spell, line))
	        {
		        return false;
	        }
	        
	        //Now Create the SpellHandler
	        spellHandler = ScriptMgr.CreateSpellHandler(owner, spell, line);
	        
	        spellHandler.Tick();
	        return true;

	        //
	        //       bool casted = false;
	        //
	        // if (IsCrafting)
	        // {
	        //              Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
	        // 	CraftTimer.Stop();
	        // 	CraftTimer = null;
	        // 	Out.SendCloseTimerWindow();
	        // }
	        //
	        // if (spell.SpellType == "StyleHandler" || spell.SpellType == "MLStyleHandler")
	        // {
	        // 	Style style = SkillBase.GetStyleByID((int)spell.Value, CharacterClass.ID);
	        // 	//Andraste - Vico : try to use classID=0 (easy way to implement CL Styles)
	        // 	if (style == null) style = SkillBase.GetStyleByID((int)spell.Value, 0);
	        // 	if (style != null)
	        // 	{
	        // 		StyleProcessor.TryToUseStyle(this, style);
	        // 	}
	        // 	else { Out.SendMessage("That style is not implemented!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow); }
	        // }
	        // else if (spell.SpellType == "BodyguardHandler")
	        // {
	        // 	Ability ab = SkillBase.GetAbility("Bodyguard");
	        // 	IAbilityActionHandler handler = SkillBase.GetAbilityActionHandler(ab.KeyName);
	        // 	if (handler != null)
	        // 	{
	        // 		handler.Execute(ab, this);
	        // 		return true;
	        // 	}
	        // }
	        // else
	        // {
	        // 	if (IsStunned)
	        // 	{
	        // 		Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.CantCastStunned"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 		return false;
	        // 	}
	        // 	if (IsMezzed)
	        // 	{
	        // 		Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.CantCastMezzed"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 		return false;
	        // 	}
	        //
	        // 	if (IsSilenced)
	        // 	{
	        //                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.CantCastFumblingWords"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
	        // 		return false;
	        // 	}
	        //
	        // 	double fumbleChance = GetModified(eProperty.SpellFumbleChance);
	        // 	fumbleChance *= 0.01;
	        // 	if (fumbleChance > 0)
	        // 	{
	        // 		if (Util.ChanceDouble(fumbleChance))
	        // 		{
	        //                      Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.CantCastFumblingWords"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
	        // 			return false;
	        // 		}
	        // 	}
	        //
	        // 	lock (m_spellQueueAccessMonitor)
	        // 	{
	        // 		if (m_runningSpellHandler != null)
	        // 		{
	        // 			if (m_runningSpellHandler.CanQueue == false)
	        // 			{
	        // 				m_runningSpellHandler.CasterMoves();
	        // 				return false;
	        // 			}
	        //
	        // 			if (spell.CastTime > 0 && !(m_runningSpellHandler is ChamberSpellHandler) && spell.SpellType != "Chamber")
	        // 			{
	        // 				if (m_runningSpellHandler.Spell.InstrumentRequirement != 0)
	        // 				{
	        // 					Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.AlreadyPlaySong"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 					return false;
	        // 				}
	        // 				if (SpellQueue)
	        // 				{
	        // 					if (spell.SpellType.ToLower() == "archery")
	        // 					{
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.FollowSpell", spell.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
	        // 					}
	        // 					else
	        // 					{
	        // 						Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.AlreadyCastFollow"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 					}
	        //
	        // 					m_nextSpell = spell;
	        // 					m_nextSpellLine = line;
	        // 					m_nextSpellTarget = TargetObject as GameLiving;
	        // 					return true;
	        // 				}
	        // 				else Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.AlreadyCastNoQueue"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 				
	        // 				return false;
	        // 			}
	        // 			else if (m_runningSpellHandler is PrimerSpellHandler)
	        // 			{
	        // 				if (!spell.IsSecondary)
	        // 				{
	        //                              Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.OnlyASecondarySpell"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 				}
	        // 				else
	        // 				{
	        // 					if (SpellQueue && !(m_runningSpellHandler is ChamberSpellHandler))
	        // 					{
	        // 						Spell cloneSpell = null;
	        // 						if (m_runningSpellHandler is PowerlessSpellHandler)
	        // 						{
	        // 							cloneSpell = spell.Copy();
	        // 							cloneSpell.CostPower = false;
	        // 							m_nextSpell = cloneSpell;
	        // 							m_nextSpellLine = line;
	        // 							casted = true;
	        // 						}
	        // 						else if (m_runningSpellHandler is RangeSpellHandler)
	        // 						{
	        // 							cloneSpell = spell.Copy();
	        // 							cloneSpell.CostPower = false;
	        // 							cloneSpell.OverrideRange = m_runningSpellHandler.Spell.Range;
	        // 							m_nextSpell = cloneSpell;
	        // 							m_nextSpellLine = line;
	        // 							casted = true;
	        // 						}
	        // 						else if (m_runningSpellHandler is UninterruptableSpellHandler)
	        // 						{
	        // 							cloneSpell = spell.Copy();
	        // 							cloneSpell.CostPower = false;
	        // 							m_nextSpell = cloneSpell;
	        // 							m_nextSpellLine = line;
	        // 							casted = true;
	        // 						}
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.PrepareSecondarySpell"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 					}
	        // 					return casted;
	        // 				}
	        // 			}
	        // 			else if (m_runningSpellHandler is ChamberSpellHandler)
	        // 			{
	        // 				ChamberSpellHandler chamber = (ChamberSpellHandler)m_runningSpellHandler;
	        // 				if (IsMoving || IsStrafing)
	        // 				{
	        // 					m_runningSpellHandler = null;
	        // 					return false;
	        // 				}
	        // 				if (spell.IsPrimary)
	        // 				{
	        // 					if (spell.SpellType == "Bolt" && !chamber.Spell.AllowBolt)
	        // 					{
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.SpellNotInChamber"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 						return false;
	        // 					}
	        // 					if (chamber.PrimarySpell == null)
	        // 					{
	        // 						Spell cloneSpell = spell.Copy();
	        // 						cloneSpell.InChamber = true;
	        // 						cloneSpell.CostPower = false;
	        // 						chamber.PrimarySpell = cloneSpell;
	        // 						chamber.PrimarySpellLine = line;
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.SpellInChamber", spell.Name, ((ChamberSpellHandler)m_runningSpellHandler).Spell.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.SelectSecondSpell", ((ChamberSpellHandler)m_runningSpellHandler).Spell.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
	        // 					}
	        // 					else
	        // 					{
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.SpellNotInChamber"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 					}
	        // 				}
	        // 				else if (spell.IsSecondary)
	        // 				{
	        // 					if (chamber.PrimarySpell != null)
	        // 					{
	        // 						if (chamber.SecondarySpell == null)
	        // 						{
	        // 							Spell cloneSpell = spell.Copy();
	        // 							cloneSpell.CostPower = false;
	        // 							cloneSpell.InChamber = true;
	        // 							cloneSpell.OverrideRange = chamber.PrimarySpell.Range;
	        // 							chamber.SecondarySpell = cloneSpell;
	        // 							chamber.SecondarySpellLine = line;
	        //
	        //                                      Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.SpellInChamber", spell.Name, ((ChamberSpellHandler)m_runningSpellHandler).Spell.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
	        // 						}
	        // 						else
	        // 						{
	        //                                      Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.AlreadyChosenSpells"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 						}
	        // 					}
	        // 					else
	        // 					{
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.PrimarySpellFirst"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 					}
	        // 				}
	        //
	        // 			}
	        // 			else if (!(m_runningSpellHandler is ChamberSpellHandler) && spell.SpellType == "Chamber")
	        // 			{
	        //                          Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.NotAFollowSpell"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 				return false;
	        // 			}
	        // 		}
	        // 	}
	        // 	ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(this, spell, line);
	        // 	if (spellhandler != null)
	        // 	{
	        // 		if (spell.CastTime > 0)
	        // 		{
	        // 			GameSpellEffect effect = SpellHandler.FindEffectOnTarget(this, "Chamber", spell.Name);
	        //
	        // 			if (effect != null && spell.Name == effect.Spell.Name)
	        // 			{
	        // 				casted = spellhandler.CastSpell();
	        // 			}
	        // 			else
	        // 			{
	        // 				if (spellhandler is ChamberSpellHandler && m_runningSpellHandler == null)
	        // 				{
	        // 					((ChamberSpellHandler)spellhandler).EffectSlot = ChamberSpellHandler.GetEffectSlot(spellhandler.Spell.Name);
	        // 					m_runningSpellHandler = spellhandler;
	        // 					m_runningSpellHandler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
	        // 					casted = spellhandler.CastSpell();
	        // 				}
	        // 				else if (m_runningSpellHandler == null)
	        // 				{
	        // 					m_runningSpellHandler = spellhandler;
	        // 					m_runningSpellHandler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
	        // 					casted = spellhandler.CastSpell();
	        // 				}
	        // 			}
	        // 		}
	        // 		else
	        // 		{
	        // 			if (spell.IsSecondary)
	        // 			{
	        // 				GameSpellEffect effect = SpellHandler.FindEffectOnTarget(this, "Powerless");
	        // 				if (effect == null)
	        // 					effect = SpellHandler.FindEffectOnTarget(this, "Range");
	        // 				if (effect == null)
	        // 					effect = SpellHandler.FindEffectOnTarget(this, "Uninterruptable");
	        //
	        // 				if (m_runningSpellHandler == null && effect == null)
	        //                              Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.CantSpellDirectly"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 				else if (m_runningSpellHandler != null)
	        // 				{
	        // 					if (m_runningSpellHandler.Spell.IsPrimary)
	        // 					{
	        // 						lock (m_spellQueueAccessMonitor)
	        // 						{
	        // 							if (SpellQueue && !(m_runningSpellHandler is ChamberSpellHandler))
	        // 							{
	        //                                          Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.PrepareSecondarySpell"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        // 								m_nextSpell = spell;
	        // 								spell.OverrideRange = m_runningSpellHandler.Spell.Range;
	        // 								m_nextSpellLine = line;
	        // 								casted = true;
	        // 							}
	        // 						}
	        // 					}
	        // 					else if (!(m_runningSpellHandler is ChamberSpellHandler))
	        //                                  Out.SendMessage(LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CastSpell.CantSpellDirectly"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
	        //
	        // 				}
	        // 				else if (effect != null)
	        // 				{
	        // 					Spell cloneSpell = null;
	        // 					if (effect.SpellHandler is PowerlessSpellHandler)
	        // 					{
	        // 						cloneSpell = spell.Copy();
	        // 						cloneSpell.CostPower = false;
	        // 						spellhandler = ScriptMgr.CreateSpellHandler(this, cloneSpell, line);
	        // 						casted = spellhandler.CastSpell();
	        // 						effect.Cancel(false);
	        // 					}
	        // 					else if (effect.SpellHandler is RangeSpellHandler)
	        // 					{
	        // 						cloneSpell = spell.Copy();
	        // 						cloneSpell.CostPower = false;
	        // 						cloneSpell.OverrideRange = effect.Spell.Range;
	        // 						spellhandler = ScriptMgr.CreateSpellHandler(this, cloneSpell, line);
	        // 						casted = spellhandler.CastSpell();
	        // 						effect.Cancel(false);
	        // 					}
	        // 					else if (effect.SpellHandler is UninterruptableSpellHandler)
	        // 					{
	        // 						cloneSpell = spell.Copy();
	        // 						cloneSpell.CostPower = false;
	        // 						spellhandler = ScriptMgr.CreateSpellHandler(this, cloneSpell, line);
	        // 						casted = spellhandler.CastSpell();
	        // 						effect.Cancel(false);
	        // 					}
	        // 				}
	        // 			}
	        // 			else
	        // 				spellhandler.CastSpell();
	        // 		}
	        // 	}
	        // 	else
	        // 	{
	        // 		Out.SendMessage(spell.Name + " not implemented yet (" + spell.SpellType + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
	        // 		return false;
	        // 	}
	        // }
	        // return casted;
        }


        private bool CheckSpellPrecast(Spell spell, SpellLine line)
        {
	        return true;
        }
    }
}